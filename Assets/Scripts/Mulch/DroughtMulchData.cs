using UnityEngine;

[CreateAssetMenu(fileName = "DroughtMulch", menuName = "Scriptable Objects/Mulch/Drought")]
public class DroughtMulchData : MulchData
{
    [Tooltip("whole waves the drought lasts, counted from the first wave that starts after it's equipped")]
    public int droughtWaves = 3;
    [Tooltip("this plant's Sun production during the drought, as a multiplier. 0 = none")]
    public float droughtSunMultiplier = 0f;
    [Tooltip("this plant's Sun production after the drought, for the rest of the level, as a multiplier. 2 = double")]
    public TierValue afterSunMultiplier = new TierValue(2f, 3f, 4f);

    public override MulchEffect CreateEffect(MulchItem item) => new DroughtMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        string during = droughtSunMultiplier <= 0f ? $"makes {Red("no")} {SunLabel}" : $"makes only {Red(Pct(droughtSunMultiplier))} of its {SunLabel}";
        return $"This plant {during} for the next {Green(droughtWaves.ToString())} waves. " +
            $"After that, it makes {Green(afterSunMultiplier.Get(r).ToString("0.##") + "x")} {SunLabel} for the rest of the level.";
    }
}
