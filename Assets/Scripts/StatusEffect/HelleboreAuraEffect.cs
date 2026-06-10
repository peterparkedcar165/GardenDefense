public class HelleboreAuraEffect : StatusEffect
{
    private readonly float resistBonus;

    public HelleboreAuraEffect(Entity target, float duration, int level, Entity source, float resistBonus)
        : base(target, duration, level, source)
    {
        this.resistBonus = resistBonus;
        effectType = Type.positive;
    }

    public override void OnApply()  => target.armorAdder += resistBonus;
    public override void OnExpire() => target.armorAdder -= resistBonus;

    public override string GetName()        => "<color=#9B30D0>Hellebore's Protection</color>";
    public override string GetDescription() => $"Armor increased by <color=#00CED1><b>{(int)resistBonus}</b></color>.";
}
