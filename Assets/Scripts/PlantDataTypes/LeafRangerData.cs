using UnityEngine;

[CreateAssetMenu(fileName = "LeafRangerData", menuName = "Scriptable Objects/PlantData/LeafRanger")]
public class LeafRangerData : PlantData
{
    public float baseSkillAttackSpeedBonus;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackSpeedPerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    public float baseCritChanceBonus = 0.1f;       // bonus crit chance granted at level 0 before upgrades
    public float path2CritChancePerLevel = 0.05f;  // additional crit chance per passive level
    public float baseBounceRadius = 2f;            // how far an arrow can seek its next bounce target
    public float path2BounceRadiusPerLevel = 0.5f;
    public float bounceSpeedMultiplier = 0.66f;    // projectile speed from the first bounce onward (% of original)

    [Header("Path 3 Scaling")]
    public float path3AttackSpeedBonusPerLevel = 0.25f;
    public float path3SkillDurationPerLevel = 0.5f;
}
