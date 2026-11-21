// granted by IronbarkCounterMulchEffect (Ironclad Riposte) whenever its plant lands a Counter Damage
// hit. it stacks (Tommy, 2026-11-21): every further counter hit while it's up adds its share to the
// shield and raises the cap by the same amount, with no limit, and starts its timer over. it only
// goes away when it runs out of time or is broken
public class IronbarkCounterShieldEffect : ShieldEffect
{
    private float cap;

    public IronbarkCounterShieldEffect(Entity target, Entity source, float amount, float duration)
        : base(target, duration, 1, source, amount)
    {
        cap = this.amount;
    }

    protected override float ShieldCap => cap;

    // the same scaling a new shield gets (the source's Heals & Shield Bonus, the target's Heals &
    // Shield Received). returns what was added
    public float Stack(float rawAmount, float fullDuration)
    {
        float bonus = source != null ? source.healingBonus : 0f;
        float scaled = rawAmount * (1f + target.healingReceived) * (1f + bonus);
        amount += scaled;
        cap += scaled;
        duration = fullDuration;
        return scaled;
    }

    public override string GetName() => "<color=#A9A9A9>Ironbark Ward</color>";
}
