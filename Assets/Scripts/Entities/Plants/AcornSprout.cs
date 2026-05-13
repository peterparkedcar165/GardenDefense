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
    [SerializeField] private GameObject acornBombPrefab;

    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        basePiercing = bP;
        activeRadius = 1f;
        baseSkillCooldown = 1f;
        baseSkillDuration = 20f;
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
        activeDamageMultiplier = 1.5f + 0.1f * level;
        baseSkillDuration = 20f + 2f * level;
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
        return $"Shoots acorns towards his target, dealing <color=green><b>{attackDamage}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage.";
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(activeRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (acornBombPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        GameObject obj = Instantiate(acornBombPrefab, position, Quaternion.identity);
        obj.GetComponent<AcornBomb>()?.Initialize(activeRadius, attackDamage * activeDamageMultiplier, skillDuration, this);
    }

    public override string GetSkillDesription()
    {
        return $"Hurls a giant acorn from the sky at a targeted location, dealing <color=green><b>{attackDamage * activeDamageMultiplier:F0}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage and stunning all insects in the impact radius for <color=green><b>2</b></color> seconds. The acorn then sits on the ground for <color=green><b>30</b></color> seconds, blocking ground insects who stop to gnaw at it.";
    }

    public override string GetPassiveDescription()
    {
        return $"Attacks have a <color=green><b>{stunChance*100}%</b></color> chance to stun targets for <color=green><b>{stunDuration}</b></color> second.";
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
        return $"Skill:\n\n{GetSkillDesription()}\n\nIncrease impact damage multiplier by <color=green><b>10%</b></color> per level. [<color=green><b>x{activeDamageMultiplier:F1}</b></color>]\n\n" +
        $"Increase acorn lifetime by <color=green><b>2</b></color> seconds per level. [<color=green><b>{skillDuration}s</b></color>]\n\n" +
        "Level: [<color=green><b>" + path3Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath3Level - path3Level) + ")</b></color>";
    }
}
