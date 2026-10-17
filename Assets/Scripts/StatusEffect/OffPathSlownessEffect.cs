public class OffPathSlownessEffect : StatusEffect
{
    private const float slowAmount = 0.25f;
    private readonly Insect insect;
    private readonly float appliedAmount;

    public OffPathSlownessEffect(Entity target)
        : base(target, float.MaxValue, 1, null)
    {
        effectType = Type.negative;
        visible = false;
        insect = target as Insect;
        appliedAmount = slowAmount + MulchManager.GlobalSlowBonus;
    }

    public override void OnApply()  { if (insect is not null) insect.movementSpeedMultiplier -= appliedAmount; }
    public override void OnExpire() { if (insect is not null) insect.movementSpeedMultiplier += appliedAmount; }
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "Off-Path Slowness";
    public override string GetDescription() => $"Moving off the path reduces Movement Speed by {slowAmount*100:F0}%.";
}
