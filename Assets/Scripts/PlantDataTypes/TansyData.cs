using UnityEngine;

[CreateAssetMenu(fileName = "TansyData", menuName = "Scriptable Objects/PlantData/Tansy")]
public class TansyData : PlantData
{
    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackRangePerLevel = 0.1f;

    [Header("Path 2 Scaling")]
    public float path2AttackRangePerLevel = 0.175f;

    [Header("Path 2 Base Effect: Distance Bonus Damage (Attack + Skill)")]
    [Tooltip("damage increase per integer of orbit radius above 1 (e.g. 0.06 = +6% per radius point)")]
    public float baseDistanceBonusDamagePerRadius = 0.06f;
    public float path2DistanceBonusDamagePerLevel = 0.01f;

    [Header("Path 3 Scaling")]
    public float path3SkillDurationPerLevel = 2f;
    public float floralGlowBaseDamage = 20f;
    public float floralGlowDamagePerLevel = 5f;
}
