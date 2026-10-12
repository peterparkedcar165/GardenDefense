using System.Collections.Generic;
using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip hit;
    [SerializeField] private float minPitch = 0.9f, maxPitch = 1.1f;
    [SerializeField] private float hitVolume = 0.5f;
    private int hitCount = 0;

    [Header("Hit Particles")]
    // one-shot burst at the point of impact, shared by every projectile - optional, left null for
    // the ones that don't have one yet
    [SerializeField] protected GameObject hitParticlePrefab;

    [Header("Combat")]
    public float projectileDamage, projectileSpeed, maxRange;
    public int piercing;
    public DamageType damageType;
    public ElementalType elementalType;
    protected Vector3 direction;
    protected Vector3 spawnPosition;
    public Plant source; // will be set by the plant who fires this projectile

    protected GameObject trackedTarget; // keeps reference to the tracked target
    protected Insect trackedInsect;     // cached component, set alongside trackedTarget

    // Base/Visual/Shadow split, mirroring Insect's own structure exactly: this root transform
    // IS Base (ground-plane logical position - drives all homing/movement and the base-distance
    // half of the hit check below, same as Insect.transform.position). Visual and Shadow are real
    // child GameObjects (every projectile prefab has them now) rather than data-only tricks -
    // Visual carries the actual rendered sprite and is the other half of the hit check (it rises
    // to mirror however high the tracked target currently is); Shadow stays glued to Base's
    // ground position always, purely cosmetic. both children's WORLD position is recomputed
    // every frame from their ORIGINAL authored local offset (captured once in Awake) rather than
    // left to normal Unity parenting, since several projectiles rotate this root to face their
    // travel direction (needles, Snowdrop's shot, etc.) - a plain local offset under a rotating
    // parent would swing around with it instead of staying "above"/"below" in world terms
    // defaults match PlantData's baseProjectileHitboxRadius/baseProjectileVisualHitboxRadius -
    // overwritten from there in Initialize() for any projectile actually fired by a Shooter plant;
    // these only matter as a fallback (e.g. a skill-only projectile with no real source plant)
    [Header("Hit Radii")]
    [SerializeField] protected float baseHitboxRadius = 0.1f;
    [SerializeField] protected float visualHitboxRadius = 0.1f;
    protected Transform visual;
    protected Transform shadow;
    private Vector3 _visualRestLocalOffset;
    private Vector3 _shadowRestLocalOffset;
    private float _visualHeightOffset;
    private float _visualHeightVelocity; // SmoothDamp's own velocity state, carried frame to frame
    private const float VisualHeightSmoothTime = 0.25f; // seconds to settle into a new target height
    private readonly HashSet<Insect> _alreadyHit = new HashSet<Insect>();

    // subclasses with their own bespoke insect-collision handling (bounce chains, orbit sweeps,
    // etc. - anything already calling OnHit/damage itself from its own OnTriggerEnter2D) set this
    // false so they don't also get hit a second time by the automatic Base/Visual scan below
    protected virtual bool UseAutomaticInsectHits => true;

    // spawns the projectile, and assigns basic stats to it
    public virtual void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        direction = (target - transform.position).normalized;
        this.projectileDamage = projectileDamage;
        this.projectileSpeed = projectileSpeed;
        this.piercing = piercing;
        this.damageType = damageType;
        this.maxRange = maxRange;
        this.elementalType = elementalType;
        this.source = source;
        this.spawnPosition = transform.position;

        // hitbox radii are authored per-plant on PlantData (under the "Shooter" header) rather
        // than per-prefab, so every projectile a given plant fires reflects that plant's own
        // sizing - falls back to this component's own serialized default if fired with no
        // Shooter/data (e.g. a skill-only projectile with no real source plant)
        if (source?.data != null)
        {
            baseHitboxRadius = source.data.baseProjectileHitboxRadius;
            visualHitboxRadius = source.data.baseProjectileVisualHitboxRadius;
        }

        // Floral Glow: whichever plant currently carries it gets a fire trail on its own
        // projectiles too, regardless of which plant's projectile this actually is - read
        // generically off the effect rather than any particular plant's own particle setup.
        // parented so it travels with the projectile; left to be destroyed along with it, same
        // as any other purely cosmetic trail (only the one-shot hit burst below needs to outlive it)
        GameObject flyPrefab = this.source?.GetEffect<FloralGlowEffect>()?.FlyParticlePrefab;
        if (flyPrefab != null)
            Instantiate(flyPrefab, transform.position, transform.rotation, transform);
    }

    protected virtual void Awake()
    {
        // some projectiles are particles-only now (no sprite at all), so this has to be optional
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 1;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        // every projectile prefab has Visual/Shadow children now (see Insect's own Base/Visual/
        // Shadow split) - fall back to this root itself for either one on the off chance a
        // prefab is missing it, rather than throwing
        visual = transform.Find("Visual") ?? transform;
        shadow = transform.Find("Shadow") ?? transform;
        _visualRestLocalOffset = visual != transform ? visual.localPosition : Vector3.zero;
        _shadowRestLocalOffset = shadow != transform ? shadow.localPosition : Vector3.zero;
    }

    protected virtual void Update()
    {
        Move();
    }

    public void SetTarget(GameObject target)
    {
        trackedTarget = target;
        trackedInsect = target != null ? target.GetComponent<Insect>() : null;
    }
    protected virtual void Move()
    {
        if (trackedTarget != null)
        {
            if (trackedInsect != null && (!trackedInsect.IsAlive || trackedInsect.team == Team.Friendly))
                { trackedTarget = null; trackedInsect = null; }
            else
            {
                // home toward the target's own ground position (Base), never its elevated
                // GetAimPoint() - that's what used to drag this projectile's own position up
                // through a flying target's height band and let an unrelated ground insect it
                // merely crossed paths with there steal the hit instead
                Vector3 baseTargetPos = trackedInsect != null ? trackedInsect.transform.position : trackedTarget.transform.position;
                Vector3 toTarget = baseTargetPos - transform.position;
                if (Vector3.Dot(direction, toTarget) > 0)
                    direction = toTarget.normalized;
                else
                    { trackedTarget = null; trackedInsect = null; }
            }
        }

        transform.position += direction * projectileSpeed * Time.deltaTime;

        // cosmetic-only: Visual rises to mirror however high the tracked target currently is, so
        // the rendered sprite still visually arcs up to meet a flying insect even though Base
        // (this root) never leaves the ground plane. Shadow always stays glued to Base. both read
        // off the ORIGINAL authored local offset via TransformPoint (not plain parenting) so a
        // rotating root doesn't swing them around - see the field comments above.
        //
        // only chases a height while there's actually a live target to chase - SmoothDamp eases
        // into it naturally (accelerates in, decelerates out, no instant snap and no sudden stop
        // at arrival) rather than a constant-rate approach. once the target's gone (died, out of
        // range, already hit), the offset is simply left exactly where it is and never touched
        // again instead of being pulled back to 0 - so a shot that was flying up at a bird keeps
        // flying at that same height in a straight line, no sudden change in vertical motion
        if (trackedInsect != null)
        {
            float targetHeight = trackedInsect.GetAimPoint().y - trackedInsect.transform.position.y;
            _visualHeightOffset = Mathf.SmoothDamp(_visualHeightOffset, targetHeight, ref _visualHeightVelocity, VisualHeightSmoothTime);
        }
        else
        {
            // frozen in place (see above) - zero the carried velocity too, so VisualHeading below
            // correctly reports "no vertical motion" instead of a stale climb/dive rate
            _visualHeightVelocity = 0f;
        }
        if (visual != transform) visual.position = transform.TransformPoint(_visualRestLocalOffset) + Vector3.up * _visualHeightOffset;
        if (shadow != transform) shadow.position = transform.TransformPoint(_shadowRestLocalOffset);

        if (UseAutomaticInsectHits) HandleInsectHits();

        if (!Tile.IsInsideGrid(transform.position))
        {
            OnBeforeDestroy();
            Destroy(gameObject);
        }
        else if (maxRange > 0f && Vector3.Distance(spawnPosition, transform.position) >= maxRange)
        {
            OnBeforeDestroy();
            Destroy(gameObject);
        }
    }

    // where Visual is actually heading this frame, as opposed to "direction" (Base's own,
    // purely-horizontal travel direction). while the height offset above is actively rising or
    // falling, Visual's real path is diagonal - combining Base's horizontal motion with however
    // fast the height is currently changing (_visualHeightVelocity, SmoothDamp's own rate) - so a
    // sprite that rotates to face its travel direction should read this instead of "direction"
    // directly, or it'll keep pointing flat/horizontal even while visibly climbing or diving.
    // settles back to exactly "direction" once the height offset stops changing
    protected Vector3 VisualHeading
    {
        get
        {
            Vector3 v = direction * projectileSpeed + Vector3.up * _visualHeightVelocity;
            return v.sqrMagnitude > 0.0001f ? v.normalized : direction;
        }
    }

    // a hit only registers when BOTH halves agree: the Base positions (ground-plane, ignoring
    // height) are within combined baseHitboxRadius, AND the Visual positions (height-aware -
    // visual.position here, GetAimPoint() on the insect's side) are also within combined
    // visualHitboxRadius. replaces the old OnTriggerEnter2D-driven insect detection entirely,
    // since what we need to compare against (GetAimPoint()) was never where either side's actual
    // Collider2D lived in the first place - scans every live hostile insect each frame instead,
    // same O(n) cost Aura.GetInsectsInRange() already pays for its own range checks
    private void HandleInsectHits()
    {
        Vector3 basePos = transform.position;
        Vector3 visualPos = visual.position;
        foreach (Insect insect in Insect.allInsects)
        {
            if (!insect.IsAlive || insect.team == Team.Friendly) continue;
            if (_alreadyHit.Contains(insect)) continue;

            float baseDist = Vector3.Distance(basePos, insect.transform.position);
            if (baseDist > baseHitboxRadius + insect.BaseHitboxRadius) continue;

            float visualDist = Vector3.Distance(visualPos, insect.GetAimPoint());
            if (visualDist > visualHitboxRadius + insect.VisualHitboxRadius) continue;

            RegisterHit(insect);
            if (hitCount > piercing) return; // about to be destroyed below, stop scanning
        }
    }

    private void RegisterHit(Insect insect)
    {
        hitCount++;
        if (hitCount == 2 && !DisablePierceFalloff) projectileDamage *= 0.5f;
        _alreadyHit.Add(insect);
        OnHit(insect);
        PlayHitParticles();
        PlayFloralGlowHitParticles();

        if (insect == trackedInsect)
        {
            trackedTarget = null;
            trackedInsect = null;
        }

        if (hitCount > piercing)
        {
            OnBeforeDestroy();
            Destroy(gameObject);
        }
    }

    protected virtual void OnHit(Insect insect)
    {
        //EMPTY METHOD INTENTIONAL
    }

    // override to stop the damage halving applied from the 2nd pierced hit onward (see
    // OnTriggerEnter2D) - used by skill-tree nodes that remove piercing's damage falloff
    protected virtual bool DisablePierceFalloff => false;

    // detaches every child particle system (a plant's own trail, Floral Glow's fly particle,
    // etc.) right before this projectile's GameObject is destroyed, so each one survives and
    // finishes fading out naturally instead of popping out of existence along with the parent.
    // Unity destroys children together with the parent, and OnDestroy() runs too late/unreliably
    // to rescue them once that's already underway, so this has to run first, while the hierarchy
    // is still fully intact - every destroy call site in this file calls it for exactly that
    // reason. still virtual so a subclass can extend it, but every projectile gets this for free
    // now rather than needing its own copy
    protected virtual void OnBeforeDestroy()
    {
        foreach (ParticleSystem trail in GetComponentsInChildren<ParticleSystem>())
        {
            // SetParent(null, true) would preserve world position by rewriting localScale to
            // cancel out the parent's own scale - but a particle system using Local Scaling Mode
            // reads localScale directly as its particle-size multiplier, so that rewrite would
            // make every particle instantly snap to a smaller size the moment it detaches.
            // reparenting with worldPositionStays: false leaves localScale untouched, so
            // position/rotation have to be restored manually instead
            Transform t = trail.transform;
            Vector3 worldPos = t.position;
            Quaternion worldRot = t.rotation;

            t.SetParent(null, false);
            t.position = worldPos;
            t.rotation = worldRot;

            trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(trail.gameObject, trail.main.startLifetime.constantMax);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // insect hits are now handled by HandleInsectHits() in Move() (Base+Visual dual check) -
        // this only needs to catch map-edge "Border" colliders now
        if (other.gameObject.CompareTag("Border"))
        {
            OnBeforeDestroy();
            Destroy(gameObject);
        }
    }

    // one-shot burst at the point of impact - null-safe, since not every projectile has a prefab
    // assigned yet. this prefab plays on its own (Play On Awake, non-looping) but doesn't
    // self-destroy (Stop Action: None), so it has to be cleaned up manually once it's done, sized
    // to whatever duration/lifetime are actually set on it
    protected void PlayHitParticles()
    {
        if (hitParticlePrefab == null) return;

        GameObject obj = Instantiate(hitParticlePrefab, visual.position, Quaternion.identity);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        float lifetime = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f;
        Destroy(obj, lifetime);
    }

    // Floral Glow: layered on top of whatever hit particle this projectile already has (if any),
    // same one-shot/independent-of-the-projectile pattern as PlayHitParticles above - read
    // generically off the effect, so it shows up regardless of which plant's projectile this is
    private void PlayFloralGlowHitParticles()
    {
        GameObject prefab = source?.GetEffect<FloralGlowEffect>()?.HitParticlePrefab;
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, visual.position, Quaternion.identity);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        float lifetime = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f;
        Destroy(obj, lifetime);
    }

    protected void PlaySound(AudioClip sound)
    {
        if (sound == null)
        {
            return;
        }

        GameObject tempAudio = new GameObject("TempAudio");
        tempAudio.transform.position = transform.position;
        AudioSource source = tempAudio.AddComponent<AudioSource>();
        source.clip = sound;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.spatialBlend = 0f;
        source.volume = hitVolume;
        source.Play();
        Destroy(tempAudio,sound.length/source.pitch);
    }

}
