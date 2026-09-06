public class BlindingPollenEffect : StatusEffect
{
    // flat point reduction on the 0-1 accuracy scale, not a % of the target's own accuracy - most
    // insects sit at 0 base accuracy, so a relative reduction would be a no-op; this guarantees a
    // real chance-to-miss swing (against a Plant with 0 evasion, -0.75 accuracy alone gives a 75%
    // miss chance) regardless of the target's own accuracy stat
    public const float DefaultReduction = 0.75f;

    private readonly float reduction;

    public BlindingPollenEffect(Entity target, float duration, int level, Entity source, float reduction = DefaultReduction)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Wind;
        this.reduction = reduction;
    }

    public override void OnApply()  => target.accuracyAdder -= reduction;
    public override void OnExpire() => target.accuracyAdder += reduction;

    public override string GetName() => "<color=#B2EBF2>Blinding Pollen</color>";
    public override string GetDescription() =>
        $"<color=#E0E0E0><b>Accuracy</b></color> reduced by <color=red><b>{reduction * 100f:F0}%</b></color>.";
}
