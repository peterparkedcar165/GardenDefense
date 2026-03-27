using UnityEngine;

public class Sunflower : Shooter
{
    protected override void Awake()
    {
        baseAttackDamage = 12f;
        baseAttackSpeed = 0.5f;
        baseAttackRange = 3f;
        baseProjectileSpeed = 3f;
        basePiercing = 0;
        baseMaxRange = 6f;
        base.Awake();
        // sun cost is set in inspector!
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        SunflowerProjectile petal = projectile.GetComponent<SunflowerProjectile>();

        if (petal != null)
        {
            petal.owner = this; // sets owner of projectile to this plant
            petal.SetTarget(FindTarget()); // assign the target of this plant to the projectile
            petal.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, DamageType.Magic);
        }
    }
    
    public override void LevelUp()
        {
            base.LevelUp();
            int perLevel = (level - 1);
            baseAttackDamage = 12f + (perLevel * 1f);
            baseAttackSpeed = 0.5f + (perLevel * 0.03f);
            baseAttackRange = 3f + (perLevel * 0.2f);
            baseProjectileSpeed = 3f + (perLevel * 0.3f);
        }
}
