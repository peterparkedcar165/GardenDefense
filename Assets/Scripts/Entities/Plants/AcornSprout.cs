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
        baseMaxRange = 7f;
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
            acorn.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, DamageType.Physical, ElementalType.Nature, this);
        }
    }

    public override void LevelUp()
    {
        base.LevelUp();
        int perLevel = (level - 1);
        baseAttackDamage = 7f + (perLevel * 0.25f);
        baseAttackSpeed = 0.8f + (perLevel * 0.08f);
        baseAttackRange = 3f + (perLevel * 0.2f);
        // baseProjectileSpeed = 8f + (perLevel * 0.2f);
    }
}
