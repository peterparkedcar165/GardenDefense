using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DroughtMulch", menuName = "Scriptable Objects/Mulch/Drought")]
public class DroughtMulchData : MulchData
{
    [Tooltip("seconds the drought lasts: one timer for the whole level, from the first drought equipped, in game time while " +
        "any plant carrying a drought is alive. droughts equipped after it ran out start at their after multiplier")]
    public float haltDuration = 180f;
    [Tooltip("this plant's Sun production during the drought, as a multiplier. 0 = none")]
    public float droughtSunMultiplier = 0f;
    [Tooltip("this plant's Sun production after the drought, for the rest of the level, as a multiplier. 2 = double")]
    public TierValue afterSunMultiplier = new TierValue(2f, 3f, 4f);

    // only drops when something makes sun: a sun producer in the loadout, or sun sap
    public override bool IsUseful(List<PlantData> loadout) => HasSunMaker(loadout);

    public override MulchEffect CreateEffect(MulchItem item) => new DroughtMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        string seconds = Green(haltDuration.ToString("0"));
        string during = droughtSunMultiplier <= 0f
            ? $"Halts {SunLabel} production for the next {seconds} seconds"
            : $"Reduces {SunLabel} production to {Red(Pct(droughtSunMultiplier))} for the next {seconds} seconds";
        return $"{during}, then produces {SunLabel} at {Green(Pct(afterSunMultiplier.Get(r)))} effectiveness. " +
            "Every Drought shares one timer: once it has run out, another Drought starts at full effectiveness.";
    }
}
