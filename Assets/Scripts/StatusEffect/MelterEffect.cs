public class MelterEffect : StatusEffect
{
    private const float BonusPerStack = 0.06f;
    public const int MaxStacks = 10;

    public MelterEffect(Entity target, float duration, int stacks, Entity source)
        : base(target, duration, stacks, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Fire;
    }

    public override void OnApply()
    {
        target.elementalAffinityAdder += BonusPerStack * level;
    }

    public override void OnExpire()
    {
        target.elementalAffinityAdder -= BonusPerStack * level;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=orange><b>Melter</b></color>";
    public override string GetDescription() =>
        $"Increase <color=#FFD700><b>Elemental Affinity</b></color> by <color=green><b>{level * BonusPerStack * 100f:F0}%</b></color>.";
}
