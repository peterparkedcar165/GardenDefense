public class TauntEffect : StatusEffect
{
    public IAttackable taunter;

    public TauntEffect(Entity target, float duration, int level, Entity source, IAttackable taunter)
        : base(target, duration, level, source)
    {
        this.taunter = taunter;
        effectType = Type.negative;
    }

    public override string GetName() => "Taunted";
    public override string GetDescription() => "Forced to attack a target.";
}
