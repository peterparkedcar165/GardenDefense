using UnityEngine;

// exp-to-level curve for a plant's persistent, per-species meta level (SaveData.PlantExpRecord).
// exponential by design: leveling from 1 is quick, but each subsequent level costs meaningfully
// more, so maxing a plant out is a long-term "the more you use it, the more it grows" grind.
// tune BaseExp/GrowthRate here if the pacing needs adjusting - nothing else needs to change
public static class PlantLevelCurve
{
    // level 1 starts at 0 skill points (no level-up has happened yet), and each level-up grants
    // exactly 1 point - so reaching 25 total points means leveling up 25 times, i.e. level 26
    public const int MaxLevel = 26;
    private const int BaseExp = 3500;
    private const float GrowthRate = 1.5f;

    // exp needed to go from `level` to `level + 1` - this is also the amount a plant's totalExp
    // (progress within the current level, reset to 0 on every level-up) must reach, since exp
    // is never cumulative across levels
    public static int ExpForNextLevel(int level) => Mathf.RoundToInt(BaseExp * Mathf.Pow(GrowthRate, level - 1));
}
