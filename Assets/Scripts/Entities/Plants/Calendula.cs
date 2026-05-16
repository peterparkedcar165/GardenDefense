using UnityEngine;
using System.Collections.Generic;

public class Calendula : Aura
{
    protected override void Awake()
    {
        elementalType = ElementalType.Fire;
        damageType = DamageType.Magic;
        baseAttackDamage = 20f;
        baseAttackSpeed = 0.5f;
        baseAttackRange = 2.5f;
        baseSkillCooldown = 15f;
        baseSkillDuration = 10f;
        sunCost = 125;
        base.Awake();
    }

    protected override bool ShowLight => DarknessManager.instance != null && DarknessManager.instance.isDark;

    protected override void UpdateStats()
    {
        baseLightEmissionRange = baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier);
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
        FieryInfusionEffect existing = targetPlant.GetEffect<FieryInfusionEffect>();
        if (existing != null && existing.level > myLevel)
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed);
            return;
        }
        skillCooldownTimer = skillCooldown;
        targetPlant.ApplyEffect(new FieryInfusionEffect(targetPlant, skillDuration, myLevel, this, this));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = 20f + 5f * level;
        baseFireDamage = 0.05f * level;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAttackRange = 2.5f + 0.3f * level;
    }

    public override void OnPath3Unlock()
    {
        skillCooldownTimer = 0f;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = 10f + 2f * level;
    }

    public override string GetName() => "<b><color=orange>Calendula</color></b>";
    public override string GetDescription() => $"The {GetName()} periodically releases waves of flaming petals and can infuse allies with fire energy.";
    public override string GetPath1Name() => "Petals";
    public override string GetPath2Name() => "Illuminate";
    public override string GetPath3Name() => "Fiery Infusion";
    public override string GetAttackDescription()
        => $"Releases flaming petals dealing <color=green><b>{attackDamage:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage to all insects within range.";

    public override string GetPassiveDescription()
        => $"Illuminate the surrounding area with a radius equal to her Attack Range.";

    public override string GetSkillDesription()
        => $"Target a plant anywhere on the field to grant <color=orange>Fiery Infusion</color> for <color=green><b>{skillDuration:F0}s</b></color>. The plant's projectiles deal an additional <color=green><b>{attackDamage:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage on hit. Heals the plant for <color=green><b>{8f + 1f * effectivePath3Level:F0}</b></color> health per second. Emits light equal to <b><color=orange>Calendula</color></b>'s range.";

    public override string GetPath1Description()
        => $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Attack Damage by <color=green><b>5</b></color> per level. [<color=green><b>+{5 * effectivePath1Level}</b></color>]\n\n" +
           $"Increase Fire Damage by <color=green><b>5%</b></color> per level. [<color=green><b>+{5 * effectivePath1Level}%</b></color>]\n\n" +
           $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description()
        => $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease Attack Range by <color=green><b>0.3</b></color> per level. [<color=green><b>+{0.3 * effectivePath2Level}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description()
        => $"Skill:\n\n{GetSkillDesription()}\n\nIncrease duration by <color=green><b>2</b></color> seconds per level. [<color=green><b>+{2 * effectivePath3Level}s</b></color>]\n\n" +
           $"Increase Healing per second by <color=green><b>1</b></color> per level. [<color=green><b>+{1 * effectivePath3Level}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
