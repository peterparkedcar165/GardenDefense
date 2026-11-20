using UnityEngine;

[CreateAssetMenu(fileName = "DormancyMulch", menuName = "Scriptable Objects/Mulch/Dormancy")]
public class DormancyMulchData : MulchData
{
    [Tooltip("whole waves the penalty lasts, counted from the first wave that starts after it's equipped")]
    public int dormantWaves = 3;
    [Tooltip("damage this plant loses during those waves, e.g. 0.15 = 15% less")]
    public TierValue damagePenalty = TierValue.Scaled(0.15f);
    [Tooltip("damage this plant gains after that, for the rest of the level, e.g. 0.25 = 25% more")]
    public TierValue damageBonus = TierValue.Scaled(0.25f);

    public override MulchEffect CreateEffect(MulchItem item) => new DormancyMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"For the next {Green(dormantWaves.ToString())} waves this plant deals {Red(Pct(damagePenalty.Get(r)))} less damage. " +
            $"After that, it deals {Green(Pct(damageBonus.Get(r)))} more damage for the rest of the level.";
    }
}
