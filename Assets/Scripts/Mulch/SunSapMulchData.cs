using UnityEngine;

[CreateAssetMenu(fileName = "SunSapMulch", menuName = "Scriptable Objects/Mulch/Sun Sap")]
public class SunSapMulchData : MulchData
{
    [Tooltip("Sun made each time")]
    public TierValue sunAmount = new TierValue(5f, 10f, 15f);
    [Tooltip("seconds between each time (game time)")]
    public float interval = 30f;

    public override MulchEffect CreateEffect(MulchItem item) => new SunSapMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"This plant makes {Green(sunAmount.GetInt(r).ToString())} {SunLabel} every {Green(interval.ToString("0.#"))} seconds. " +
            $"Works with Sunset, Solar Flare and Drought like any {SunLabel} plant.";
    }
}
