public class EatingAcornEffect : StatusEffect
{
    public EatingAcornEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "Eating Acorn";
    public override string GetDescription() => "Stopped to eat an acorn.";
}
