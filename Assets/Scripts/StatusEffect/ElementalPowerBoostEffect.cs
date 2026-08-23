public class elementalAffinityBoostEffect : StatusEffect
{
    public readonly float bonus;
    public readonly float magicPenFlatBonus;

    public elementalAffinityBoostEffect(Entity target, float duration, int level, Entity source, float bonus, float magicPenFlatBonus = 0f)
        : base(target, duration, level, source)
    {
        this.bonus = bonus;
        this.magicPenFlatBonus = magicPenFlatBonus;
        effectType      = Type.positive;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        target.elementalAffinityAdder += bonus;
        if (magicPenFlatBonus > 0f) target.magicPenFlatAdder += magicPenFlatBonus;
    }

    public override void OnExpire()
    {
        target.elementalAffinityAdder -= bonus;
        if (magicPenFlatBonus > 0f) target.magicPenFlatAdder -= magicPenFlatBonus;
    }

    public override string GetName() => "<color=green><b>Begonia's Blessing</b></color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Elemental Affinity</b></color> by <color=green><b>{bonus * 100f:F0}%</b></color>.";
        if (magicPenFlatBonus > 0f) desc += $" and <color=green><b>Magic Penetration</b></color> by <color=green><b>{magicPenFlatBonus:F0}</b></color>.";
        return desc;
    }
}
