using UnityEngine;

[CreateAssetMenu(fileName = "SnowdropData", menuName = "Scriptable Objects/PlantData/Snowdrop")]
public class SnowdropData : PlantData
{
    [Header("Passive Chill")]
    public float baseSlow    = 0.24f;
    public float scalingSlow = 0.06f;

    [Header("Passive Cooling")]
    public float coolingPerSecond = 2f;

    [Header("Skill Blizzard")]
    public float baseBlizzardDamage        = 0f;
    public float blizzardDamagePerLevel    = 15f;
    public float baseBlizzardDuration      = 5f;
    public float blizzardDurationPerLevel  = 1f;
    public float blizzardChillMultiplier   = 1.5f;
    public float blizzardCoolingMultiplier = 2f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 1f;
    public float path1AttackRangePerLevel  = 0.1f;

    [Header("Path 3 Scaling")]
    public float path3BlizzardWidthPerLevel = 0.5f;

    public override string GetAttackDescription() =>
        $"Continuously deals <color=green><b>{baseAttackDamage}</b></color> <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage to all ground-level insects within range.";

    public override string GetPassiveDescription() =>
        $"Applies <color=#00FFFF>Chill</color> to nearby insects, slowing their movement by <color=green><b>{baseSlow * 100f:F0}%</b></color>, until comfort.\n\n" +
        $"Plants within the radius receive <color=#00FFFF>Cooling</color>, reducing temperature by <color=green><b>{coolingPerSecond:F1}</b></color> per second.";

    public override string GetSkillDescription() =>
        $"Summon a strong blizzard, aiming it towards the targeted area. The blizzard deals <color=green><b>{baseBlizzardDamage}</b></color> <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage per second to insects caught in the area, " +
        $"and applies <color=#00FFFF>Chill</color> at <color=green><b>{blizzardChillMultiplier:F1}×</b></color> strength. " +
        $"Plants within the Blizzard also receive <color=#00FFFF>Cooling</color> effect for <color=green><b>{blizzardCoolingMultiplier:F1}×</b></color> the effect.";
}
