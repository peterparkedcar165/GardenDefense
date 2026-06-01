// applied by the Stargazer's flamethrower. each stack increases Burn DoT damage the target
// takes. read by BurnEffect when it ticks. level == current stack count
public class FlammableEffect : StatusEffect
{
    private readonly float bonusPerStack;

    public FlammableEffect(Entity target, float duration, int stacks, Entity source, float bonusPerStack)
        : base(target, duration, stacks, source)
    {
        this.bonusPerStack = bonusPerStack;
        effectType = Type.negative;
    }

    // multiplier applied to Burn DoT ticks
    public float BurnMultiplier => 1f + level * bonusPerStack;

    public override void OnApply() { }
    public override void OnExpire() { }
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#FF6B1A>Flammable</color>";
    public override string GetDescription() =>
        $"Increases <color=orange>Burn</color> damage taken by <color=green><b>{(BurnMultiplier - 1f) * 100f:F0}%</b></color>.";
}
