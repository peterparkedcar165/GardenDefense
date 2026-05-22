public class CactusTauntingEffect : StatusEffect
{
    private readonly float healingBonus;

    public CactusTauntingEffect(Entity target, float duration, int level, Entity source, float healingBonus)
        : base(target, duration, level, source)
    {
        this.healingBonus = healingBonus;
        effectType = Type.positive;
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
    public override string GetDescription() => $"Taunting nearby insects. Healing received increased by {healingBonus * 100f:F0}%.";
}
