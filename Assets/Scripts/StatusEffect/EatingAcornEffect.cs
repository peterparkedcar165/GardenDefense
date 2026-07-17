public class EatingAcornEffect : StatusEffect
{
    public EatingAcornEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Grass;
    }

    public override string GetName() => "<color=green>Eating Acorn</color>";
    public override string GetDescription() => "Stopped to eat an acorn.";
}
