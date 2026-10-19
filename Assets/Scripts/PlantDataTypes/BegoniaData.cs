using UnityEngine;

[CreateAssetMenu(fileName = "BegoniaData", menuName = "Scriptable Objects/PlantData/Begonia")]
public class BegoniaData : PlantData
{
    public float baseElementalAffinityBonus = 0.24f;
    public float baseAttackSpeedBonus;
    public float baseGrassDamageBonus = 0.2f;
    public float basePassiveMultiplier;
    public float baseSkillMultiplier;
    public float basePrimerCooldownReduction = 0.5f;
    public float baseGerminateSpeedup = 0.5f;

    [Header("Path 1 Scaling")]
    public float path1AttackRangePerLevel = 0.2f;
    [Tooltip("flat seconds shaved off an insect's Water primer internal cooldown while standing in her flowers")]
    public float baseWaterCooldownReduction = 2f;
    public float path1CooldownReductionPerLevel = 0.6f;
    [Tooltip("Path1 max level bonus: Grass Resistance reduction on insects standing in her flowers")]
    public float maxLevelGrassResistanceShred = 0.35f;

    [Header("Path 2 Scaling")]
    public float path2ElementalAffinityPerLevel = 0.08f;
    public float path2GrassDamagePerLevel = 0.04f;
    public float path2GerminateSpeedupPerLevel = 0.1f;

    [Header("Path 3 Scaling")]
    public float path3AttackSpeedBonusPerLevel = 0.04f;
    public float path3SkillDurationPerLevel = 1f;
    public float path3PrimerCooldownReductionPerLevel = 0.1f;

    public override string GetAttackDescription() =>
        "Grows wet flowers around her, applying <color=#4FC3F7><b>Water</b></color> primer to insects in contact with them and reducing its internal cooldown.";

    public override string GetPassiveDescription() =>
        "Plants within her attack radius are granted <color=#4FC3F7><b>Begonia's Blessing</b></color>, increasing <color=green><b>Elemental Affinity</b></color> and <color=green><b>Grass Damage</b></color>, and making their <color=green><b>Germinate</b></color> triggers detonate earlier. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";

    public override string GetSkillDescription() =>
        "Plants within her attack range are granted <color=green><b>Blossoming</b></color>, increasing <color=green><b>Attack Speed</b></color>. While active, dealing <color=#4FC3F7><b>Water</b></color> or <color=green><b>Grass</b></color> damage reduces that insect's own primer cooldown for that element. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";
}
