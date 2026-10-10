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
                Vector3 aimPos = trackedInsect != null ? trackedInsect.GetAimPoint() : trackedTarget.transform.position;
                Vector3 toTarget = aimPos - transform.position;
                if (Vector3.Dot(direction, toTarget) > 0)
                    direction = toTarget.normalized;
                else
                    { trackedTarget = null; trackedInsect = null; }
            }
        }

        transform.position += direction * projectileSpeed * Time.deltaTime;

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

        if (other.CompareTag("Insect"))
        {
            Insect insect = other.GetComponentInParent<Insect>();
            if (insect != null && insect.IsAlive && insect.team != Team.Friendly)
            {
                hitCount++;
                if (hitCount == 2 && !DisablePierceFalloff) projectileDamage *= 0.5f;
                OnHit(insect);
                PlayHitParticles();
                PlayFloralGlowHitParticles();

                trackedTarget = null;
                trackedInsect = null;
                if (hitCount > piercing)
                {
                    OnBeforeDestroy();
                    Destroy(gameObject);
                }
            }

        }

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

        GameObject obj = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
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

        GameObject obj = Instantiate(prefab, transform.position, Quaternion.identity);
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
