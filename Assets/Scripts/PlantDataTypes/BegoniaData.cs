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

    [Header("Path 1 Scaling")]
    public float path1AttackRangePerLevel = 0.2f;
    public float path1AttackSpeedPerLevel = 0.1f;

    [Header("Path 2 Scaling")]
    public float path2ElementalAffinityPerLevel = 0.08f;
    public float path2GrassDamagePerLevel = 0.04f;

    [Header("Path 3 Scaling")]
    public float path3AttackSpeedBonusPerLevel = 0.04f;
    public float path3RadiusPerLevel = 0.15f;
    public float path3SkillDurationPerLevel = 1f;
    public float path3PrimerCooldownReductionPerLevel = 0.1f;

    public override string GetAttackDescription() =>
        $"Fires a magical bolt dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        "Plants within her attack radius are granted <color=#4FC3F7><b>Begonia's Blessing</b></color>, increasing <color=green><b>Elemental Affinity</b></color> and <color=green><b>Grass Damage</b></color>. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";

    public override string GetSkillDescription() =>
        "Target an area on the field. Plants within the selected area are granted <color=green><b>Blossoming</b></color>, increasing <color=green><b>Attack Speed</b></color>. While active, dealing <color=#4FC3F7><b>Water</b></color> or <color=green><b>Grass</b></color> damage reduces that insect's own primer cooldown for that element. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";
}
