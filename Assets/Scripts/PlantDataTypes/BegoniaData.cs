using UnityEngine;

[CreateAssetMenu(fileName = "BegoniaData", menuName = "Scriptable Objects/PlantData/Begonia")]
public class BegoniaData : PlantData
{
    public float baseelementalAffinityBonus;
    public float baseGrassDamageBonus;
    public float baseAttackSpeedBonus;
    public float basePassiveMultiplier;
    public float baseSkillMultiplier;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 4f;
    public float path1AttackRangePerLevel = 0.2f;

    [Header("Path 2 Scaling")]
    public float path2elementalAffinityPerLevel = 0.06f;

    [Header("Path 3 Scaling")]
    public float path3GrassDamageBonusPerLevel = 0.04f;
    public float path3AttackSpeedBonusPerLevel = 0.04f;
    public float path3RadiusPerLevel = 0.15f;

    public override string GetAttackDescription() =>
        $"Fires a magical bolt dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        "Plants within her attack radius are granted <color=green><b>Begonia's Blessing</b></color>, increasing <color=green><b>Elemental Affinity</b></color>. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";

    public override string GetSkillDescription() =>
        "Target an area on the field. Plants within the selected area are granted <color=green><b>Blossoming</b></color>, increasing <color=green><b>Grass Power</b></color> and <color=green><b>Attack Speed</b></color>. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";
}
