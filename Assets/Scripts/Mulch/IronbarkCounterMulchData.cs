using UnityEngine;

[CreateAssetMenu(fileName = "IronbarkCounterMulch", menuName = "Scriptable Objects/Mulch/Ironclad Riposte")]
public class IronbarkCounterMulchData : MulchData
{
    [Tooltip("Counter Damage added to this plant, e.g. 0.25 = +25%")]
    public TierValue counterDamageBonus = TierValue.Scaled(0.25f);
    [Tooltip("shield when its Counter Damage lands, as a share of that hit's damage. it stacks with no limit while counter hits keep landing")]
    public TierValue shieldPercentOfDamage = TierValue.Scaled(0.05f);
    [Tooltip("how long that shield lasts; every counter hit starts it over")]
    public float shieldDuration = 4f;

    public override MulchEffect CreateEffect(MulchItem item) => new IronbarkCounterMulchEffect(this, item);

    public override string GetDescription(MulchItem item) => Describe(item, null);

    public override string GetDescription(MulchItem item, MulchEffect effect) => Describe(item, effect as IronbarkCounterMulchEffect);

    // the shield's number (X) is the real one once the plant has landed a counter hit: what that hit
    // gave. before that, and in the bag, it reads as the share of the hit
    private string Describe(MulchItem item, IronbarkCounterMulchEffect effect)
    {
        MulchTier r = item.rarity;
        string counter = Kw("Counter Damage", CounterColor);
        string amount = effect != null && effect.LastShieldGain > 0f
            ? $"{Kw(effect.LastShieldGain.ToString("0"), ShieldColor)} damage"
            : $"damage equal to {Kw(Pct(shieldPercentOfDamage.Get(r)), ShieldColor)} of the {counter} dealt";
        return $"Increase {counter} by {Green(Pct(counterDamageBonus.Get(r)))}.\n\n" +
            $"Dealing {counter} generates a stackable {Kw("shield", ShieldColor)} that protects the plant from {amount}, " +
            $"for {Green(shieldDuration.ToString("0.#"))} seconds.";
    }
}
