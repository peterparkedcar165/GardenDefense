public abstract class TauntingEffect : StatusEffect
{
    protected TauntingEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
    }

    public override string GetDescription() => "Forcing nearby insects to target this plant.";

    public override void OnApply() { }
    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
