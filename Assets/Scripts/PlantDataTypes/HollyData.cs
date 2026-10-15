using UnityEngine;

[CreateAssetMenu(fileName = "HollyData", menuName = "Scriptable Objects/PlantData/Holly")]
public class HollyData : PlantData
{
    [Header("Passive - Snow Ward Shield")]
    public float baseShieldRampPerSecond = 4f;
    public float path2ShieldRampPerLevel = 2f;
    public float baseShieldMax = 40f;
    public float path2ShieldMaxPerLevel = 20f;
    public float shieldTickInterval = 0.5f;
    // how long Snow Ward lingers once a plant stops being refreshed by it (leaves the wind/blizzard)
    public float snowWardLingerDuration = 1f;
    // Path2 max level bonus: Snow Ward also grants this much flat Magic Resistance
    public float path2MaxMagicResistanceBonus = 0.15f;
    // Magic Power scaling: flat bonus per point of Holly's own Magic Power, same convention as
    // AloeVera's heal scaling (direct coefficient, not a per-100 normalization)
    public float shieldRampMagicPowerScaling = 0.1f;
    public float shieldMaxMagicPowerScaling  = 0.5f;

    [Header("Passive - Cooling")]
    public float baseCoolingPerSecond = 1f;
    public float path2CoolingPerLevel = 0.2f;

    [Header("Skill - Blizzard")]
    public float baseBlizzardDamagePerSecond = 20f;
    public float path3BlizzardDamagePerLevel = 4f;
    public float blizzardDamageMagicPowerScaling = 0.5f;
    public float blizzardTickInterval = 0.5f;
    public float baseBlizzardPowerMultiplier = 1.5f;
    public float path3BlizzardPowerPerLevel = 0.2f;
    public float path3SkillDurationPerLevel = 1f;
    public float path3BlizzardLengthPerLevel = 0.5f;
    public float path3BlizzardWidthPerLevel = 0.1f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 3f;
    public int path1MagicArmorPerLevel = 4;
    public float path1AttackRangePerLevel = 0.2f;

    public override string GetAttackDescription() =>
        $"Conjures a spiraling cold wind around herself, dealing {DamageTypeLabel(damageType)} to all insects within range.";
    public override string GetPassiveDescription() =>
        "Plants caught in the spiraling wind are afflicted with <color=#00FFFF><b>Snow Ward</b></color>, which grants a rampant <color=#00CED1><b>Shield</b></color>, cools them from hot weather, and grants immunity to <color=#E8D9A8><b>Sandstorm</b></color>. " +
        "Only the <b><color=#00FFFF>Holly</color></b> with the largest <color=#00CED1><b>Shield</b></color> cap affects a given plant - other Hollies cannot interfere or stack their shielding.";
    public override string GetSkillDescription() =>
        $"Conjures a blizzard blowing toward a targeted location, dealing {DamageTypeLabel(damageType)} per second to insects within it, and afflicting plants within it with the same <color=#00FFFF><b>Snow Ward</b></color> as her passive, at a higher power.";
}
