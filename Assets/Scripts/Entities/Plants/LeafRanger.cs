using UnityEngine;

public class LeafRanger : Shooter
{
    private float 
    bAD = 72f, // base attack damage
    bAS = 0.4f, // base attack speed
    bAR = 99f, // base attack range
    bPS = 20f, // base projectile speed
    bMR = 20f; // base max range
    private int bP = 1; // base piercing

    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
    }

    protected override void Update()
    {
        base.Update();
        basePiercing = bP + passiveLevel;
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        LeafRangerProjectile arrow = projectile.GetComponent<LeafRangerProjectile>();

        if (arrow != null)
        {
            arrow.SetTarget(FindTarget());
            arrow.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, DamageType.Physical, ElementalType.Nature, this);
        }
    }

    public override void LevelUp()
    {
        base.LevelUp();
        int perLevel = (level - 1);
        baseAttackDamage = bAD + (perLevel * 1f);
        baseAttackSpeed = bAS + (perLevel * 0.02f);
        baseAttackRange = bAR + (perLevel * 0f);
        baseProjectileSpeed = bPS + (perLevel * 0.2f);
    }
}
