public class ZinniaAuraEffect : StatusEffect
{
    private readonly float fireDamageBonus;
    private readonly float magicPowerBonus;
    private readonly float elementalAffinityBonus;

    public ZinniaAuraEffect(Entity target, float duration, int level, Entity source, float fireDamageBonus, float magicPowerBonus, float elementalAffinityBonus = 0f)
        : base(target, duration, level, source)
    {
        this.fireDamageBonus       = fireDamageBonus;
        this.magicPowerBonus       = magicPowerBonus;
        this.elementalAffinityBonus = elementalAffinityBonus;
        effectType      = Type.positive;
        elementalType   = ElementalType.Fire;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        target.fireDamageAdder       += fireDamageBonus;
        target.magicPowerAdder       += magicPowerBonus;
        if (elementalAffinityBonus > 0f) target.elementalAffinityAdder += elementalAffinityBonus;
    }

    public override void OnExpire()
    {
        target.fireDamageAdder       -= fireDamageBonus;
        target.magicPowerAdder       -= magicPowerBonus;
        if (elementalAffinityBonus > 0f) target.elementalAffinityAdder -= elementalAffinityBonus;
    }

    public override string GetName() => "<color=orange><b>Zinnia's Warmth</b></color>";
    public override string GetDescription()
    {
        string desc = $"<color=orange><b>Fire Damage</b></color> increased by <color=green><b>{fireDamageBonus * 100f:F0}%</b></color> and <color=#FFB6C1><b>Magic Power</b></color> by <color=green><b>{magicPowerBonus:F0}</b></color>.";
        if (elementalAffinityBonus > 0f)
            desc += $" <color=green><b>Elemental Affinity</b></color> increased by <color=green><b>{elementalAffinityBonus * 100f:F0}%</b></color>.";
        return desc;
    }
}
