using UnityEngine;

[CreateAssetMenu(fileName = "CalendulaData", menuName = "Scriptable Objects/PlantData/Calendula")]
public class CalendulaData : PlantData
{
    public float baseFloralGlowHeal;
    public float baseSkillHealingMultiplier;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1FireDamagePerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    public float path2AttackRangePerLevel = 0.175f;

    [Header("Path 3 Scaling")]
    public float path3SkillDurationPerLevel = 2f;
    public float path3HealPerLevel = 1f;

    public override string GetAttackDescription() =>
        $"Releases flaming petals dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage to all insects within range.";

    public override string GetPassiveDescription() =>
        "Illuminates the surrounding area with a radius equal to her Attack Range.";

    public override string GetSkillDescription() =>
        $"Target a plant anywhere on the field to grant it <color=orange>Floral Glow</color>. The plant's projectiles deal additional {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage on hit, and the plant heals over time. Emits light equal to <b><color=orange>Calendula</color></b>'s range.";
}
