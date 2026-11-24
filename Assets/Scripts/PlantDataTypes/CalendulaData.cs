using UnityEngine;

[CreateAssetMenu(fileName = "CalendulaData", menuName = "Scriptable Objects/PlantData/Calendula")]
public class CalendulaData : PlantData
{
    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1FireDamagePerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    public float path2AttackRangePerLevel = 0.175f;
    [Tooltip("Illuminate's max-level bonus: Fire Damage granted to every plant within illumination range")]
    public float maxLevelFireDamageBonus = 0.06f;

    [Header("Path 3 Scaling")]
    public float path3SkillDurationPerLevel = 2f;
    public float floralGlowBaseDamageScaling = 0.25f;
    public float floralGlowDamageScalingPerLevel = 0.05f;
    [Tooltip("Floral Glow's max-level bonus: Attack Speed granted to the carrier for as long as Floral Glow is active")]
    public float floralGlowMaxLevelAttackSpeedBonus = 0.15f;
    [Tooltip("how fast the Floral Glow delivery projectile (see Calendula.floralGlowProjectilePrefab) travels toward its target")]
    public float floralGlowProjectileSpeed = 8f;
}
