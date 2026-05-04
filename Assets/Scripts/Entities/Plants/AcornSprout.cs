using UnityEngine;
using UnityEngine.InputSystem;

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
        return $"The {GetName()} shoots acorns at targets, dealing damage with a chance of stunning.";
    }

    public override string GetAttackDescription()
    {
        return $"Shoots acorns towards his target, dealing <color=green>{attackDamage}</color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage.";
    }

    public override string GetSkillDesription()
    {
        return $"The {GetName()} fires a bursting acorn that breaks apart, dealing <color=green>Nature</color> <color=#A0522D>Physical</color> damage to targets struck by it, and leaving irresistible morsels that lure insects towards it.";
    }

    public override string GetPassiveDescription()
    {
        return $"Attacks have a <color=green>{stunChance*100}%</color> chance to stun targets for <color=green>{stunDuration}</color> second.";
    }

    public override string GetPath1Description()
    {
        return $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Attack Damage by <color=green><b>8</b></color> per level. [<color=green><b>+" + (8*effectivePath1Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path1Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath1Level-path1Level) + ")</b></color>"; 
    }

    public override string GetPath2Description()
    {
        return $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease Stun Chance by <b><color=green>5%</color></b> per level. [<b><color=green>+" + (5 * effectivePath2Level) + "%</color></b>]\n" +
        "Increase Stun Duration by <b><color=green>0.1s</color></b> per level. [<b><color=green>+" + (0.1f * effectivePath2Level) + "s</color></b>]\n\n" +
        "Level: [<color=green><b>" + path2Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath2Level-path2Level) + ")</b></color>";
    }

    public override string GetPath3Description()
    {
        return "";
    }
}
