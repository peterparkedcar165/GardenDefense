using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DroughtMulch", menuName = "Scriptable Objects/Mulch/Drought")]
public class DroughtMulchData : MulchData
{
    [Tooltip("seconds the drought lasts: each plant keeps its own timer, from its first drought equipped, in game time while " +
        "that plant is alive. a drought equipped on that plant after it ran out starts at its after multiplier")]
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
            "Each plant keeps its own timer: once it has run out, another Drought on that plant starts at full effectiveness.";
    }
}
