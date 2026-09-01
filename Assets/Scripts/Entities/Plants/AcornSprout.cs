using UnityEngine;

public class AcornSprout : Shooter
{
    public float stunChance, activeRadius, activeDamageMultiplier, acornBombHealth;
    [SerializeField] private GameObject acornBombPrefab;

    private AcornSproutData AcornData => data as AcornSproutData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        if (AcornData != null)
        {
            stunChance = AcornData.stunChance;
            basePassiveDuration = AcornData.stunDuration;
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
            if (IsPath2Maxed)
                acorn.SetBounces(piercing);
            acorn.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        if (IsPath1Maxed)
            attackDamage += armor * 0.33f;
        if (IsPath2Maxed)
            piercing += 1;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * (AcornData?.path1AttackDamagePerLevel ?? 8f);
        baseAttackSpeed  = data.baseAttackSpeed  + level * (AcornData?.path1AttackSpeedPerLevel  ?? 0.05f);
        baseArmor        = data.baseArmor        + level * (AcornData?.path1ArmorPerLevel        ?? 4);
    }

    public override void OnPath2Upgrade(int level)
    {
        if (AcornData == null) return;
        stunChance          = AcornData.stunChance   + AcornData.path2StunChancePerLevel   * level;
        basePassiveDuration = AcornData.stunDuration + AcornData.path2StunDurationPerLevel * level;
    }

    public override void OnPath3Upgrade(int level)
    {
        float dmgPerLevel    = AcornData?.path3DamageMultiplierPerLevel ?? 0.25f;
        float durPerLevel    = AcornData?.path3SkillDurationPerLevel    ?? 2f;
        float hpPerLevel     = AcornData?.path3HealthPerLevel           ?? 50f;
        float radiusPerLevel = AcornData?.path3RadiusPerLevel           ?? 0.15f;
        activeDamageMultiplier = data.baseSkillDamageMultiplier + dmgPerLevel * level;
        baseSkillDuration      = data.baseSkillDuration         + durPerLevel * level;
        acornBombHealth        = data.baseSkillHealth           + hpPerLevel  * level;
        activeRadius           = data.baseSkillRadius           * (1f + radiusPerLevel * level);
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

    public override string GetName() => $"<b><color=green>{(data != null ? data.displayName : "Acorn Sprout")}</color></b>";

    public override string GetDescription()
    {
        return $"The {GetName()} shoots acorns at targets, dealing damage with a chance of stunning.";
    }

    public override string GetPath1Description(bool details = false)
    {
        float adpl = AcornData?.path1AttackDamagePerLevel ?? 8f;
        float aspl = AcornData?.path1AttackSpeedPerLevel  ?? 0.05f;
        int   armorpl = AcornData?.path1ArmorPerLevel     ?? 4;
        string desc = details
            ? $"Shoots acorns towards his target, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : $"Shoots acorns towards his target, dealing <color=green><b>{attackDamage}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=#00CED1><b>Armor</b></color> by <color=green><b>{armorpl}</b></color> per level. [<color=green><b>+{armorpl * effectivePath1Level}</b></color>]\n\n" +
               $"{Level5Section(path1Level, details ? "Increase Attack Damage by 33% of <color=#00CED1><b>Armor</b></color>." : $"Increase Attack Damage by <color=#00CED1><b>{armor * 0.33f:F0}</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float chanceBase = AcornData?.stunChance               ?? 0.05f;
        float durBase    = AcornData?.stunDuration             ?? 0.5f;
        float chancepl   = AcornData?.path2StunChancePerLevel   ?? 0.05f;
        float durpl      = AcornData?.path2StunDurationPerLevel ?? 0.1f;
        string desc = details
            ? $"Attacks have a <color=green><b>[({chanceBase * 100f:F0}) + ({chancepl * 100f:F0}/Lvl.)]</b></color>% chance to stun targets for <color=green><b>[({durBase:F1}) + ({durpl:F1}/Lvl.)]</b></color> seconds."
            : $"Attacks have a <color=green><b>{stunChance * 100f}%</b></color> chance to stun targets for <color=green><b>{passiveDuration:F1}</b></color> seconds.";
        string passiveMaxBonus = "Attacks' <color=green><b>Piercing</b></color> instead bounce to nearby targets.\n\nIncrease <color=green><b>Piercing</b></color> by <color=green><b>1</b></color>.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Stun Chance</b></color> by <color=green><b>{chancepl * 100f:F0}%</b></color> per level. [<color=green><b>+{chancepl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Stun Duration</b></color> by <color=green><b>{durpl:F1}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path2Level, passiveMaxBonus)}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float dmgpl    = AcornData?.path3DamageMultiplierPerLevel ?? 0.25f;
        float durpl    = AcornData?.path3SkillDurationPerLevel    ?? 2f;
        float hppl     = AcornData?.path3HealthPerLevel           ?? 50f;
        float radiuspl = AcornData?.path3RadiusPerLevel           ?? 0.15f;
        string skillMaxBonus = details
            ? "Whenever the <color=green><b>Acorn</b></color> is healed, its lifetime is extended by 2% of the healing amount, in seconds.\n\nThe <color=green><b>Acorn</b></color> inherits the Acorn Sprout's <color=#00CED1><b>Armor</b></color>."
            : $"Whenever the <color=green><b>Acorn</b></color> is healed, its lifetime is extended by 2% of the healing amount, in seconds.\n\nThe <color=green><b>Acorn</b></color> gains <color=#00CED1><b>{armor:F0} Base Armor</b></color>.";
        string desc = details
            ? $"Hurls a giant <color=green><b>Acorn</b></color> from the sky at a targeted location, dealing <color=green><b>[{activeDamageMultiplier * 100f:F0}% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage and stunning all insects in the impact radius for <color=green><b>2</b></color> seconds. " +
              $"The <color=green><b>Acorn</b></color> then sits on the ground for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds, blocking ground insects who stop to gnaw at it. The <color=green><b>Acorn</b></color> has <color=green><b>[({data.baseSkillHealth:F0}) + ({hppl:F0}/Lvl.)]</b></color> health."
            : $"Hurls a giant <color=green><b>Acorn</b></color> from the sky at a targeted location, dealing <color=green><b>{attackDamage * activeDamageMultiplier:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage and stunning all insects in the impact radius for <color=green><b>2</b></color> seconds. " +
              $"The <color=green><b>Acorn</b></color> then sits on the ground for <color=green><b>{skillDuration}</b></color> seconds, blocking ground insects who stop to gnaw at it. The <color=green><b>Acorn</b></color> has <color=green><b>{acornBombHealth:F0}</b></color> health.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase impact damage multiplier by <color=green><b>{dmgpl * 100f:F0}%</b></color> per level. [<color=green><b>+{dmgpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Acorn</b></color> lifetime by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Acorn</b></color> health by <color=green><b>{hppl:F0}</b></color> per level. [<color=green><b>+{hppl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Acorn</b></color> size and impact radius by <color=green><b>{radiuspl * 100f:F0}%</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, skillMaxBonus)}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
