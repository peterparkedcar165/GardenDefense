using UnityEngine;

[CreateAssetMenu(fileName = "CactusData", menuName = "Scriptable Objects/PlantData/Cactus")]
public class CactusData : PlantData
{
    public float baseShieldAmount = 100f;

    [Header("Path 1 Scaling")]
    public int path1NeedlesBase = 8;
    public int path1NeedlesPerLevel = 3;

    [Header("Path 2 Scaling")]
    public float path2HealthPerLevel = 60f;

    [Header("Path 3 Scaling")]
    public float path3ShieldPerLevel = 10f;
    public float path3SkillDurationPerLevel = 2f;
    public float baseSkillHealBonus = 0.16f;
    public float path3HealBonusPerLevel = 0.04f;

    public override string GetAttackDescription() =>
        $"Fires needles in all directions, dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage per needle hit. Each hit applies <color=#A0522D>Punctured</color>.";

    public override string GetPassiveDescription() =>
        $"Insects that attack the Cactus take {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage in return. Applies <color=#A0522D>Punctured</color>.";

    public override string GetSkillDescription() =>
        "Grants a shield for a duration. While the shield holds, nearby insects are forced to target the Cactus.";
}
