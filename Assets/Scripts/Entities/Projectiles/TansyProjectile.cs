using System.Collections.Generic;
using UnityEngine;

// Tansy's orbiting attack projectile. infinite piercing - it is never destroyed by a hit, only
// when Tansy herself dies (base attack) or when it's explicitly sent home (Floral Glow).
// orbits its center at a fixed, player-chosen radius (see Tansy.SetOrbitRadius) - revolution
// time (one full lap) is set by Attack Speed alone and stays the SAME regardless of radius, so a
// larger radius means a faster-moving projectile covering more ground per lap, not slower
// revolutions. the base attack always orbits Tansy; Floral Glow reuses this same class but
// flies out to orbit a target plant instead (dealing damage along the way, same as always), driven
// by the TARGET's Attack Speed rather than Tansy's once it arrives
public class TansyProjectile : MonoBehaviour
{
    [SerializeField] private GameObject hitParticlePrefab;
    // the sprite's own default facing angle (degrees) - e.g. 180 if it's drawn facing left like
    // Sunflower's, 0 if it's drawn facing right, 90/-90 if up/down. adjust to match your art
    [SerializeField] private float spriteDefaultFacingAngle = 180f;

    private Tansy _tansy; // damage is always attributed to this Tansy, regardless of who it orbits
    private Plant _center;        // who we orbit right now; null while traveling
    private Plant _pendingCenter; // destination plant while traveling to a new orbit (not yet arrived)
    private Plant _speedSource;   // whose Attack Speed sets the orbit's angular rate
    private bool _traveling;
    private bool _returningHome;

    private bool _isSkillMode; // true for a Floral Glow projectile, false for Tansy's own attack
    private bool _reservedSlot; // true once PromoteNextWaiter has claimed a slot for it, before it arrives to actually take it

    // at most this many Floral Glow projectiles may orbit any one plant at a time (so players can't
    // stack Floral Glow from a crowd of Tansys onto a single plant without limit). Tansy's
    // own base-attack projectiles are NOT counted against this cap or queued - only Floral Glow is.
    // the buff itself is never gated by this - see UpdateTravel, it's granted on arrival regardless
    private const int MaxFloralGlowPerCenter = 3;

    private float _orbitAngle;
    // remaining angular gap toward a newly (re)assigned slot - whenever the group's membership
    // changes, RespaceOrbiters hands everyone a fresh target slot. rather than snapping there or
    // adding a flat extra rotation on top, this is closed by modulating THIS instance's own
    // orbital speed: if the slot sits ahead (within 180 degrees, in the direction of travel) this
    // projectile speeds up to catch up to it; if it sits behind, this projectile slows down so the
    // slot (which keeps advancing at the shared base rate) catches back up to it. see UpdateOrbit
    private float _angleCorrection;
    private float _orbitRadius = 1f;
    private float _targetOrbitRadius = 1f; // player-chosen fixed radius, set via Tansy.SetOrbitRadius
    private Vector3 _direction; // current heading, maintained deliberately (see Update's rotation comment)

    private const float RadiusLerpSpeed = 2f;
    // how much faster/slower (as a fraction of the base angular speed) this projectile moves while
    // closing out a pending _angleCorrection - e.g. 0.5 means 50% faster when catching up ahead, or
    // 50% slower when waiting for the slot to catch up from behind
    private const float CorrectionSpeedFactor = 0.5f;
    private const float TravelSpeed = 6f;
    private const float ArrivalThreshold = 0.15f;

    // pierce damage falloff: each insect this projectile hits costs it 10% damage, down to a floor
    // of 50% - and it's reset back to full the moment it next crosses 12 o'clock (directly "above"
    // whatever it's orbiting), so a fresh lap always starts hitting at full damage again. applies
    // the same way whether this is the base attack (orbiting Tansy) or Floral Glow (orbiting
    // another plant) - tracked per-instance, never shared with the rest of the orbiting group
    private const float PierceDamageReductionPerHit = 0.1f;
    private const float MinPierceDamageMultiplier = 0.5f;
    private const float HalfPi = Mathf.PI * 0.5f;
    private const float TwoPi = Mathf.PI * 2f;
    private int _pierceHitsSinceReset;
    private float _nextResetAngle;

    private static readonly DamageTag[] _attackDamageTags = { DamageTag.Attack, DamageTag.Projectile };
    // Coordinated: Entity.Damage() automatically multiplies this hit by (1 + source.coordinatedDamage)
    private static readonly DamageTag[] _skillDamageTags  = { DamageTag.SkillDamage, DamageTag.Projectile, DamageTag.Coordinated };

    // every TansyProjectile currently orbiting a given plant, regardless of which Tansy (or
    // Tansys) they belong to - Tansy's own base-attack projectiles and any number of
    // different Tansys' Floral Glow projectiles can all target the same plant simultaneously
    // (additive), and this is what keeps the WHOLE set evenly spaced, recomputed every time any one
    // of them joins or leaves
    private static readonly Dictionary<Plant, List<TansyProjectile>> _orbitersByCenter = new Dictionary<Plant, List<TansyProjectile>>();
    // abstract (no live GameObject) Floral Glow requests queued for a center that's already at
    // MaxFloralGlowPerCenter, FIFO - the projectile that discovered the target full is destroyed
    // immediately rather than sitting there with nowhere to go; the first entry here is respawned
    // fresh (by its own Tansy) once a slot actually frees up there
    private static readonly Dictionary<Plant, List<Tansy>> _queuedByCenter = new Dictionary<Plant, List<Tansy>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        _orbitersByCenter.Clear();
        _queuedByCenter.Clear();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) =>
        {
            _orbitersByCenter.Clear();
            _queuedByCenter.Clear();
        };
    }

    // true once a center already has MaxFloralGlowPerCenter Floral Glow projectiles orbiting it
    private static bool IsFloralGlowFull(Plant center)
    {
        _orbitersByCenter.TryGetValue(center, out List<TansyProjectile> currentOrbiters);
        if (currentOrbiters == null) return false;
        int floralGlowCount = 0;
        foreach (TansyProjectile p in currentOrbiters)
            if (p != null && p._isSkillMode) floralGlowCount++;
        return floralGlowCount >= MaxFloralGlowPerCenter;
    }

    // joins the orbit, claiming its slot. called once travel arrives at a new center - either for
    // Tansy's own base attack (never capped/queued), or a Floral Glow projectile that already
    // reserved a slot here via PromoteNextWaiter (so it skips the fullness check - that slot is
    // already its own, re-checking could wrongly bounce it back into its own vacated queue spot)
    private void TryJoinOrbit(Plant center)
    {
        _reservedSlot = false;
        _traveling = false;
        _center = center;
        _pendingCenter = null;
        _orbitRadius = _targetOrbitRadius;
        _nextResetAngle = ComputeNextResetAngle(_orbitAngle);
        RegisterOrbiter(center);
    }

    // smallest angle >= fromAngle that sits at 12 o'clock (directly "above" the center, i.e.
    // congruent to 90 degrees modulo a full lap) - used to schedule the next pierce-reduction reset
    private static float ComputeNextResetAngle(float fromAngle)
    {
        float remaining = Mathf.Repeat(HalfPi - fromAngle, TwoPi);
        if (remaining <= 0.0001f) remaining += TwoPi;
        return fromAngle + remaining;
    }

    // records an abstract queue entry for center (no live GameObject - this projectile already
    // completed its trip and is consumed/destroyed) so its Tansy gets another try once a slot
    // there actually frees up
    private static void EnqueueFloralGlowRequest(Tansy source, Plant center)
    {
        if (!_queuedByCenter.TryGetValue(center, out List<Tansy> queue))
        {
            queue = new List<Tansy>();
            _queuedByCenter[center] = queue;
        }
        queue.Add(source);
    }

    // promotes whoever's been queued longest for this center, if anyone - called right after a
    // Floral Glow projectile leaves it, so the count never drops below the cap while someone's
    // still queued for a spot. has that Tansy spawn a brand new projectile to actually claim it
    private static void PromoteNextWaiter(Plant center)
    {
        if (center == null || !_queuedByCenter.TryGetValue(center, out List<Tansy> queue) || queue.Count == 0) return;
        Tansy next = queue[0];
        queue.RemoveAt(0);
        if (queue.Count == 0) _queuedByCenter.Remove(center);
        if (next != null) next.SpawnReservedFloralGlowProjectile(center);
    }

    private void RegisterOrbiter(Plant center)
    {
        if (center == null) return;
        if (!_orbitersByCenter.TryGetValue(center, out List<TansyProjectile> list))
        {
            list = new List<TansyProjectile>();
            _orbitersByCenter[center] = list;
        }
        if (!list.Contains(this)) list.Add(this);
        RespaceOrbiters(list);
    }

    private void UnregisterOrbiter(Plant center)
    {
        if (center == null) return;
        bool wasFloralGlowOrbiter = _isSkillMode;
        if (_orbitersByCenter.TryGetValue(center, out List<TansyProjectile> list))
        {
            list.Remove(this);
            if (list.Count == 0) _orbitersByCenter.Remove(center);
            else RespaceOrbiters(list);
        }
        // a Floral Glow slot just freed up here - let the next waiter (if any) take it
        if (wasFloralGlowOrbiter) PromoteNextWaiter(center);
    }

    private static void RespaceOrbiters(List<TansyProjectile> list)
    {
        float step = 2f * Mathf.PI / list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            TansyProjectile p = list[i];
            if (p == null) continue;
            float targetAngle = i * step;
            // shortest signed distance from the current angle to the new target, wrapped to
            // (-pi, pi] - smoothly closed over time in UpdateOrbit instead of snapping here, and
            // turning the short way around rather than potentially sweeping almost a full circle
            float diff = targetAngle - p._orbitAngle;
            diff = Mathf.Repeat(diff + Mathf.PI, 2f * Mathf.PI) - Mathf.PI;
            p._angleCorrection = diff;
        }
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

    // base attack: always orbits Tansy, starting immediately. its slot among everyone else
    // currently orbiting Tansy (her own other attack projectiles, and/or anyone else's Floral
    // Glow projectiles if she's been targeted by one) is assigned by RegisterOrbiter below
    public void InitializeOrbitingTansy(Tansy tansy)
    {
        _tansy = tansy;
        _center = tansy;
        _speedSource = tansy;
        _orbitRadius = 1f;
        _nextResetAngle = ComputeNextResetAngle(_orbitAngle);
        transform.position = OrbitPosition(tansy.transform.position);
        RegisterOrbiter(tansy);
    }

    // Floral Glow: always spawns at Tansy and immediately flies out toward the target, no
    // matter how many Floral Glow projectiles are already there - whether it actually gets to join
    // the visible orbit once it arrives is decided later (see UpdateTravel); the buff itself is
    // granted on arrival either way
    public void InitializeAsFloralGlow(Tansy tansy, Plant target)
    {
        _tansy = tansy;
        _isSkillMode = true;
        transform.position = tansy.transform.position;
        TravelTo(target);
    }

    // marks this freshly-spawned projectile as already owning a reserved slot at wherever it's
    // currently headed, so it skips the fullness check on arrival (see Tansy.SpawnReservedFloralGlowProjectile)
    public void ReserveSlot() => _reservedSlot = true;

    // retargets a live Floral Glow projectile to a different bearer without destroying/respawning it
    public void TravelTo(Plant newCenter)
    {
        UnregisterOrbiter(_center); // vacate the old center's orbit slot immediately, if it had one
        _center = null;
        _pendingCenter = newCenter;
        _speedSource = newCenter;
        _reservedSlot = false; // an ordinary retarget never comes with a pre-claimed slot
        _traveling = true;
        _returningHome = false;
    }

    // Floral Glow ended (bearer died, or the effect expired with no new target) - fly back to
    // Tansy and self-destruct on arrival, dealing damage along the way like any other travel
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
        if (_tansy == null || !_tansy.IsAlive) { DestroySelf(); return; }

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
            ? _tansy.transform.position
            : (_pendingCenter != null ? _pendingCenter.transform.position : transform.position);

        Vector3 toDestination = destination - transform.position;
        if (toDestination.sqrMagnitude > 0.0001f) _direction = toDestination.normalized;

        transform.position = Vector3.MoveTowards(transform.position, destination, TravelSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, destination) > ArrivalThreshold) return;

        if (_returningHome) { DestroySelf(); return; }

        Plant center = _pendingCenter;

        // the buff is granted the moment the projectile reaches the target, whether or not there's
        // room for it to actually join the visible orbit - the slot cap below only ever gates the
        // damage-dealing projectile itself, never the buff
        if (_isSkillMode) _tansy.GrantTansyGlowEffect(center);

        // a reserved slot (promoted from the queue) always takes priority over a fresh fullness
        // check - it's already its own slot. otherwise, if the target's orbit is already full, this
        // projectile is consumed/destroyed here (the buff above still landed) and queued so its
        // Tansy gets a fresh projectile sent out once a slot actually frees up
        if (_isSkillMode && !_reservedSlot && IsFloralGlowFull(center))
        {
            _tansy.OnFloralGlowProjectileQueued(this);
            EnqueueFloralGlowRequest(_tansy, center);
            DestroySelf();
            return;
        }

        TryJoinOrbit(center);
    }

    // the player-chosen radius this orbit should glide toward - pushed once at spawn (snapImmediately,
    // so a fresh orbit doesn't visibly grow out from a radius of 1) and again whenever the player
    // toggles it afterward (gliding, so a live change doesn't teleport). clamped here too in case
    // it's ever set past Tansy's current Attack Range (e.g. a skill tree respec shrinking it)
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

        // revolution time is set by Attack Speed alone, independent of radius - a full lap always
        // takes 1/attackSpeed seconds no matter how wide the orbit is, so a bigger radius means a
        // faster-moving projectile (covering more distance per lap) rather than a slower-ticking
        // one. this also means everyone orbiting the same center naturally shares the same angular
        // speed regardless of their own radius or gliding state, so RespaceOrbiters' even spacing
        // can never be broken by a radius difference the way it could when speed depended on radius
        float attackSpeed = _speedSource != null ? _speedSource.attackSpeed : 1f;
        float angularSpeed = attackSpeed * Mathf.PI * 2f;
        float baseStep = angularSpeed * Time.deltaTime;

        // close out any pending re-spacing correction (see RespaceOrbiters) by nudging THIS
        // instance's own speed up or down for as long as the gap remains, rather than snapping or
        // adding a flat extra rotation on top. the target slot advances every frame at exactly
        // baseStep too (it shares the same angular speed), so the gap shrinks by precisely however
        // much extra/less than baseStep this instance moves - once it's fully closed, speed
        // reverts to normal
        float extra = 0f;
        if (_angleCorrection != 0f)
        {
            extra = Mathf.Sign(_angleCorrection) * CorrectionSpeedFactor * baseStep;
            if (Mathf.Abs(extra) >= Mathf.Abs(_angleCorrection)) extra = _angleCorrection;
            _angleCorrection -= extra;
        }
        _orbitAngle += baseStep + extra;

        // crossed 12 o'clock this frame - reset the pierce damage falloff back to full for the
        // next lap
        if (_orbitAngle >= _nextResetAngle)
        {
            _pierceHitsSinceReset = 0;
            _nextResetAngle += TwoPi;
        }

        // exact tangential direction at the new angle (perpendicular to the radius, in the
        // direction of travel) - analytic, so it stays clean even while the radius is also gliding
        _direction = new Vector3(-Mathf.Sin(_orbitAngle), Mathf.Cos(_orbitAngle), 0f);

        transform.position = OrbitPosition(_center.transform.position);
    }

    private Vector3 OrbitPosition(Vector3 center) =>
        center + new Vector3(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle), 0f) * _orbitRadius;

    // safety net: covers every destruction path (DestroySelf, or Tansy directly Destroy()-ing
    // one of her own projectiles, e.g. to shrink her attack count or because she herself just died
    // mid-orbit) so the center it was last sitting on always cleans up and re-spaces/promotes
    // correctly, even if a call site forgets to do it explicitly.
    // skipped entirely while the scene is unloading - UnregisterOrbiter can promote a queued
    // request, which spawns a brand new GameObject, and doing that mid-teardown is exactly what
    // Unity's "objects not cleaned up when closing the scene" warning is about
    private void OnDestroy()
    {
        if (!gameObject.scene.isLoaded) return;
        UnregisterOrbiter(_center);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_tansy == null || !other.CompareTag("Insect")) return;
        Insect insect = other.GetComponentInParent<Insect>();
        if (insect == null) return;

        _touchCount.TryGetValue(insect, out int count);
        _touchCount[insect] = count + 1;
        if (count > 0) return; // already touching this insect through another collider - not a new pass

        if (!insect.IsAlive || insect.team == Team.Friendly) return;
        float baseDamage = _isSkillMode ? _tansy.EffectiveSkillDamage(_center) : _tansy.EffectiveAttackDamage(_center);
        float pierceMultiplier = Mathf.Max(MinPierceDamageMultiplier, 1f - _pierceHitsSinceReset * PierceDamageReductionPerHit);
        _pierceHitsSinceReset++;
        float damage = baseDamage * pierceMultiplier;
        DamageTag[] tags = _isSkillMode ? _skillDamageTags : _attackDamageTags;
        insect.Damage(damage, _tansy.damageType, _tansy.elementalType, _tansy, true, tags);

        // Entity.Damage() only auto-plays the impact sound for DamageTag.Attack hits, and the skill
        // is deliberately tagged SkillDamage instead (so it doesn't also pull in unrelated Attack-tag
        // side effects like the Symbiosis/Wither cooldown-reduction passive) - so it's played here
        // directly instead, to keep particles and sound identical between the attack and the skill
        if (_isSkillMode) SfxPlayer.Play(_tansy.data?.impactSound, transform.position);
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
