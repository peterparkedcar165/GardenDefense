public class VerdantFervorEffect : StatusEffect
{
    private int _stacks;
    private const int maxStacks = 10;
    private const float speedPerStack = 0.04f;
    private const float stackDuration = 8f;

    public VerdantFervorEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Grass;
        _stacks = 1;
    }

    public override void OnApply()
    {
        target.attackSpeedTotalMultiplier += speedPerStack;
    }

    public void AddStack()
    {
        duration = stackDuration;
        if (_stacks >= maxStacks) return;
        _stacks++;
        target.attackSpeedTotalMultiplier += speedPerStack;
    }

    public override void OnExpire()
    {
        target.attackSpeedTotalMultiplier -= speedPerStack * _stacks;
    }

    public override string GetName() => "<color=green><b>Verdant Fervor</b></color>";
    public override string GetDescription() => $"Total Attack Speed increased by <color=green><b>{_stacks * speedPerStack * 100f:F0}%</b></color> ({_stacks} stack{(_stacks > 1 ? "s" : "")}).";
}
