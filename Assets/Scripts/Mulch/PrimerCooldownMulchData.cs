using UnityEngine;

[CreateAssetMenu(fileName = "PrimerCooldownMulch", menuName = "Scriptable Objects/Mulch/Primer Cooldown Reduction")]
public class PrimerCooldownMulchData : MulchData
{
    [Tooltip("how much shorter the cooldown is after this plant puts a primer on an insect, e.g. 0.3 = 30%. capped at 90%")]
    public TierValue cooldownReductionPercent = TierValue.Scaled(0.3f);

    public override MulchEffect CreateEffect(MulchItem item) => new PrimerCooldownMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"After this plant puts an <b><color=green>Elemental Primer</color></b> on an insect, that insect's Primer cooldown is " +
            $"{Green(Pct(Mathf.Min(0.9f, cooldownReductionPercent.Get(r))))} shorter.";
    }
}
