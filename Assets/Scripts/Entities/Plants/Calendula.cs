using UnityEngine;
using System.Collections.Generic;

public class Calendula : Aura
{
    public float skillHealingMultiplier;
    private CalendulaData CData => data as CalendulaData;

    // total Floral Glow heal per second: base +/Lvl. + magic power scaling
    public float FloralGlowHealPerSecond =>
        (CData?.baseFloralGlowHeal ?? 8f) + (CData?.path3HealPerLevel ?? 1f) * effectivePath3Level
        + skillHealingMultiplier * magicPower;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        skillHealingMultiplier = CData?.baseSkillHealingMultiplier ?? 0f;
    }

    protected override bool ShowLight => DarknessManager.instance != null && (DarknessManager.instance.isDark || DarknessManager.instance.pitchBlack);
    protected override bool ShowDarkCircle => false;

    public override void UpdateStats()
    {
        baseLightEmissionRange = 2f * (baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier));
        base.UpdateStats();
    }

    protected override void Update()
    {
        base.Update();

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();
    }

    protected override void Attack()
    {
        base.Attack();
        List<Insect> insects = GetInsectsInRange();
        foreach (Insect insect in insects)
            insect.Damage(attackDamage, damageType, elementalType, this, true,
                new DamageTag[] { DamageTag.AoE, DamageTag.Attack });
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
    }

    private void OnTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return;
        int myLevel = effectivePath3Level + 1;
        FloralGlowEffect existing = targetPlant.GetEffect<FloralGlowEffect>();
        if (existing != null && existing.level > myLevel)
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
            return;
        }
        skillCooldownTimer = skillCooldown;
        targetPlant.ApplyEffect(new FloralGlowEffect(targetPlant, skillDuration, myLevel, this, this));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (CData?.path1AttackDamagePerLevel ?? 5f)  * level;
        baseFireDamage   = (CData?.path1FireDamagePerLevel ?? 0.05f) * level;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAttackRange = data.baseAttackRange + (CData?.path2AttackRangePerLevel ?? 0.175f) * level;
    }

    public override void OnPath3Unlock()
    {
        skillCooldownTimer = 0f;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (CData?.path3SkillDurationPerLevel ?? 2f) * level;
    }

    public override string GetName() => "<b><color=orange>Calendula</color></b>";
    public override string GetDescription() => $"The {GetName()} periodically releases waves of flaming petals and can infuse allies with fire energy.";
    public override string GetPath1Name() => "Petals";
    public override string GetPath2Name() => "Illuminate";
    public override string GetPath3Name() => "Floral Glow";

    public override string GetAttackDescription()
        => $"Releases flaming petals dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects within range.";

    public override string GetPassiveDescription()
        => $"Illuminate the surrounding area with a radius equal to <color=green><b>2Ã—</b></color> her Attack Range.";

    public override string GetSkillDesription()
    {
        float healpl = CData?.path3HealPerLevel ?? 1f;
        float healBase = CData?.baseFloralGlowHeal ?? 8f;
        return $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>{skillDuration:F0}s</b></color>. The plant's attacks deal an additional <color=green><b>{attackDamage:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage on hit. Heals the plant for <color=green><b>{healBase + healpl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillHealingMultiplier * magicPower:F0}</b></color>] health per second. Emits light equal to <b><color=orange>Calendula</color></b>'s Base Illumination range.";
    }

    public override string GetPath1Description(bool details = false)
    {
        float adpl   = CData?.path1AttackDamagePerLevel ?? 5f;
        float firepl = CData?.path1FireDamagePerLevel    ?? 0.05f;
        string desc = details
            ? $"Releases flaming petals dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects within range."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase Fire Damage by <color=green><b>{firepl * 100f:F0}%</b></color> per level. [<color=green><b>+{firepl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(effectivePath1Level)}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float rangepl = CData?.path2AttackRangePerLevel ?? 0.175f;
        string desc = details
            ? $"Illuminate the surrounding area with a radius equal to <color=green><b>2×</b></color> her Attack Range."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F3}</b></color> per level. [<color=green><b>+{rangepl * effectivePath2Level:F3}</b></color>]\n\n" +
               $"{Level5Section(effectivePath2Level)}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl  = CData?.path3SkillDurationPerLevel ?? 2f;
        float healpl = CData?.path3HealPerLevel           ?? 1f;
        float healBase = CData?.baseFloralGlowHeal ?? 8f;
        string desc = details
            ? $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. The plant's attacks deal an additional <color=green><b>[100% Attack Damage + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage on hit. Heals the plant for <color=green><b>[({healBase:F0}) + ({healpl:F0}/Lvl.) + <color=#FFB6C1>{skillHealingMultiplier * 100f:F0}% Magic Power</color>]</b></color> health per second. Emits light equal to <b><color=orange>Calendula</color></b>'s Base Illumination range."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase Healing per second by <color=green><b>{healpl:F0}</b></color> per level. [<color=green><b>+{healpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{Level5Section(effectivePath3Level)}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
