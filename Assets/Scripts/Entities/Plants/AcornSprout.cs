using UnityEngine;

public class AcornSprout : Shooter
{
    private float 
    bAD = 33f, // base attack damage
    bAS = 0.8f, // base attack speed
    bAR = 3f, // base attack range
    bPS = 8f, // base projectile speed
    bMR = 20f; // base max range
    private int bP = 0; // base piercing
    public float stunChance = 0.25f, stunDuration = 0.5f, activeRadius, activeDamageMultiplier = 1.5f;
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        basePiercing = bP;
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
            acorn.SetTarget(FindTarget());
            acorn.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = bAD + (level * 8f);
    }

    public override void OnPath2Upgrade(int level)
    {
        stunChance = 0.25f + (0.05f * level);
        stunDuration = 0.5f + (0.1f * level);
    }

    public override void OnPath3Upgrade(int level)
    {
        activeDamageMultiplier = 1.5f + 0.1f*(level -1);
    }


    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=green>Acorn Sprout</color></b>";
    }

    public override string GetDescription()
    {
        return "A feisty little fighter that hurls hardened acorns at enemies with surprising force.";
    }

    public override string GetAttackDescription()
    {
        return $"The {GetName()} shoots acorns towards his target, dealing <color=green>Nature</color> <color=brown>Physical</color> damage.";
    }

    public override string GetSkillDesription()
    {
        return $"The {GetName()} fires a bursting acorn that breaks apart, dealing <color=green>Nature</color> <color=brown>Physical</color> damage to targets struck by it, and leaving irresistible morsels that lure insects towards it.";
    }

    public override string GetPassiveDescription()
    {
        return $"The {GetName()}'s acorn shots have a chance to briefly stun his target.";
    }
}
