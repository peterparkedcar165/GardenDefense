using UnityEngine;

[CreateAssetMenu(fileName = "BegoniaData", menuName = "Scriptable Objects/PlantData/Begonia")]
public class BegoniaData : PlantData
{
    public float baseCritChanceBonus = 0.08f;
    public float baseMaxDamageBonus = 0.08f;
    public float baseAttackSpeedBonus;
    public float baseAttackDamageBonus = 0.2f;
    public float basePassiveMultiplier;
    public float baseSkillMultiplier;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 4f;
    public float path1AttackRangePerLevel = 0.2f;

    [Header("Path 2 Scaling")]
    public float path2CritChancePerLevel = 0.04f;
    public float path2MaxDamagePerLevel = 0.04f;

    [Header("Path 3 Scaling")]
    public float path3AttackDamagePerLevel = 0.06f;
    public float path3AttackSpeedBonusPerLevel = 0.04f;
    public float path3RadiusPerLevel = 0.15f;
    public float path3SkillDurationPerLevel = 1f;

    public override string GetAttackDescription() =>
        $"Fires a magical bolt dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        "Plants within her attack radius are granted <color=green><b>Begonia's Blessing</b></color>, increasing <color=green><b>Critical Chance</b></color> and <color=green><b>Maximum Damage</b></color>. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";

    public override string GetSkillDescription() =>
        "Target an area on the field. Plants within the selected area are granted <color=green><b>Blossoming</b></color>, increasing <color=green><b>Attack Damage</b></color> and <color=green><b>Attack Speed</b></color>. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";
}
