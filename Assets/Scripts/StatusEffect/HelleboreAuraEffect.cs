public class HelleboreAuraEffect : StatusEffect
{
    private readonly float resistBonus;

    public HelleboreAuraEffect(Entity target, float duration, int level, Entity source, float resistBonus)
        : base(target, duration, level, source)
    {
        this.resistBonus = resistBonus;
        effectType = Type.positive;
    }

    public override void OnApply()  => target.physicalResistanceAdder += resistBonus;
    public override void OnExpire() => target.physicalResistanceAdder -= resistBonus;

    public override string GetName()        => "<color=#9B30D0>Hellebore's Protection</color>";
    public override string GetDescription() => $"Physical Resistance increased by <color=green><b>{resistBonus * 100f:F0}%</b></color>.";
}
