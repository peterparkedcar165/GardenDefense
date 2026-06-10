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
            if (HasEffect<ShieldEffect>()) ApplyFrozenRageInRange();
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
        if (ShieldAmount > 0f)
            ApplyEffect(new HollyShieldEffect(this, skillDuration, 1, this, ShieldAmount));
    }

    private void ApplyFrozenRageInRange()
    {
        float r = Mathf.Clamp(FrozenRageReduction, 0f, 0.99f);
        float armorReduction = 100f * r / (1f - r);
        foreach (Insect insect in GetInsectsInRange())
            insect.ApplyEffect(new FrozenRageEffect(insect, EffectDuration, 1, this, this, armorReduction));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (HData?.path1AttackDamagePerLevel ?? 4f) * level;
        baseArmor        = data.baseArmor        + (HData?.path1ArmorPerLevel        ?? 8) * level;
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
        $"Increases Max Health by [<color=#FFB6C1><b>+{(HData?.baseHealthBonusMP ?? 0f) * magicPower:F0}</b></color>]. " +
        $"While {GetName()} is shielded, insects within range are afflicted with <color=#00FFFF><b>Frozen Rage</b></color>, " +
        $"forcing them to target {GetName()} and reducing their Armor by " +
        $"<color=green><b>{FrozenRageReductionBase * 100f / (1f - FrozenRageReductionBase):F1}</b></color> " +
        $"[<color=#FFB6C1><b>+{FrozenRageReductionMP * 100f / Mathf.Max(1f - FrozenRageReductionMP, 0.01f):F1}</b></color>]. " +
        $"Taunted insects deal <color=#FF6666><b>50%</b></color> less Attack damage.";

    public override string GetSkillDesription() =>
        $"Grants {GetName()} a shield of <color=grey><b>{ShieldAmount:F0}</b></color> [<color=#FFB6C1><b>+{ShieldAmountMP:F0}</b></color>] for <color=green><b>{skillDuration:F0}s</b></color>. " +
        $"While the shield holds, nearby insects are forced to target {GetName()} via <color=#00FFFF><b>Frozen Rage</b></color>.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl    = HData?.path1AttackDamagePerLevel ?? 4f;
        float armorpl = HData?.path1ArmorPerLevel        ?? 8;
        string scaling = details
            ? $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
              $"Increase Armor by <color=green><b>{armorpl:F0}</b></color> per level. [<color=green><b>+{armorpl * effectivePath1Level:F0}</b></color>]"
            : $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color>.\n\n" +
              $"Increase Armor by <color=green><b>{armorpl:F0}</b></color>.";
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float retalipl = HData?.path2RetaliationPerLevel ?? 0.05f;
        float hppl     = HData?.path2HealthPerLevel      ?? 40f;
        string scaling = details
            ? $"Increase retaliation percentages by <color=green><b>{retalipl * 100f:F0}%</b></color> per level for both. [<color=green><b>+{retalipl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
              $"Increase Max Health by <color=green><b>{hppl:F0}</b></color> per level. [<color=green><b>+{hppl * effectivePath2Level:F0}</b></color>]"
            : $"Increase retaliation percentages by <color=green><b>{retalipl * 100f:F0}%</b></color> for both.\n\n" +
              $"Increase Max Health by <color=green><b>{hppl:F0}</b></color>.";
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float shieldpl = HData?.path3ShieldPerLevel       ?? 20f;
        float ragepl   = HData?.path3FrozenRagePerLevel   ?? 0.04f;
        float durpl    = HData?.path3SkillDurationPerLevel ?? 2f;
        float rageArmorpl = ragepl * 100f / (1f - ragepl);
        string scaling = details
            ? $"Scaling: <color=#FFB6C1><b>{(HData?.baseFrozenRageReductionMP ?? 0f) * 100f:F0}%</b></color> Magic Power (Frozen Rage)\n\n<color=#FFB6C1><b>{(HData?.baseSkillShieldMP ?? 0f) * 100f:F0}%</b></color> Magic Power (Shield)\n\n" +
              $"Increase Armor reduction by <color=green><b>{rageArmorpl:F1}</b></color> per level. [<color=green><b>+{rageArmorpl * effectivePath3Level:F1}</b></color>]\n\n" +
              $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
              $"Increase shield by <color=green><b>{shieldpl:F0}</b></color> per level. [<color=green><b>+{shieldpl * effectivePath3Level:F0}</b></color>]"
            : $"Increase Armor reduction by <color=green><b>{rageArmorpl:F1}</b></color>.\n\n" +
              $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds.\n\n" +
              $"Increase shield by <color=green><b>{shieldpl:F0}</b></color>.";
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
