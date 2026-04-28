using UnityEngine;

public abstract class Projectile : MonoBehaviour
{

    public float projectileDamage, projectileSpeed, maxRange;
    public int piercing;
    public DamageType damageType;
    public ElementalType elementalType;
    protected Vector3 direction;
    protected Vector3 spawnPosition;
    public Plant source; // will be set by the plant who fires this projectile

    protected GameObject trackedTarget; // keeps reference to the tracked target

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

    protected virtual void Update()
    {
        Move();
    }

    public void SetTarget(GameObject target)
    {
        trackedTarget = target;

    }
    protected virtual void Move()
    {
        if (trackedTarget != null)
        {
            Vector3 toTarget = trackedTarget.transform.position - transform.position;
            if (Vector3.Dot(direction, toTarget) > 0)
                direction = toTarget.normalized;
            else
                trackedTarget = null;
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
            Insect insect = other.GetComponent<Insect>();
            if (insect != null)
            {
                OnHit(insect);
            }
            
        }

        if (other.gameObject.CompareTag("Border"))
        Destroy(gameObject);
    }

}
