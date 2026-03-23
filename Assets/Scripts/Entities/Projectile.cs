using UnityEngine;

public abstract class Projectile : MonoBehaviour
{

    public float projectileDamage, projectileSpeed, maxRange;
    public int piercing;
    public DamageType damageType;
    protected Vector3 direction;
    protected Vector3 spawnPosition;

    public virtual void Initialize(GameObject target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType)
    {
        direction = (target.transform.position - transform.position).normalized;
        this.projectileDamage = projectileDamage;
        this.projectileSpeed = projectileSpeed;
        this.piercing = piercing;
        this.damageType = damageType;
        this.maxRange = maxRange;
        this.spawnPosition = transform.position;
    }

    protected virtual void Update()
    {
        Move();
    }

    protected virtual void Move()
    {
        // travels in fixed direction, never updates
        transform.position += direction * projectileSpeed * Time.deltaTime;
        if (Vector3.Distance(transform.position, spawnPosition) >= maxRange)
        {
            Debug.Log(gameObject + " deleted due to reaching max range");
            Destroy(gameObject);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Insect"))
        {
            Entity entity = other.GetComponent<Entity>();
            if (entity != null)
            {
                entity.Damage(projectileDamage, damageType);
            }

            if (piercing > 0)
            {
                piercing--;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    
}
