public class BlindEffect : StatusEffect
{
    private readonly float accuracyPenalty;

    public BlindEffect(Entity target, float duration, int level, Entity source, float accuracyPenalty)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        this.accuracyPenalty = accuracyPenalty;
    }

    public void RefreshDuration(float newDuration) => duration = newDuration;

    public override void OnApply()
    {
        target.accuracyAdder -= accuracyPenalty;
        StatusIndicator.Spawn(target.transform.position + new UnityEngine.Vector3(0.4f, 0f, 0f), "Blinded", new UnityEngine.Color(0f, 0.8f, 0.9f));
    }
    public override void OnExpire() => target.accuracyAdder += accuracyPenalty;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#DDDDDD>Blinded</color>";
    public override string GetDescription() =>
        $"Reduces Accuracy by <color=green><b>{accuracyPenalty * 100f:F0}%</b></color>, causing attacks to frequently miss.";
}
