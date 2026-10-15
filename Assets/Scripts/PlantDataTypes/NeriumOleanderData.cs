using UnityEngine;

[CreateAssetMenu(fileName = "NeriumOleanderData", menuName = "Scriptable Objects/PlantData/NeriumOleander")]
public class NeriumOleanderData : PlantData
{
    public int baseBounceCount = 1;
    public float bounceDamageReduction = 0.1f;
    public float baseSproutDuration = 15f;
    public float baseCursePoisonResistReduction = 0.04f;

    [Header("Passive - Oleandic Ward")]
    public float baseWardDuration = 6f;
    // weak on purpose - the real value is wardRegenPerLock, below
    public float baseWardHealAmount = 5f;
    public float wardRegenPerLock = 3f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 10f;
    public float path1AttackRangePerLevel = 0.5f;
    public int path1BouncePerLevel = 1;
    public float path1MaxPoisonExtendPerHit = 1f;

    [Header("Path 2 Scaling")]
    public float path2WardDurationPerLevel = 2f;
    // Path2 max level bonus: Ward also grants this much flat Magic Resistance per debuff locked
    public float path2MaxMagicResistancePerLock = 0.05f;

    [Header("Path 3 Scaling")]
    public float path3SproutDurationPerLevel = 3f;
    public float path3CurseReductionPerLevel = 0.01f;
    public int path3MaxBounceBonus = 3;

    public override string GetAttackDescription() =>
        $"Fires a toxic petal at the target dealing {DamageTypeLabel(damageType)}. The petal bounces to additional targets.";

    public override string GetPassiveDescription() =>
        "Nearby allies are granted <color=#2ECC71>Oleandic Ward</color>, immediately cleansing a random debuff and locking it out, with a weak heal and a regeneration that scales with how many debuffs are locked.";

    public override string GetSkillDescription() =>
        "Places an Oleander Sprout that petals can freely bounce through, cursing nearby insects with reduced <color=purple>Poison Resistance</color> for as long as it stands.";
}
