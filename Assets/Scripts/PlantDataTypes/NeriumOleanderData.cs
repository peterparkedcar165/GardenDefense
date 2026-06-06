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
    public int path1BouncePerLevel = 1;

    [Header("Path 2 Scaling")]
    public float path2ToxinDurationPerLevel = 2f;

    [Header("Path 3 Scaling")]
    public float path3SkillDamagePerLevel = 20f;
    public float path3RootDurationPerLevel = 0.5f;
    public float path3SkillRadiusPerLevel = 0.5f;

    public override string GetAttackDescription() =>
        $"Fires a toxic petal at the target dealing <color=green><b>{baseAttackDamage:F0}</b></color> {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage. The petal bounces to <color=green><b>{baseBounceCount}</b></color> additional target(s).";

    public override string GetPassiveDescription() =>
        $"Each petal hit applies <color=#9B59B6>Oleandic Toxin</color> for <color=green><b>{baseToxinDuration}s</b></color>, immediately cleansing a random buff and immunizing the insect to it.";

    public override string GetSkillDescription() =>
        $"Target an area. Instantly deals <color=green><b>{baseSkillFlatDamage:F0}</b></color> {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage to all insects within <color=green><b>{baseSkillRadius}</b></color> radius, rooting them and applying <color=#9B59B6>Oleandic Toxin</color>.";
}
