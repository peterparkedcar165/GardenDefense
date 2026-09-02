using UnityEngine;

[CreateAssetMenu(fileName = "HollyData", menuName = "Scriptable Objects/PlantData/Holly")]
public class HollyData : PlantData
{
    public float baseRetaliationHollyPercent;
    public float baseRetaliationInsectPercent;
    public float baseHealthBonusMP;
    public float baseFrozenRageReduction;
    public float baseFrozenRageReductionMP;
    public float baseSkillShield;
    public float baseSkillShieldMP;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 4f;
    public int path1ArmorPerLevel = 8;

    [Header("Path 2 Scaling")]
    public float path2HealthPerLevel = 40f;
    public float path2RetaliationPerLevel = 0.05f;

    [Header("Path 3 Scaling")]
    public float path3ShieldPerLevel = 20f;
    public float path3FrozenRagePerLevel = 0.04f;
    public float path3SkillDurationPerLevel = 2f;

    public override string GetAttackDescription() =>
        $"Releases icy thorns dealing {DamageTypeLabel(damageType)} to all insects within range.";

    public override string GetPassiveDescription() =>
        "Insects that attack <b><color=#00FFFF>Holly</color></b> receive retaliatory damage. Max Health scales with <color=#FFB6C1><b>Magic Power</b></color>. " +
        "While shielded, insects within range are afflicted with <color=#00FFFF><b>Frozen Rage</b></color>, forcing them to target her and reducing their Armor. Taunted insects deal reduced Attack damage.";

    public override string GetSkillDescription() =>
        "Grants <b><color=#00FFFF>Holly</color></b> a shield for a duration. While the shield holds, nearby insects are forced to target her via <color=#00FFFF><b>Frozen Rage</b></color>.";
}
