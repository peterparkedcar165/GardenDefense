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
    public float baseShieldArmorBonus = 25f;
    public float path3ShieldArmorPerLevel = 5f;

    public override string GetAttackDescription() =>
        $"Fires needles in all directions, dealing {DamageTypeLabel(damageType)} per needle hit. Each hit applies <color=#A0522D>Punctured</color>.";

    public override string GetPassiveDescription() =>
        $"Insects that attack the Cactus take {DamageTypeLabel(damageType)} in return. Applies <color=#A0522D>Punctured</color>.";

    public override string GetSkillDescription() =>
        "Grants a shield for a duration. While shielded, the Cactus gains bonus Armor and nearby insects are forced to target it.";
}
