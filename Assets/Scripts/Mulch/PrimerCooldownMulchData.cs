using UnityEngine;

// Reaction Adept (class and asset names kept from when it was Primer Cooldown Reduction): primers
// this plant lands start the insect's element cooldown some seconds shorter (Tommy, 2026-11-21: a
// flat cut in seconds, it was a percentage)
[CreateAssetMenu(fileName = "PrimerCooldownMulch", menuName = "Scriptable Objects/Mulch/Reaction Adept")]
public class PrimerCooldownMulchData : MulchData
{
    [Tooltip("seconds taken off the element cooldown an insect gets when this plant puts a primer on it. never below zero")]
    public TierValue cooldownReduction = new TierValue(2f, 4f, 8f);

    public override MulchEffect CreateEffect(MulchItem item) => new PrimerCooldownMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Reduce the cooldown of your elemental primer on insects by {Green(cooldownReduction.Get(r).ToString("0.#"))} seconds.";
    }
}
