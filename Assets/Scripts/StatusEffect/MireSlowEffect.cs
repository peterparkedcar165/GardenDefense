public class MireSlowEffect : StatusEffect
{
    private const float SlowAmount = 0.5f;

    public MireSlowEffect(Entity target, float duration, Entity source) : base(target, duration, 1, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#6B8E23>Mire</color>";
    public override string GetDescription() => $"Reduce <color=green><b>Movement Speed</b></color> by <color=green><b>{SlowAmount * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier -= SlowAmount;
    }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier += SlowAmount;
    }
}
