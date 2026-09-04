// Bird of Paradise's Path2 max: tracks how many attack hits have landed on THIS specific target,
// so every 3rd hit can trigger an extra delayed on-hit proc (see BirdOfParadise.OnAttackHit).
// hidden, purely a counter - resets if this Bird stops hitting the target for a few seconds,
// same shape as PsionicMarkEffect's stacking-mark pattern
public class TalonHitCounterEffect : StatusEffect
{
    public int hitCount;
    private const float ResetDuration = 5f;

    public TalonHitCounterEffect(Entity target, Entity source)
        : base(target, ResetDuration, 1, source)
    {
        effectType      = Type.neutral;
        sourceStackable = true;
        visible         = false;
    }

    public void RegisterHit()
    {
        hitCount++;
        duration = ResetDuration;
    }

    public override void OnApply() => hitCount = 0;
    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }

    public override string GetName() => "Talon Mark";
    public override string GetDescription() => "";
}
