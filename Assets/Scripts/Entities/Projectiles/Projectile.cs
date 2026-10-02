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

    // hook for a subclass to detach/release a child effect (e.g. a particle trail) right before
    // this projectile's GameObject is destroyed. Unity destroys children along with the parent,
    // and OnDestroy() runs too late/unreliably to rescue them once that's already underway, so
    // this fires first, while the hierarchy is still fully intact
    protected virtual void OnBeforeDestroy() { }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Insect"))
        {
            Insect insect = other.GetComponentInParent<Insect>();
            if (insect != null && insect.IsAlive && insect.team != Team.Friendly)
            {
                hitCount++;
                if (hitCount == 2) projectileDamage *= 0.5f;
                OnHit(insect);
                PlayHitParticles();

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
