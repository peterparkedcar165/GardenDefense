using UnityEngine;

[CreateAssetMenu(fileName = "PoisonShroomData", menuName = "Scriptable Objects/PlantData/PoisonShroom")]
public class PoisonShroomData : PlantData
{
    public float basePoisonDPS;
    public float baseToxicSporeDuration = 3f;

    [Header("Path 1 Scaling")]
    public float path1AttackSpeedPerLevel = 0.08f;
    public float path1AttackRangePerLevel = 0.1f;
    public float path1ToxicSporeDurationPerLevel = 0.4f;

    [Header("Path 2 Scaling")]
    public float baseCritChanceBonus = 0.1f;
    public float path2CritChancePerLevel = 0.03f;
    public float baseElementalAffinityBonus = 0.15f;
    public float path2ElementalAffinityPerLevel = 0.04f;
    public float path2MaxElementalEffectChanceBonus = 0.1f;
    public float path2MaxDotDurationBonus = 0.5f;

    [Header("Path 3 Scaling")]
    public float path3SkillDurationPerLevel = 1f;
    public float path3RadiusPerLevel = 0.2f;

    public override string GetAttackDescription() =>
        $"Fires Toxic Spores at the target, dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage over time.";

    public override string GetPassiveDescription() =>
        "Can deal Critical Damage with its Damage Over Time effects, and gains bonus Critical Chance and Elemental Affinity.";

    public override string GetSkillDescription() =>
        $"Hurls a toxic blob towards a targeted area, creating a poison field that lasts for a duration. Insects standing in the field take {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage per second, and any debuffs on them are frozen in time.";
}
