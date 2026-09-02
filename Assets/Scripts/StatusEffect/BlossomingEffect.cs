using UnityEngine;

public class BlossomingEffect : StatusEffect
{
    private readonly float elementalEffectChanceBonus;
    private readonly float attackSpeedBonus;

    private const float MaxLevelMinimumDamageBonus = 0.2f;
    private bool _maxLevelBonusActive;

    public BlossomingEffect(Entity target, float duration, int level, Entity source, float elementalEffectChanceBonus, float attackSpeedBonus)
        : base(target, duration, level, source)
    {
        this.elementalEffectChanceBonus = elementalEffectChanceBonus;
        this.attackSpeedBonus = attackSpeedBonus;
        effectType      = Type.positive;
        elementalType   = ElementalType.Grass;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Blossoming", new Color(0.3f, 1f, 0.2f));
        target.elementalEffectChanceAdder += elementalEffectChanceBonus;
        target.attackSpeedMultiplier += attackSpeedBonus;

        _maxLevelBonusActive = source is Begonia beg && beg.path3Level >= Plant.absoluteLevelCap;
        if (_maxLevelBonusActive)
            target.minimumDamageAdder += MaxLevelMinimumDamageBonus;
    }

    public override void OnExpire()
    {
        target.elementalEffectChanceAdder -= elementalEffectChanceBonus;
        target.attackSpeedMultiplier -= attackSpeedBonus;
        if (_maxLevelBonusActive)
            target.minimumDamageAdder -= MaxLevelMinimumDamageBonus;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=green>Blossoming</color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Elemental Effect Chance</b></color> by <color=green><b>{elementalEffectChanceBonus * 100f:F0}%</b></color>, and <color=green><b>Attack Speed</b></color> by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color>.";
        if (_maxLevelBonusActive)
            desc += $" Also increases <color=green><b>Minimum Damage</b></color> by <color=green><b>{MaxLevelMinimumDamageBonus * 100f:F0}%</b></color>.";
        return desc;
    }
}
