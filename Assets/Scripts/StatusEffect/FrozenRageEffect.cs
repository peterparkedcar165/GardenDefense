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
        target.armorAdder -= resistanceReduction;
    }

    public override void OnExpire()
    {
        target.armorAdder += resistanceReduction;
    }

    public override string GetName() => "<color=#00FFFF>Frozen Rage</color>";
    public override string GetDescription() =>
        $"Forced to attack Holly. Armor reduced by <color=#FF6666><b>{resistanceReduction:F1}</b></color>.";
}
