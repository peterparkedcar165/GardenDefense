public class FrozenRageEffect : TauntEffect
{
    private readonly float resistanceReduction;

    public FrozenRageEffect(Entity target, float duration, int level, Entity source, IAttackable taunter, float resistanceReduction)
        : base(target, duration, level, source, taunter)
    {
        this.resistanceReduction = resistanceReduction;
    }

    public override void OnApply()
    {
        target.physicalResistanceAdder -= resistanceReduction;
    }

    public override void OnExpire()
    {
        target.physicalResistanceAdder += resistanceReduction;
    }

    public override string GetName() => "<color=#00FFFF>Frozen Rage</color>";
    public override string GetDescription() =>
        $"Forced to attack Holly. Physical Resistance reduced by <color=#FF6666><b>{resistanceReduction * 100f:F0}%</b></color>.";
}
