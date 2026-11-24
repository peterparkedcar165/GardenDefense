using UnityEngine;

[CreateAssetMenu(fileName = "NeriumOleanderData", menuName = "Scriptable Objects/PlantData/NeriumOleander")]
public class NeriumOleanderData : PlantData
{
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
}
