using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip hit;
    [SerializeField] private float minPitch = 0.9f, maxPitch = 1.1f;
    [SerializeField] private float hitVolume = 0.5f;
    private int hitCount = 0;

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
        GetComponentInChildren<SpriteRenderer>().sortingOrder = 1;
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

        if (Vector3.Distance(transform.position, spawnPosition) >= maxRange)
        Destroy(gameObject);

    }

    protected virtual void OnHit(Insect insect)
    {
        //EMPTY METHOD INTENTIONAL
    }
    
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

                trackedTarget = null;
                trackedInsect = null;
                if (hitCount > piercing)
                    Destroy(gameObject);
            }

        }

        if (other.gameObject.CompareTag("Border"))
        Destroy(gameObject);
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
