public class elementalAffinityBoostEffect : StatusEffect
{
    public readonly float bonus;

    public elementalAffinityBoostEffect(Entity target, float duration, int level, Entity source, float bonus)
        : base(target, duration, level, source)
    {
        this.bonus = bonus;
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        target.elementalAffinityAdder += bonus;
    }

    public override void OnExpire()
    {
        target.elementalAffinityAdder -= bonus;
    }

    public override string GetName() => "<color=green><b>Begonia's Blessing</b></color>";
    public override string GetDescription() => $"Increase Elemental Affinity by <color=green><b>{bonus * 100f:F0}%</b></color>.";
}
