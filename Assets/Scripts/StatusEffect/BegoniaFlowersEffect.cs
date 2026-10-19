public class BegoniaFlowersEffect : StatusEffect
{
    private readonly float waterCooldownReduction;
    // 0 unless Begonia's Path1 is maxed when this tick applied/refreshed it
    private readonly float grassResistanceShred;

    public BegoniaFlowersEffect(Entity target, float duration, Entity source, float waterCooldownReduction, float grassResistanceShred)
        : base(target, duration, 1, source)
    {
        this.waterCooldownReduction = waterCooldownReduction;
        this.grassResistanceShred = grassResistanceShred;
        effectType = Type.negative;
        elementalType = ElementalType.Water;
    }

    public override void OnApply()
    {
        target.waterInternalCooldownAdder -= waterCooldownReduction;
        target.grassResistanceAdder -= grassResistanceShred;
    }

    public override void OnExpire()
    {
        target.waterInternalCooldownAdder += waterCooldownReduction;
        target.grassResistanceAdder += grassResistanceShred;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#4FC3F7>Begonia's Flowers</color>";
    public override string GetDescription()
    {
        string desc = $"Reduces <color=#4FC3F7><b>Water</b></color> primer internal cooldown by <color=green><b>{waterCooldownReduction:F1}s</b></color>.";
        if (grassResistanceShred > 0f)
            desc += $" Also reduces <color=green><b>Grass Resistance</b></color> by <color=green><b>{grassResistanceShred * 100f:F0}%</b></color>.";
        return desc;
    }
}
