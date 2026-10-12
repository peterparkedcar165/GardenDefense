// applied to every plant while the level's weather includes Sandstorm. reduces Accuracy by
// 10% per intensity level - Aloe Vera and Cactus are desert-adapted and immune to the penalty
public class SandstormExposedEffect : StatusEffect
{
    private const float PenaltyPerLevel = 0.1f;

    private readonly float penalty;

    public SandstormExposedEffect(Entity target, Entity source, int intensity)
        : base(target, float.MaxValue, intensity, source)
    {
        effectType = Type.negative;
        bool immune = target is AloeVera || target is Cactus;
        penalty = immune ? 0f : PenaltyPerLevel * level;
    }

    public override void OnApply()  => target.accuracyAdder -= penalty;
    public override void OnExpire() => target.accuracyAdder += penalty;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#E8D9A8>[Weather]: Sandstorm</color>";
    public override string GetDescription() =>
        penalty > 0f
            ? $"Reduce <color=red><b>Accuracy</b></color> by <color=red><b>{penalty * 100f:F0}%</b></color>."
            : "Desert-adapted: immune to the Sandstorm's Accuracy penalty.";
}
