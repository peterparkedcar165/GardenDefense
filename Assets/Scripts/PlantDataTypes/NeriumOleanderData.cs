using UnityEngine;

[CreateAssetMenu(fileName = "NeriumOleanderData", menuName = "Scriptable Objects/PlantData/NeriumOleander")]
public class NeriumOleanderData : PlantData
{
    public int baseBounceCount = 1;
    public float baseToxinDuration = 6f;
    public float bounceDamageReduction = 0.1f;
    public float baseSproutDuration = 15f;
    public float baseCursePoisonResistReduction = 0.04f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 10f;
    public float path1AttackRangePerLevel = 0.5f;
    public int path1BouncePerLevel = 1;
    public float path1MaxPoisonExtendPerHit = 1f;

    [Header("Path 2 Scaling")]
    public float path2ToxinDurationPerLevel = 2f;
    public float path2MaxMagicArmorPerLock = 12f;

    [Header("Path 3 Scaling")]
    public float path3SproutDurationPerLevel = 3f;
    public float path3CurseReductionPerLevel = 0.01f;
    public int path3MaxBounceBonus = 3;

    public override string GetAttackDescription() =>
        $"Fires a toxic petal at the target dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage. The petal bounces to additional targets.";

    public override string GetPassiveDescription() =>
        "Each petal hit applies <color=#9B59B6>Oleandic Toxin</color>, immediately cleansing a random buff and immunizing the insect to it.";

    public override string GetSkillDescription() =>
        "Places an Oleander Sprout that petals can freely bounce through, cursing nearby insects with reduced <color=purple>Poison Resistance</color> for as long as it stands.";
}
