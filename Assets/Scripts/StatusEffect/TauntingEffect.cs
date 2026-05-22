public class TauntingEffect : StatusEffect
{
    public TauntingEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
    }

    public override string GetName() => "<color=#00FFFF>Holly Taunt</color>";
    public override string GetDescription() => "Forcing nearby insects to target this plant. Emitting a frozen aura.";

    public override void OnApply() { }
    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
