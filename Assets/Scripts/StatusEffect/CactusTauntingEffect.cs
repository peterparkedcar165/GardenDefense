public class CactusTauntingEffect : StatusEffect
{
    private readonly float healingBonus;

    public CactusTauntingEffect(Entity target, float duration, int level, Entity source, float healingBonus)
        : base(target, duration, level, source)
    {
        this.healingBonus = healingBonus;
        effectType = Type.positive;
        elementalType = ElementalType.Nature;
    }

    public override void OnApply()
    {
        target.healingReceivedAdder += healingBonus;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.healingReceivedAdder -= healingBonus;
    }

    public override string GetName() => "<color=green>Cactus Taunt</color>";
    public override string GetDescription() => $"Forces nearby insects to target it. Returns <color=green><b>150%</b></color> of the attacker's ATK as <color=green>Nature</color> Physical damage.";
}
