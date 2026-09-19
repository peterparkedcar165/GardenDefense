using UnityEngine;

[CreateAssetMenu(fileName = "PoisonShroomData", menuName = "Scriptable Objects/PlantData/PoisonShroom")]
public class PoisonShroomData : PlantData
{
    public float basePoisonDPS;
    public float baseToxicSporeDuration = 3f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;
    public float path1AttackSpeedPerLevel = 0.08f;
    public float path1AttackRangePerLevel = 0.1f;

    [Header("Path 2 Scaling")]
    public float path2ToxicSporeDurationPerLevel = 0.4f;
    // Toxic Spore also deals bonus damage equal to a fraction of the target's CURRENT health
    // per second while active
    public float basePercentHealthDPS = 0.012f;
    public float path2PercentHealthDPSPerLevel = 0.004f;

    [Header("Path 3 Scaling")]
    public float path3SkillDurationPerLevel = 1f;
    public float path3RadiusPerLevel = 0.2f;

    public override string GetAttackDescription() =>
        $"Fires Toxic Spores at the target, dealing {DamageTypeLabel(damageType)} over time.";

    public override string GetPassiveDescription() =>
        "Toxic Spores also deal bonus damage equal to a percentage of the target's current health while active, and last longer per level. When fully grown, gains the ability to deal Critical Damage with Damage Over Time effects, along with bonus Critical Chance.";

    public override string GetSkillDescription() =>
        $"Hurls a toxic blob towards a targeted area, creating a poison field that lasts for a duration. Insects standing in the field take {DamageTypeLabel(damageType)} per second, and any debuffs on them are frozen in time.";
}
