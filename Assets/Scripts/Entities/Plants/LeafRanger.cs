using UnityEngine;

public class LeafRanger : Shooter
{
    private float 
    bAD = 90f, // base attack damage
    bAS = 0.3f, // base attack speed
    bAR = 99f, // base attack range
    bPS = 20f, // base projectile speed
    bMR = 20f; // base max range
    private int bP = 0; // base piercing
    public float activeDuration = 3f, activeRadius = 2f;

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
        basePiercing = bP + effectivePath1Level;
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        LeafRangerProjectile arrow = projectile.GetComponent<LeafRangerProjectile>();

        if (arrow != null)
        {
            arrow.SetTarget(FindTarget());
            arrow.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }
        public override void OnPath1Upgrade(int level)
    {
        baseCriticalChance = 0.05f + 0.05f*level;
        // piercing taken care of in update
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAttackSpeed = bAS + (0.05f*level);
    }

    public override void OnPath3Upgrade(int level)
    {
        activeDuration = 3f + (level * 0.5f);
        activeRadius = 3f + (level * 0.3f);
    }


    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=green>Leaf Ranger</color></b>";
    }

    public override string GetDescription()
    {
        return $"No insect is out of reach. The {GetName()} has claimed the whole garden as his territory, and his arrows always find their mark.";
    }

    public override string GetAttackDescription()
    {
        return $"The {GetName()} shoots slow but precise and fierce arrows at his target, dealing <color=green>Nature</color> <color=brown>Physical</color> damage.";
    }

    public override string GetSkillDesription()
    {
        return $"The {GetName()} releases a trap that imprisons insects within its area, dealing <color=green>Nature</color> <color=brown>Physical</color> damage upon impact.";
    }

    public override string GetPassiveDescription()
    {
        return $"The {GetName()}'s arrows pierce his targets, and have increased critical chance.";
    }
}
