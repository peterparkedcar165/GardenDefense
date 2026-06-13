public class elementalAffinityBoostEffect : StatusEffect
{
    public readonly float bonus;
    public readonly float armorPenFlatBonus;

    public elementalAffinityBoostEffect(Entity target, float duration, int level, Entity source, float bonus, float armorPenFlatBonus = 0f)
        : base(target, duration, level, source)
    {
        this.bonus = bonus;
        this.armorPenFlatBonus = armorPenFlatBonus;
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        target.elementalAffinityAdder += bonus;
        if (armorPenFlatBonus > 0f) target.armorPenFlatAdder += armorPenFlatBonus;
    }

    public override void OnExpire()
    {
        target.elementalAffinityAdder -= bonus;
        if (armorPenFlatBonus > 0f) target.armorPenFlatAdder -= armorPenFlatBonus;
    }

    public override string GetName() => "<color=green><b>Begonia's Blessing</b></color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Elemental Affinity</b></color> by <color=green><b>{bonus * 100f:F0}%</b></color>.";
        if (armorPenFlatBonus > 0f) desc += $" and <color=#A0522D><b>Armor Penetration</b></color> by <color=#A0522D><b>{armorPenFlatBonus:F0}</b></color>.";
        return desc;
    }
}
