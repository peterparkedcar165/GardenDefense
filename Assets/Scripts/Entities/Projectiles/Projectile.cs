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
    private float _visualHeightVelocity; // derived each frame for VisualHeading's rotation use
    // protected rather than private so a subclass with its own post-hit behavior (e.g.
    // LeafRangerProjectile's bounce-redirect) can still search "every live insect I haven't hit
    // yet" using the exact same detection/record-keeping everyone else goes through
    protected readonly HashSet<Insect> _alreadyHit = new HashSet<Insect>();

    // light emission: either the firing plant currently has Floral Glow active (every
    // projectile glows while that buff is up, same as Calendula's own FloralGlowProjectile
    // already did on its own), or the plant has its own innate "Projectile Emit Light" ability
    // (PlantData checkbox, e.g. Sunflower) - decided once at spawn in Initialize(), same
    // one-shot-setup convention FloralGlowProjectile itself uses rather than re-checking every
    // frame for a buff that's very unlikely to change mid-flight
    private Transform _lightObj;
    private LightFader _lightFader;
    private const float LightFalloffStrength = 0.5f;
    private const float LightFadeDuration = 0.3f;

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
        // parented to Visual (not Base/this transform), same reasoning as SetupLight() below:
        // Base never leaves the ground plane, so a trail stuck to it would visibly detach from
        // the sprite while climbing toward a flying target. left to be destroyed along with it,
        // same as any other purely cosmetic trail (only the one-shot hit burst below needs to
        // outlive it)
        GameObject flyPrefab = this.source?.GetEffect<FloralGlowEffect>()?.FlyParticlePrefab;
        if (flyPrefab != null)
            Instantiate(flyPrefab, visual.position, visual.rotation, visual);

        SetupLight();
    }

    // see the field comments above for the two conditions this checks. exactly one Light2D is
    // ever created per projectile, never two/stacked - if both conditions apply at once (e.g. a
    // Sunflower with his own innate Projectile Emit Light also catching a Floral Glow buff),
    // radius+color+intensity are resolved as a single set BEFORE anything is instantiated:
    // whichever radius is larger wins outright, color and intensity included, rather than
    // creating a light for each and layering them (which would double the effective brightness).
    // both conditions default to the same DefaultProjectileLightRadius/Intensity - Floral Glow
    // intentionally does NOT scale with source.lightEmissionRange (that's the plant's own, often
    // much larger, self-glow stat); every projectile's glow stays a small, consistent size
    // regardless of which plant or buff is behind it, only the color/intensity differ (plain
    // white at default intensity for Floral Glow, PlantData's own projectileLightColor/
    // projectileLightIntensity for the innate ability)
    private const float DefaultProjectileLightRadius = 0.5f;
    private const float DefaultProjectileLightIntensity = 0.4f;

    private void SetupLight()
    {
        if (DarknessManager.instance == null || source == null) return;

        float radius = 0f;
        float intensity = DefaultProjectileLightIntensity;
        Color color = Color.white;
        if (source.data != null && source.data.projectileEmitLight)
        {
            radius = source.data.projectileLightRadius;
            intensity = source.data.projectileLightIntensity;
            color = source.data.projectileLightColor;
        }
        if (source.HasEffect<FloralGlowEffect>() && DefaultProjectileLightRadius > radius)
        {
            radius = DefaultProjectileLightRadius;
            intensity = DefaultProjectileLightIntensity;
            color = Color.white;
        }
        if (radius <= 0f) return;

        // parented to Visual (not Base/this transform) so the glow actually sits where the
        // sprite visually is, including mid-flight while climbing toward a flying target - Base
        // never leaves the ground plane, so a light stuck to it would visibly detach from the
        // rising sprite. visual already falls back to this transform itself on any prefab
        // without a real Visual child, so this is always safe
        GameObject lightGO = new GameObject("ProjectileLight");
        lightGO.transform.SetParent(visual, false);
        lightGO.transform.localPosition = Vector3.zero;

        var light = lightGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = color;
        light.intensity = 0f;
        light.falloffIntensity = LightFalloffStrength;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.3f;

        _lightFader = lightGO.AddComponent<LightFader>();
        _lightFader.Setup(light, intensity);
        _lightFader.FadeIn(LightFadeDuration);

        _lightObj = lightGO.transform;
        DarknessManager.RegisterLightSource(_lightObj, radius);
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

        // think of Base's own position as the "XZ" plane and the height offset below as the
        // real "Y" - projectileSpeed is the hypotenuse of a right triangle whose two legs are
        // however far Base still has to go on the ground (groundDist) and however big the
        // height gap still is (heightGap). splitting projectileSpeed across both legs in that
        // same ratio means the combined/diagonal speed of the actual visible shot is always
        // exactly projectileSpeed (never faster just because it's also climbing), AND both legs
        // always finish at the same moment by construction - fixing the old desync where Base
        // could cross the target's ground footprint (satisfying the base-distance half of the
        // hit check) before Visual had risen enough to satisfy the visual-distance half, or vice
        // versa: a clean miss despite visually crossing the same space at some point
        float groundSpeed = projectileSpeed;
        float heightRate = 0f;
        float targetHeight = 0f;
        if (trackedInsect != null)
        {
            targetHeight = trackedInsect.GetAimPoint().y - trackedInsect.transform.position.y;
            float groundDist = Vector3.Distance(transform.position, trackedInsect.transform.position);
            float heightGap = targetHeight - _visualHeightOffset;
            float totalRemaining = Mathf.Sqrt(groundDist * groundDist + heightGap * heightGap);
            if (totalRemaining > 0.0001f)
            {
                groundSpeed = projectileSpeed * (groundDist / totalRemaining);
                heightRate = projectileSpeed * (heightGap / totalRemaining); // signed: negative while descending
            }
        }

        transform.position += direction * groundSpeed * Time.deltaTime;

        // cosmetic-only: Visual rises to mirror however high the tracked target currently is, so
        // the rendered sprite still visually arcs up to meet a flying insect even though Base
        // (this root) never leaves the ground plane. Shadow always stays glued to Base. both read
        // off the ORIGINAL authored local offset via TransformPoint (not plain parenting) so a
        // rotating root doesn't swing them around - see the field comments above. once the
        // target's gone (died, out of range, already hit), the offset is simply left exactly
        // where it is and never touched again instead of being pulled back to 0 - so a shot that
        // was flying up at a bird keeps flying at that same height in a straight line, no sudden
        // change in vertical motion
        if (trackedInsect != null)
        {
            _visualHeightOffset = Mathf.MoveTowards(_visualHeightOffset, targetHeight, Mathf.Abs(heightRate) * Time.deltaTime);
            _visualHeightVelocity = heightRate;
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
        // snapshot first: RegisterHit() below can kill the insect synchronously (OnHit -> Damage),
        // which removes it from the live allInsects list via its own OnDestroy - mutating the
        // exact list this loop is enumerating and throwing InvalidOperationException otherwise
        foreach (Insect insect in Insect.allInsects.ToArray())
        {
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly) continue;
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

        OnAfterHit(insect);
    }

    // runs after every registered hit, detection method aside. default: once piercing is used up
    // (hitCount exceeds it), destroy - a normal pierce-through shot. LeafRangerProjectile
    // overrides this to redirect toward its next bounce target instead of just dying, while still
    // going through the exact same Base+Visual detection every other projectile uses
    protected virtual void OnAfterHit(Insect insect)
    {
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

        // same detach-and-outlive treatment as the particle trails above, but for the light (if
        // any was set up): unregister immediately so darkness stops accounting for it right away,
        // then let it fade out in place instead of popping off instantly alongside the projectile
        if (_lightObj != null)
        {
            DarknessManager.UnregisterLightSource(_lightObj);
            Vector3 worldPos = _lightObj.position;
            _lightObj.SetParent(null, false);
            _lightObj.position = worldPos;
            _lightFader?.FadeOut(LightFadeDuration, destroyOnComplete: true);
            _lightObj = null;
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
