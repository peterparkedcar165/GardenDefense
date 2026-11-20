using UnityEngine;

[CreateAssetMenu(fileName = "CriticalRejuvenationMulch", menuName = "Scriptable Objects/Mulch/Critical Rejuvenation")]
public class CriticalRejuvenationMulchData : MulchData
{
    [Tooltip("heal crit chance added to this plant, e.g. 0.25 = +25%")]
    public TierValue healCritChance = TierValue.Scaled(0.25f);

    public override MulchEffect CreateEffect(MulchItem item) => new CriticalRejuvenationMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Healing this plant gives can critically strike. {Green("+" + Pct(healCritChance.Get(r)))} Heal Crit Chance.";
    }
}
