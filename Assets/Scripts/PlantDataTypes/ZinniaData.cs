using UnityEngine;

[CreateAssetMenu(fileName = "ZinniaData", menuName = "Scriptable Objects/PlantData/Zinnia")]
public class ZinniaData : PlantData
{
    [Header("Ablaze Detonation")]
    public float baseDetonationMultiplier = 0.6f;
    public float baseDetonationFlat       = 15f;

    [Header("Passive Aura Base")]
    public float baseFireDamageBonus;
    public float baseMagicPowerBonus;
    public float basePassiveMultiplier;
    public float baseElementalAffinityBonus = 0.1f;

    [Header("Path 1 Scaling")]
    public float path1AttackSpeedPerLevel  = 0.08f;
    public float path1AttackRangePerLevel  = 0.15f;
    public float path1MaxAttackSpeedBonus  = 0.4f;

    [Header("Path 2 Scaling")]
    public float path2FireDamageBonusPerLevel  = 0.04f;
    public float path2MagicPowerBonusPerLevel  = 5f;

    [Header("Path 3 Scaling")]
    public float path3SkillDurationPerLevel = 2f;
    public int   baseSkillIntensity         = 0;
    public int   path3SunIntensityPerLevel  = 1;

    public override string GetAttackDescription() =>
        $"Releases fiery sparks dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage to nearby insects. Nearby plants take no damage and are instead marked with <color=orange><b>Ablaze</b></color>.";

    public override string GetPassiveDescription() =>
        "Plants within her radius gain <color=orange><b>Zinnia's Warmth</b></color>, increasing <color=orange><b>Fire Damage</b></color> and <color=#FFB6C1><b>Magic Power</b></color>.";

    public override string GetSkillDescription() =>
        "Call upon the sun, changing the weather to <color=orange><b>Sunny</b></color> for a duration.";
}
