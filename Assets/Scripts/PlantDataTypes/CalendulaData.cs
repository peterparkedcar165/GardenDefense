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

    public override string GetAttackDescription() =>
        $"Releases flaming petals dealing {DamageTypeLabel(damageType)} to all insects within range.";

    public override string GetPassiveDescription() =>
        "Illuminates the surrounding area with a radius equal to her Attack Range.";

    public override string GetSkillDescription() =>
        $"Target a plant anywhere on the field to grant it <color=orange>Floral Glow</color>. The plant's attacks deal additional {DamageTypeLabel(damageType)} on hit. Emits light equal to <b><color=orange>Calendula</color></b>'s range.";
}
