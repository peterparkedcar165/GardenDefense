using UnityEngine;

public class AcornSprout : Shooter
{
    public float stunChance, stunDuration, activeRadius, activeDamageMultiplier, acornBombHealth;
    [SerializeField] private GameObject acornBombPrefab;

    private AcornSproutData AcornData => data as AcornSproutData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        if (AcornData != null)
        {
            stunChance = AcornData.stunChance;
            stunDuration = AcornData.stunDuration;
        }
        activeDamageMultiplier = data.baseSkillDamageMultiplier;
        acornBombHealth        = data.baseSkillHealth;
        activeRadius           = data.baseSkillRadius;
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
        baseAttackDamage = data.baseAttackDamage + (level * 8f);
    }

    public override void OnPath2Upgrade(int level)
    {
        if (AcornData == null) return;
        stunChance   = AcornData.stunChance   + (0.05f * level);
        stunDuration = AcornData.stunDuration + (0.1f  * level);
    }

    public override void OnPath3Upgrade(int level)
    {
        activeDamageMultiplier = data.baseSkillDamageMultiplier + 0.25f * level;
        baseSkillDuration      = data.baseSkillDuration         + 2f    * level;
        acornBombHealth        = data.baseSkillHealth           + 50f   * level;
        activeRadius           = data.baseSkillRadius           * (1f + 0.15f * level);
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
        obj.GetComponent<AcornBomb>()?.Initialize(activeRadius, attackDamage * activeDamageMultiplier, skillDuration, acornBombHealth, this);
    }

    public override string GetDescription()
    {
        return $"The {GetName()} shoots acorns at targets, dealing damage with a chance of stunning.";
    }

    public override string GetPath1Description()
    {
        return $"Attack:\n\n" +
               $"Shoots acorns towards his target, dealing <color=green><b>{attackDamage}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage.\n\n" +
               $"Increase Base Attack Damage by <color=green><b>8</b></color> per level. [<color=green><b>+{8 * effectivePath1Level}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        return $"Passive:\n\n" +
               $"Attacks have a <color=green><b>{stunChance * 100f}%</b></color> chance to stun targets for <color=green><b>{stunDuration}</b></color> second.\n\n" +
               $"Increase Stun Chance by <b><color=green>5%</color></b> per level. [<b><color=green>+{5 * effectivePath2Level}%</color></b>]\n" +
               $"Increase Stun Duration by <b><color=green>0.1s</color></b> per level. [<b><color=green>+{0.1f * effectivePath2Level}s</color></b>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        return $"Skill:\n\n" +
               $"Hurls a giant acorn from the sky at a targeted location, dealing <color=green><b>{attackDamage * activeDamageMultiplier:F0}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage and stunning all insects in the impact radius for <color=green><b>2</b></color> seconds. " +
               $"The acorn then sits on the ground for <color=green><b>{skillDuration}</b></color> seconds, blocking ground insects who stop to gnaw at it. The acorn can sustain <color=green><b>{acornBombHealth:F0}</b></color> damage.\n\n" +
               $"Increase impact damage multiplier by <color=green><b>25%</b></color> per level. [<color=green><b>+{25 * effectivePath3Level}%</b></color>]\n\n" +
               $"Increase acorn lifetime by <color=green><b>2</b></color> seconds per level. [<color=green><b>+{2 * effectivePath3Level}s</b></color>]\n\n" +
               $"Increase acorn health by <color=green><b>50</b></color> per level. [<color=green><b>+{50 * effectivePath3Level}</b></color>]\n\n" +
               $"Increase acorn size and impact radius by <color=green><b>15%</b></color> per level. [<color=green><b>+{15 * effectivePath3Level}%</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
