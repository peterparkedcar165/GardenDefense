using UnityEngine;
using System.Collections.Generic;

public class Holly : Aura
{
    private HollyData HData => data as HollyData;

    private float _tickTimer;
    private const float TickInterval = 0.25f;
    private const float EffectDuration = 0.35f;

    private float PassiveHealthBonus =>
        (HData?.baseHealthBonusMP ?? 0f) * magicPower +
        (HData?.path2HealthPerLevel ?? 40f) * effectivePath2Level;

    public float RetaliationHollyPct  => (HData?.baseRetaliationHollyPercent  ?? 0.75f) + (HData?.path2RetaliationPerLevel ?? 0.05f) * effectivePath2Level;
    public float RetaliationInsectPct => (HData?.baseRetaliationInsectPercent ?? 0.75f) + (HData?.path2RetaliationPerLevel ?? 0.05f) * effectivePath2Level;

    private float FrozenRageReductionBase => (HData?.baseFrozenRageReduction   ?? 0.12f) + (HData?.path3FrozenRagePerLevel ?? 0.04f) * effectivePath3Level;
    private float FrozenRageReductionMP   => (HData?.baseFrozenRageReductionMP ?? 0f)    * magicPower / 100f;
    private float FrozenRageReduction     => FrozenRageReductionBase + FrozenRageReductionMP;

    private float ShieldAmountBase => (HData?.baseSkillShield ?? 0f) + (HData?.path3ShieldPerLevel ?? 20f) * effectivePath3Level;
    private float ShieldAmountMP   => (HData?.baseSkillShieldMP ?? 0f) * magicPower;
    private float ShieldAmount     => ShieldAmountBase + ShieldAmountMP;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        maxHealth += PassiveHealthBonus;
    }

    protected override void Update()
    {
        base.Update();

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();

        _tickTimer += Time.deltaTime;
        if (_tickTimer >= TickInterval)
        {
            _tickTimer -= TickInterval;
            if (HasEffect<TauntingEffect>()) ApplyFrozenRageInRange();
        }
    }

    protected override void Attack()
    {
        base.Attack();
        foreach (Insect insect in GetInsectsInRange())
            insect.Damage(attackDamage, damageType, elementalType, this, false, new DamageTag[] { DamageTag.AoE, DamageTag.Attack });
    }

    protected override void OnHitByInsect(Insect attacker)
    {
        if (!IsAlive || !attacker.IsAlive) return;
        float retaliationDamage = RetaliationHollyPct * attackDamage + RetaliationInsectPct * attacker.attackDamage;
        attacker.Damage(retaliationDamage, damageType, elementalType, this, false, new DamageTag[] { DamageTag.Melee, DamageTag.Counter, DamageTag.PassiveDamage });
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        ApplyEffect(new HollyTauntingEffect(this, skillDuration, 1, this));
        if (ShieldAmount > 0f)
            ApplyEffect(new HollyShieldEffect(this, skillDuration, 1, this, ShieldAmount));
    }

    private void ApplyFrozenRageInRange()
    {
        foreach (Insect insect in GetInsectsInRange())
            insect.ApplyEffect(new FrozenRageEffect(insect, EffectDuration, 1, this, this, FrozenRageReduction));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage      = data.baseAttackDamage       + (HData?.path1AttackDamagePerLevel          ?? 4f)    * level;
        basePhysicalResistance = data.basePhysicalResistance + (HData?.path1PhysicalResistancePerLevel ?? 0.04f) * level;
    }

    public override void OnPath2Upgrade(int level)
    {
        float hppl = HData?.path2HealthPerLevel ?? 40f;
        health += hppl;
        UpdateHealthBar();
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (HData?.path3SkillDurationPerLevel ?? 2f) * level;
    }

    public override string GetName() => "<b><color=#00FFFF>Holly</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a resilient ice tank that retaliates against attackers and can taunt insects into targeting her.";

    public override string GetAttackDescription() =>
        $"Releases icy thorns dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects within range.";

    public override string GetPassiveDescription() =>
        $"Insects that attack {GetName()} retaliate for <color=green><b>{RetaliationHollyPct * 100f:F0}%</b></color> of {GetName()}'s Attack Damage + " +
        $"<color=green><b>{RetaliationInsectPct * 100f:F0}%</b></color> of the attacker's Attack Damage. " +
        $"Increases Max Health by [<color=#FFB6C1><b>+{(HData?.baseHealthBonusMP ?? 0f) * magicPower:F0}</b></color>].";

    public override string GetSkillDesription() =>
        $"Enter a taunting state for <color=green><b>{skillDuration:F0}s</b></color>. Gains a <color=grey><b>{ShieldAmount:F0}</b></color> [<color=#FFB6C1><b>+{ShieldAmountMP:F0}</b></color>] shield for the duration. " +
        $"Insects within range are afflicted with <color=#00FFFF><b>Frozen Rage</b></color>, forcing them to target {GetName()} and reducing their Physical Resistance by " +
        $"<color=green><b>{FrozenRageReductionBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{FrozenRageReductionMP * 100f:F0}%</b></color>].";

    public override string GetPath1Description()
    {
        float adpl     = HData?.path1AttackDamagePerLevel          ?? 4f;
        float resistpl = HData?.path1PhysicalResistancePerLevel ?? 0.04f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase Physical Resistance by <color=green><b>{resistpl * 100f:F0}%</b></color> per level. [<color=green><b>+{resistpl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        float retalipl = HData?.path2RetaliationPerLevel ?? 0.05f;
        float hppl     = HData?.path2HealthPerLevel      ?? 40f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Increase retaliation percentages by <color=green><b>{retalipl * 100f:F0}%</b></color> per level for both. [<color=green><b>+{retalipl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase Max Health by <color=green><b>{hppl:F0}</b></color> per level. [<color=green><b>+{hppl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float shieldpl = HData?.path3ShieldPerLevel       ?? 20f;
        float ragepl   = HData?.path3FrozenRagePerLevel   ?? 0.04f;
        float durpl    = HData?.path3SkillDurationPerLevel ?? 2f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(HData?.baseFrozenRageReductionMP ?? 0f) * 100f:F0}%</b></color> Magic Power (Frozen Rage)\n\n<color=#FFB6C1><b>{(HData?.baseSkillShieldMP ?? 0f) * 100f:F0}%</b></color> Magic Power (Shield)\n\n" +
               $"Increase Physical Resistance reduction by <color=green><b>{ragepl * 100f:F0}%</b></color> per level. [<color=green><b>+{ragepl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"Increase shield by <color=green><b>{shieldpl:F0}</b></color> per level. [<color=green><b>+{shieldpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
