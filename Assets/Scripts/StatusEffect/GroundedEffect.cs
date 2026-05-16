public class GroundedEffect : StatusEffect
{
    private FlyingInsect flyingInsect;

    public GroundedEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#8B4513>Grounded</color>";
    public override string GetDescription() => "Prevents flight.";

    public override void OnApply()
    {
        if (target is Insect i) i.verticalVelocity = 0f;
        flyingInsect = target as FlyingInsect;
        flyingInsect?.SetFlight(false);
    }

    public override void OnExpire()
    {
        if (flyingInsect == null) return;
        flyingInsect.verticalVelocity = 0f;
        flyingInsect.SetFlight(true);
    }
}
