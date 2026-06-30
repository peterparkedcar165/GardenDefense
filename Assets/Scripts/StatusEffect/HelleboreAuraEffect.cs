public class HelleboreAuraEffect : StatusEffect
{
    private readonly float resistBonus;
    private readonly float magicResistBonus;

    public HelleboreAuraEffect(Entity target, float duration, int level, Entity source, float resistBonus, float magicResistBonus = 0f)
        : base(target, duration, level, source)
    {
        this.resistBonus      = resistBonus;
        this.magicResistBonus = magicResistBonus;
        effectType      = Type.positive;
        elementalType   = ElementalType.Poison;
        sourceStackable = true;
    }

    public override void OnApply()  { target.armorAdder += resistBonus; target.magicArmorAdder += magicResistBonus; }
    public override void OnExpire() { target.armorAdder -= resistBonus; target.magicArmorAdder -= magicResistBonus; }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=purple>Hellebore's Protection</color>";
    public override string GetDescription()
    {
        string s = $"Armor increased by <color=#00CED1><b>{(int)resistBonus}</b></color>.";
        if (magicResistBonus > 0f)
            s += $"\n<color=#FFB6C1><b>Magic Armor</b></color> increased by <color=#FFB6C1><b>{(int)magicResistBonus}</b></color>.";
        return s;
    }
}
