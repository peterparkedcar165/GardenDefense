public class OffPathSlownessEffect : StatusEffect
{
    private const float slowAmount = 0.25f;
    private readonly Insect insect;

    public OffPathSlownessEffect(Entity target)
        : base(target, float.MaxValue, 1, null)
    {
        effectType = Type.negative;
        insect = target as Insect;
    }

    public override void OnApply()  { if (insect is not null) insect.movementSpeedMultiplier -= slowAmount; }
    public override void OnExpire() { if (insect is not null) insect.movementSpeedMultiplier += slowAmount; }
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "Off-Path Slowness";
    public override string GetDescription() => "Moving off the path reduces movement speed by 25%.";
}
