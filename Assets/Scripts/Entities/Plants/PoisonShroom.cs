using UnityEngine;

public class PoisonShroom : Shooter
{
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = 4f;
        baseAttackSpeed = 0.6f;
        baseAttackRange = 3f;
        baseProjectileSpeed = 3f;
        basePiercing = 0;
        baseMaxRange = 7f;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        PoisonShroomProjectile puff = projectile.GetComponent<PoisonShroomProjectile>();

        if (puff != null)
        {
            puff.owner = this; // sets owner of projectile to this plant
            puff.Initialize(target, attackDamage * (1 + poisonDamage), projectileSpeed, maxRange, piercing, DamageType.Magic);
        }
    }

    public override void LevelUp()
    {
        base.LevelUp();
        int perLevel = (level - 1);
        baseAttackDamage = 4f + (perLevel * 0.3f);
        baseAttackSpeed = 0.6f + (perLevel * 0.04f);
        baseAttackRange = 3f + (perLevel * 0.2f);
        // baseProjectileSpeed = 8f + (perLevel * 0.2f);
    }
}
