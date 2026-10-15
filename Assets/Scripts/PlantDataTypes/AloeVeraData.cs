using UnityEngine;

[CreateAssetMenu(fileName = "AloeVeraData", menuName = "Scriptable Objects/PlantData/AloeVera")]
public class AloeVeraData : PlantData
{
    [Header("Aloe Vera")]
    public float baseHealAmount = 24f;
    public float baseTempReduction = 4.5f;

    [Header("Soothing Rain")]
    public float baseSkillHealPerTick = 10f;
    public float baseSkillHealInterval = 1f;
    public float baseSkillTempReduction = 2f;

    // how long the water droplet hovers/bobs at the peak of its arc above the target before
    // falling - scaled down by Attack Speed at runtime (see AloeVera.Fire), and reduced to 0
    // outright by the Flash Drop skill tree node
    [Header("Attack Arc")]
    public float baseLobTime = 0.4f;

    // Max Health scaling: flat % of her own current Max Health added on top of each output's
    // other sources (Magic Power included) - same direct-coefficient convention as her existing
    // Magic Power bonuses, just independently tunable per output here
    [Header("Max Health Scaling")]
    public float healMaxHealthScaling      = 0.05f;
    public float attackMaxHealthScaling    = 0.05f;
    public float skillHealMaxHealthScaling = 0.05f;

    [Header("Path 1 Scaling")]
    public float path1AttackSpeedPerLevel = 0.02f;
    public float path1AttackRangePerLevel = 0.2f;
    // Attack path also grants flat Max Health per level
    public float path1MaxHealthPerLevel = 20f;

    [Header("Path 2 Scaling")]
    public float path2HealPerLevel = 8f;
    public float path2TempReductionPerLevel = 0.5f;

    [Header("Path 3 Scaling")]
    public float path3SkillHealPerLevel = 2f;
    public float path3SkillDurationPerLevel = 1f;
    public float path3RadiusPerLevel = 0.3f;

    public override string GetAttackDescription() =>
        $"Lobs a water droplet that bursts on landing, dealing {DamageTypeLabel(damageType)} in an area.";

    public override string GetPassiveDescription() =>
        "Water droplets also heal plants and reduce their temperature toward comfort. If an injured plant is within range, switches targeting to the one with the lowest Health.";

    public override string GetSkillDescription() =>
        "Channels briefly, then calls down a <color=#4FC3F7>Soothing Rain</color> on a targeted area, healing all plants within and reducing their temperature toward comfort over time.";
}
