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

    public override string GetAttackDescription() =>
        $"An orbiting petal projectile deals {DamageTypeLabel(damageType)} to any insect it passes through.";

    public override string GetPassiveDescription() =>
        "Illuminates the surrounding area with a radius equal to her Attack Range.";

    public override string GetSkillDescription() =>
        $"Target a plant anywhere on the field to grant it <color=orange>Floral Glow</color>, summoning an orbiting petal projectile around it that deals {DamageTypeLabel(damageType)}, sourced from <b><color=orange>Tansy</color></b>, to anything it passes through. Emits light equal to <b><color=orange>Tansy</color></b>'s range.";
}
