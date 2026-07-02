using UnityEngine;

[CreateAssetMenu(fileName = "NeriumOleanderData", menuName = "Scriptable Objects/PlantData/NeriumOleander")]
public class NeriumOleanderData : PlantData
{
    public int baseBounceCount = 1;
    public float baseToxinDuration = 6f;
    public float baseSkillFlatDamage = 50f;
    public float bounceSearchRadius = 6f;
    public float bounceDamageReduction = 0.1f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 10f;
    public float path1AttackRangePerLevel = 0.5f;
    public int path1BouncePerLevel = 1;

    [Header("Path 2 Scaling")]
    public float path2ToxinDurationPerLevel = 2f;

    [Header("Path 3 Scaling")]
    public float path3SkillDamagePerLevel = 20f;
    public float path3RootDurationPerLevel = 0.5f;
    public float path3SkillRadiusPerLevel = 0.5f;

    public override string GetAttackDescription() =>
        $"Fires a toxic petal at the target dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage. The petal bounces to additional targets.";

    public override string GetPassiveDescription() =>
        "Each petal hit applies <color=#9B59B6>Oleandic Toxin</color>, immediately cleansing a random buff and immunizing the insect to it.";

    public override string GetSkillDescription() =>
        $"Target an area. Deals {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage to all insects within, rooting them and applying <color=#9B59B6>Oleandic Toxin</color>.";
}
