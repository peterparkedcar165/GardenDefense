public class ScorcherEffect : StatusEffect
{
    private const float BonusPerStack = 0.04f;
    public const int MaxStacks = 10;

    public ScorcherEffect(Entity target, float duration, int stacks, Entity source)
        : base(target, duration, stacks, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Fire;
    }

    public override void OnApply()
    {
        target.fireDamageAdder += BonusPerStack * level;
    }

    public override void OnExpire()
    {
        target.fireDamageAdder -= BonusPerStack * level;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=orange><b>Scorcher</b></color>";
    public override string GetDescription() =>
        $"Increase <color=orange><b>Fire Damage</b></color> by <color=green><b>{level * BonusPerStack * 100f:F0}%</b></color>.";
}
