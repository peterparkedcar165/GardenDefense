using UnityEngine;

[CreateAssetMenu(fileName = "BogIrisData", menuName = "Scriptable Objects/PlantData/BogIris")]
public class BogIrisData : PlantData
{
    public float baseSunInterval = 4f;
    public int baseSunGenerated = 2;
    public float baseKnockUpHeight;
    public float baseGeyserDamage;
    public float waterweightDuration = 8f;
    public float waterweightArmorShred = 20f;
    public float waterweightFallDamageResistanceShred = 0.15f;

    [Header("Path 1 Scaling")]
    public float baseHealthDamageScaling = 0.12f;
    public float path1HealthScalingPerLevel = 0.02f;
    public float path1AttackSpeedPerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    public float baseRegenPercent = 0.02f;
    public float path2RegenPercentPerLevel = 0.01f;
    public int   baseOpenBonusSun = 2;
    public int   path2OpenBonusSunPerLevel = 1;
    public float baseReduceChance = 0.35f;
    public float path2ReduceChancePerLevel = 0.05f;
    public float path2MaxHealthPerLevel = 15f;

    [Header("Path 3 Scaling")]
    public float path3GeyserDamagePerLevel = 15f;
    public float path3KnockUpPerLevel = 1f;
    public float path3GeyserRadiusPerLevel = 0.15f;
}
