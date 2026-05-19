using UnityEngine;
using System.Collections.Generic;

public class Calendula : Aura
{
    public float skillHealingMultiplier;

    private CalendulaData CData => data as CalendulaData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        skillHealingMultiplier = CData?.baseSkillHealingMultiplier ?? 0f;
    }

    protected override bool ShowLight => DarknessManager.instance != null && DarknessManager.instance.isDark;
    protected override bool ShowDarkCircle => false;

    public override void UpdateStats()
    {
        baseLightEmissionRange = 1.5f * (baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier));
        base.UpdateStats();
    }

    protected override void Update()
    {
        base.Update();

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else
            Attack();
    }

    protected override void Attack()
    {
        base.Attack();
        List<Insect> insects = GetInsectsInRange();
        foreach (Insect insect in insects)
            insect.Damage(attackDamage, DamageType.Magic, ElementalType.Fire, this, true,
                new DamageTag[] { DamageTag.AoE, DamageTag.PassiveDamage });
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return;
        int myLevel = effectivePath3Level + 1;
        FloralGlowEffect existing = targetPlant.GetEffect<FloralGlowEffect>();
        if (existing != null && existing.level > myLevel)
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed);
            return;
        }
        skillCooldownTimer = skillCooldown;
        targetPlant.ApplyEffect(new FloralGlowEffect(targetPlant, skillDuration, myLevel, this, this));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + 5f * level;
        baseFireDamage = 0.05f * level;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAttackRange = data.baseAttackRange + 0.175f * level;
    }

    public override void OnPath3Unlock()
    {
        skillCooldownTimer = 0f;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + 2f * level;
    }

    public override PlantBaseStats GetBaseStats() => new PlantBaseStats
    {
        attackDamage = 20f, attackSpeed = 0.5f, attackRange = 1.75f,
        skillCooldown = 15f, skillDuration = 10f,
        floralGlowHeal = 8f,
    };

    public override string GetName() => "<b><color=orange>Calendula</color></b>";
    public override string GetDescription() => $"The {GetName()} periodically releases waves of flaming petals and can infuse allies with fire energy.";
    public override string GetPath1Name() => "Petals";
    public override string GetPath2Name() => "Illuminate";
    public override string GetPath3Name() => "Floral Glow";
    public override string GetAttackDescription()
        => $"Releases flaming petals dealing <color=green><b>{attackDamage:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage to all insects within range.";

    public override string GetPassiveDescription()
        => $"Illuminate the surrounding area with a radius equal to <color=green><b>1.5×</b></color> her Attack Range.";

    public override string GetSkillDesription()
        => $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>{skillDuration:F0}s</b></color>. The plant's projectiles deal an additional <color=green><b>{attackDamage:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage on hit. Heals the plant for <color=green><b>{8f + 1f * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillHealingMultiplier * magicPower:F0}</b></color>] health per second. Emits light equal to <b><color=orange>Calendula</color></b>'s range.";

    public override string GetPath1Description()
        => $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Attack Damage by <color=green><b>5</b></color> per level. [<color=green><b>+{5 * effectivePath1Level}</b></color>]\n\n" +
           $"Increase Fire Damage by <color=green><b>5%</b></color> per level. [<color=green><b>+{5 * effectivePath1Level}%</b></color>]\n\n" +
           $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description()
        => $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease Attack Range by <color=green><b>0.175</b></color> per level. [<color=green><b>+{0.175 * effectivePath2Level}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description()
        => $"Skill:\n\n{GetSkillDesription()}\n\nScaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power (Damage)\n\nScaling: <color=#FFB6C1><b>{skillHealingMultiplier * 100f:F0}%</b></color> Magic Power (Healing)\n\nIncrease duration by <color=green><b>2</b></color> seconds per level. [<color=green><b>+{2 * effectivePath3Level}s</b></color>]\n\n" +
           $"Increase Healing per second by <color=green><b>1</b></color> per level. [<color=green><b>+{1 * effectivePath3Level}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
