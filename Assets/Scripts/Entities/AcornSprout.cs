using UnityEngine;

public class AcornSprout : Shooter
{
    protected override void Awake()
    {
        base.Awake();
        attackDamage = 7f;
        attackSpeed = 0.8f;
        attackRange = 3f;
        projectileSpeed = 8f;
        piercing = 0;
        maxRange = 3f;
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
            acorn.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, DamageType.Physical); 
        }
    }
}
