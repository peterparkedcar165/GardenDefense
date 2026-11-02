// granted by IronbarkCounterMulchEffect whenever an Ironbark plant lands a Counter Damage hit -
// a plain finite-duration shield, same minimal shape as StartingShieldEffect but not infinite
public class IronbarkCounterShieldEffect : ShieldEffect
{
    public IronbarkCounterShieldEffect(Entity target, Entity source, float amount, float duration)
        : base(target, duration, 1, source, amount) { }

    public override string GetName() => "<color=#A9A9A9>Ironbark Ward</color>";
}
