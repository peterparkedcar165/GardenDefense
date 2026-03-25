using UnityEngine;

public class AcornSprout : Shooter
{
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = 7f;
        baseAttackSpeed = 0.8f;
        baseAttackRange = 3f;
        baseProjectileSpeed = 8f;
        basePiercing = 0;
        baseMaxRange = 3f;
    }

    protected override void Update()
    {
        base.Update();
    }
    
    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        AcornProjectile acorn = projectile.GetComponent<AcornProjectile>();

        if (acorn != null)
        {
            acorn.owner = this; // sets owner of projectile to this plant
            acorn.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, DamageType.Physical); 
        }
    }
}
