public class ElementalPowerBoostEffect : StatusEffect
{
    public readonly float bonus;

    public ElementalPowerBoostEffect(Entity target, float duration, int level, Entity source, float bonus)
        : base(target, duration, level, source)
    {
        this.bonus = bonus;
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        target.elementalPowerAdder += bonus;
    }

    public override void OnExpire()
    {
        target.elementalPowerAdder -= bonus;
    }

    public override string GetName() => "<color=green><b>Begonia's Blessing</b></color>";
    public override string GetDescription() => $"Elemental Power increased by <color=green><b>{bonus * 100f:F0}%</b></color>.";
}
