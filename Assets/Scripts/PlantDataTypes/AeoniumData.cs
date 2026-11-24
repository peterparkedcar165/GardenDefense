using UnityEngine;

[CreateAssetMenu(fileName = "AeoniumData", menuName = "Scriptable Objects/PlantData/Aeonium")]
public class AeoniumData : PlantData
{
    [Header("Passive Sun Generation")]
    public int   baseSunGenerated      = 6;
    public float baseSunTimerReduction = 0.3f;
    public float sunTimerMPMultiplier  = 0.003f;

    [Header("Skill Blessing of the Sun")]
    public float baseSunYieldBonus     = 0.5f;   // +50% sun yield
    public float path3SunYieldPerLevel = 0.05f;  // extra sun yield per skill level

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 4f;
    public float path1AttackSpeedPerLevel  = 0.04f;
    public float path1AttackRangePerLevel  = 0.2f;

    [Header("Path 2 Scaling")]
    public int   path2SunPerLevel               = 1;
    public float path2SunTimerReductionPerLevel = 0.1f;
}
