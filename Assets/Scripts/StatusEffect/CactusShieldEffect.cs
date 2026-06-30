public class CactusShieldEffect : ShieldEffect
{
    public CactusShieldEffect(Entity target, float duration, int level, Entity source, float amount)
        : base(target, duration, level, source, amount)
    {
        elementalType = ElementalType.Nature;
    }

    public override string GetName() => "<color=green>Cactus Armor</color>";
}
