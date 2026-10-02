using System.Collections.Generic;
using UnityEngine;

// Calendula's orbiting attack projectile. infinite piercing - it is never destroyed by a hit, only
// when Calendula herself dies (base attack) or when it's explicitly sent home (Floral Glow).
// orbits its center at a fixed, player-chosen radius (see Calendula.SetOrbitRadius) at an angular
// speed set by Attack Speed - a larger radius means slower revolutions (fewer hits per second) at
// the same tangential speed, not a faster moving projectile. the base attack always orbits
// Calendula; Floral Glow reuses this same class but flies out to orbit a target plant instead
// (dealing damage along the way, same as always), driven by the TARGET's Attack Speed rather than
// Calendula's once it arrives
public class CalendulaProjectile : MonoBehaviour
{
    [SerializeField] private GameObject hitParticlePrefab;
    // the sprite's own default facing angle (degrees) - e.g. 180 if it's drawn facing left like
    // Sunflower's, 0 if it's drawn facing right, 90/-90 if up/down. adjust to match your art
    [SerializeField] private float spriteDefaultFacingAngle = 180f;

    private Calendula _calendula; // damage is always attributed to this Calendula, regardless of who it orbits
    private Plant _center;        // who we orbit right now; null while traveling
    private Plant _pendingCenter; // destination plant while traveling to a new orbit (not yet arrived)
    private Plant _speedSource;   // whose Attack Speed sets the orbit's angular rate
    private bool _traveling;
    private bool _returningHome;

    private bool _isSkillMode; // true for a Floral Glow projectile, false for Calendula's own attack

    private float _orbitAngle;
    private float _orbitRadius = 1f;
    private float _targetOrbitRadius = 1f; // player-chosen fixed radius, set via Calendula.SetOrbitRadius
    private Vector3 _direction; // current heading, maintained deliberately (see Update's rotation comment)

    private const float MinOrbitRadius = 0.3f;
    private const float RadiusLerpSpeed = 2f;
    private const float TravelSpeed = 6f;
    private const float ArrivalThreshold = 0.15f;

    private static readonly DamageTag[] _attackDamageTags = { DamageTag.Attack, DamageTag.Projectile };
    // Coordinated: Entity.Damage() automatically multiplies this hit by (1 + source.coordinatedDamage)
    private static readonly DamageTag[] _skillDamageTags  = { DamageTag.SkillDamage, DamageTag.Projectile, DamageTag.Coordinated };

    // every CalendulaProjectile currently orbiting a given plant, regardless of which Calendula (or
    // Calendulas) they belong to - Calendula's own base-attack projectiles and any number of
    // different Calendulas' Floral Glow projectiles can all target the same plant simultaneously
    // (additive), and this is what keeps the WHOLE set evenly spaced, recomputed every time any one
    // of them joins or leaves
    private static readonly Dictionary<Plant, List<CalendulaProjectile>> _orbitersByCenter = new Dictionary<Plant, List<CalendulaProjectile>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        _orbitersByCenter.Clear();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => _orbitersByCenter.Clear();
    }

    private void RegisterOrbiter(Plant center)
    {
        if (center == null) return;
        if (!_orbitersByCenter.TryGetValue(center, out List<CalendulaProjectile> list))
        {
            list = new List<CalendulaProjectile>();
            _orbitersByCenter[center] = list;
        }
        if (!list.Contains(this)) list.Add(this);
        RespaceOrbiters(list);
    }

    private void UnregisterOrbiter(Plant center)
    {
        if (center == null || !_orbitersByCenter.TryGetValue(center, out List<CalendulaProjectile> list)) return;
        list.Remove(this);
        if (list.Count == 0) _orbitersByCenter.Remove(center);
        else RespaceOrbiters(list);
    }

    private static void RespaceOrbiters(List<CalendulaProjectile> list)
    {
        float step = 2f * Mathf.PI / list.Count;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null) list[i]._orbitAngle = i * step;
    }

    // counts simultaneously-overlapping colliders per insect (an insect can have more than one
    // collider tagged "Insect"), so a hit only applies once per continuous touch - without this,
    // an orbiting collider grazing an insect's edge can fire Enter/Exit/Enter within one pass, or
    // overlap two of that insect's colliders at once, and double up the damage
    private readonly Dictionary<Insect, int> _touchCount = new Dictionary<Insect, int>();

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;
    }

    // base attack: always orbits Calendula, starting immediately. its slot among everyone else
    // currently orbiting Calendula (her own other attack projectiles, and/or anyone else's Floral
    // Glow projectiles if she's been targeted by one) is assigned by RegisterOrbiter below
    public void InitializeOrbitingCalendula(Calendula calendula)
    {
        _calendula = calendula;
        _center = calendula;
        _speedSource = calendula;
        _orbitRadius = 1f;
        transform.position = OrbitPosition(calendula.transform.position);
        RegisterOrbiter(calendula);
    }

    // Floral Glow: spawns at Calendula and flies out to orbit the target plant instead
    public void InitializeAsFloralGlow(Calendula calendula, Plant target)
    {
        _calendula = calendula;
        _isSkillMode = true;
        transform.position = calendula.transform.position;
        TravelTo(target);
    }

    // retargets a live Floral Glow projectile to a different bearer without destroying/respawning it
    public void TravelTo(Plant newCenter)
    {
        UnregisterOrbiter(_center); // vacate the old center's slot immediately, not once travel completes
        _center = null;
        _pendingCenter = newCenter;
        _speedSource = newCenter;
        _traveling = true;
        _returningHome = false;
    }

    // Floral Glow ended (bearer died, or the effect expired with no new target) - fly back to
    // Calendula and self-destruct on arrival, dealing damage along the way like any other travel
    public void ReturnHomeAndDestroy()
    {
        UnregisterOrbiter(_center);
        _center = null;
        _pendingCenter = null;
        _speedSource = null;
        _traveling = true;
        _returningHome = true;
    }

    private void Update()
    {
        if (_calendula == null || !_calendula.IsAlive) { DestroySelf(); return; }

        if (_traveling) UpdateTravel();
        else UpdateOrbit();

        // same convention as SunflowerProjectile: keep a persistent, deliberately-computed
        // direction vector (not one inferred from a frame-to-frame position delta, which is noisy
        // when deltaTime varies or the radius is gliding at the same time the angle is), and
        // rotate to point it along that direction's own angle minus the sprite's default facing
        if (_direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - spriteDefaultFacingAngle);
        }
    }

    private void UpdateTravel()
    {
        Vector3 destination = _returningHome
            ? _calendula.transform.position
            : (_pendingCenter != null ? _pendingCenter.transform.position : transform.position);

        Vector3 toDestination = destination - transform.position;
        if (toDestination.sqrMagnitude > 0.0001f) _direction = toDestination.normalized;

        transform.position = Vector3.MoveTowards(transform.position, destination, TravelSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, destination) > ArrivalThreshold) return;

        if (_returningHome) { DestroySelf(); return; }

        _center = _pendingCenter;
        _pendingCenter = null;
        _traveling = false;
        _orbitRadius = _targetOrbitRadius;
        RegisterOrbiter(_center);
    }

    // the player-chosen radius this orbit should glide toward - pushed once at spawn (snapImmediately,
    // so a fresh orbit doesn't visibly grow out from a radius of 1) and again whenever the player
    // toggles it afterward (gliding, so a live change doesn't teleport). clamped here too in case
    // it's ever set past Calendula's current Attack Range (e.g. a skill tree respec shrinking it)
    public void SetOrbitRadius(float radius, bool snapImmediately = false)
    {
        _targetOrbitRadius = Mathf.Max(1f, radius);
        if (snapImmediately) _orbitRadius = _targetOrbitRadius;
    }

    private void UpdateOrbit()
    {
        if (_center == null || !_center.IsAlive) { ReturnHomeAndDestroy(); return; }

        // fixed, player-chosen radius - still glides rather than snaps, so toggling it mid-game
        // doesn't make the projectile teleport
        _orbitRadius = Mathf.MoveTowards(_orbitRadius, _targetOrbitRadius, RadiusLerpSpeed * Time.deltaTime);

        float attackSpeed = _speedSource != null ? _speedSource.attackSpeed : 1f;
        float angularSpeed = (attackSpeed * Mathf.PI * 2f) / Mathf.Max(_orbitRadius, MinOrbitRadius);
        _orbitAngle += angularSpeed * Time.deltaTime;

        // exact tangential direction at the new angle (perpendicular to the radius, in the
        // direction of travel) - analytic, so it stays clean even while the radius is also gliding
        _direction = new Vector3(-Mathf.Sin(_orbitAngle), Mathf.Cos(_orbitAngle), 0f);

        transform.position = OrbitPosition(_center.transform.position);
    }

    private Vector3 OrbitPosition(Vector3 center) =>
        center + new Vector3(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle), 0f) * _orbitRadius;

    // safety net: covers every destruction path (DestroySelf, or Calendula directly Destroy()-ing
    // one of her own projectiles to shrink her attack count) so the center it was last orbiting
    // always re-spaces down, even if some future call site forgets to Unregister explicitly first
    private void OnDestroy() => UnregisterOrbiter(_center);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_calendula == null || !other.CompareTag("Insect")) return;
        Insect insect = other.GetComponentInParent<Insect>();
        if (insect == null) return;

        _touchCount.TryGetValue(insect, out int count);
        _touchCount[insect] = count + 1;
        if (count > 0) return; // already touching this insect through another collider - not a new pass

        if (!insect.IsAlive || insect.team == Team.Friendly) return;
        float damage = _isSkillMode ? _calendula.EffectiveSkillDamage : _calendula.EffectiveAttackDamage;
        DamageTag[] tags = _isSkillMode ? _skillDamageTags : _attackDamageTags;
        insect.Damage(damage, _calendula.damageType, _calendula.elementalType, _calendula, true, tags);
        PlayHitParticles();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Insect")) return;
        Insect insect = other.GetComponentInParent<Insect>();
        if (insect == null || !_touchCount.TryGetValue(insect, out int count)) return;

        if (count <= 1) _touchCount.Remove(insect);
        else _touchCount[insect] = count - 1;
    }

    // one-shot burst at the point of impact - this prefab plays on its own (Play On Awake,
    // non-looping) but doesn't self-destroy (Stop Action: None), so it has to be cleaned up
    // manually once it's done, sized to whatever duration/lifetime are actually set on it
    private void PlayHitParticles()
    {
        if (hitParticlePrefab == null) return;

        GameObject obj = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        float lifetime = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f;
        Destroy(obj, lifetime);
    }

    // detaches any child particle trail before destroying this projectile, so it survives and
    // finishes fading out naturally instead of popping out of existence along with the parent.
    // mirrors SunflowerProjectile.OnBeforeDestroy() exactly, including why worldPositionStays
    // can't be used: this particle system's Scaling Mode is Local, which reads localScale
    // directly as the particle-size multiplier, so SetParent(null, true) rewriting localScale to
    // preserve world scale would make every particle instantly snap to a smaller size on detach
    private void DestroySelf()
    {
        ParticleSystem trail = GetComponentInChildren<ParticleSystem>();
        if (trail != null)
        {
            Transform t = trail.transform;
            Vector3 worldPos = t.position;
            Quaternion worldRot = t.rotation;

            t.SetParent(null, false);
            t.position = worldPos;
            t.rotation = worldRot;

            trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(trail.gameObject, trail.main.startLifetime.constantMax);
        }

        Destroy(gameObject);
    }
}
