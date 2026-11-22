using UnityEngine;

[CreateAssetMenu(fileName = "SunSapMulch", menuName = "Scriptable Objects/Mulch/Sun Sap")]
public class SunSapMulchData : MulchData
{
    [Tooltip("Sun made each time")]
    public TierValue sunAmount = new TierValue(5f, 10f, 20f);
    [Tooltip("seconds between each time (game time)")]
    public TierValue interval = new TierValue(30f, 25f, 20f);

    public override MulchEffect CreateEffect(MulchItem item) => new SunSapMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Produces {Green(sunAmount.GetInt(r).ToString())} {SunLabel} every {Green(interval.Get(r).ToString("0.#"))} seconds.";
    }
}
