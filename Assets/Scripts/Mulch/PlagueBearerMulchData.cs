using UnityEngine;

// Plague Bearer (Tommy, 2026-11-23): an insect that dies while it has Poisoned from this plant passes
// part of the time it had left onto the nearest insects around it (PlagueBearerMulchEffect.Spread)
[CreateAssetMenu(fileName = "PlagueBearerMulch", menuName = "Scriptable Objects/Mulch/Plague Bearer")]
public class PlagueBearerMulchData : MulchData
{
    [Tooltip("share of the dying insect's remaining Poisoned duration each insect gets, 1 = all of it")]
    public TierValue durationShare = new TierValue(0.25f, 0.5f, 1f);
    [Tooltip("how many of the nearest living insects get it")]
    public TierValue targets = new TierValue(1f, 2f, 3f);
    [Tooltip("how far from the dying insect they can be, in the same units as Germinate's 2.5")]
    public TierValue radius = new TierValue(2f, 3f, 4f);

    public override MulchEffect CreateEffect(MulchItem item) => new PlagueBearerMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        int count = targets.GetInt(r);
        string poisoned = Kw("Poisoned", PoisonColor);
        return $"When an insect dies while it has {poisoned} from the plant, {Green(Pct(durationShare.Get(r)))} of its remaining " +
            $"{poisoned} duration passes to the nearest {Green(count.ToString())} {(count == 1 ? "insect" : "insects")} within a " +
            $"{Green(radius.Get(r).ToString("0.#"))} radius.\n\n" +
            $"An insect that is already {poisoned} keeps the stronger one, and the durations add up.";
    }
}
