using UnityEngine;

// Inflammable (Tommy, 2026-11-23): the Burns this plant is the source of tick faster while their
// duration stays the same, so they deal more in all (the shared over time rule, OverTimeTicker)
[CreateAssetMenu(fileName = "InflammableMulch", menuName = "Scriptable Objects/Mulch/Inflammable")]
public class InflammableMulchData : MulchData
{
    [Tooltip("how much faster its Burns tick, e.g. 0.5 = 50% faster: a 6 s Burn deals 9 ticks' worth instead of 6")]
    public TierValue burnTickSpeed = new TierValue(0.125f, 0.25f, 0.5f);

    public override MulchEffect CreateEffect(MulchItem item) => new InflammableMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"{Kw("Burn", FireColor)} applied by the plant ticks {Green(Pct(burnTickSpeed.Get(r)))} faster. " +
            $"Its duration stays the same, so it deals more damage in total.";
    }
}
