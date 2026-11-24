using UnityEngine;

[CreateAssetMenu(fileName = "PoisonShroomData", menuName = "Scriptable Objects/PlantData/PoisonShroom")]
public class PoisonShroomData : PlantData
{
    public float basePoisonDPS;
    public float baseToxicSporeDuration = 3f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;
    public float path1AttackSpeedPerLevel = 0.08f;
    public float path1AttackRangePerLevel = 0.1f;
    [Tooltip("Path1 max: Toxic Spore splashes onto at most this many of the nearest OTHER insects (the main target is unaffected by this cap)")]
    public int path1MaxSplashTargets = 2;

    [Header("Path 2 Scaling")]
    public float path2ToxicSporeDurationPerLevel = 0.4f;
    // Toxic Spore also deals bonus damage equal to a fraction of the target's CURRENT health
    // per second while active
    public float basePercentHealthDPS = 0.012f;
    public float path2PercentHealthDPSPerLevel = 0.004f;

    [Header("Path 3 Scaling")]
    public float path3SkillDurationPerLevel = 1f;
    public float path3RadiusPerLevel = 0.2f;
}
