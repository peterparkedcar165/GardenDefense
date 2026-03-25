using UnityEngine;

public class SunflowerProjectile : Projectile
{

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType);
    }

    protected override void Update()
    {
        base.Update();
    }

    // override move so we can home

    private GameObject trackedTarget; // keep reference to the actual target object

    public void SetTarget(GameObject target)
    {
        trackedTarget = target;
    }
    protected override void Move()
    {
        if (trackedTarget == null)
        {
            base.Move();
            return;
        }

        direction = (trackedTarget.transform.position - transform.position).normalized;
        transform.position += direction * projectileSpeed * Time.deltaTime;


        // while homing, if the distance of the projectile exceeds the max range
        // destroy, but idk if iw ant that so i comment it
        //if (Vector3.Distance(transform.position, spawnPosition) >= maxRange)
        //{
        //    Destroy(gameObject);
        //}
    }  
}
