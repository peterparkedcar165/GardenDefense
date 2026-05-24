using UnityEngine;

[CreateAssetMenu(fileName = "SnowdropData", menuName = "Scriptable Objects/PlantData/Snowdrop")]
public class SnowdropData : PlantData
{
    [Header("Passive – Chill")]
    public float baseSlow    = 0.24f;   // base movement slow (fraction, e.g. 0.24 = 24%)
    public float scalingSlow = 0.06f;   // additional slow per Chill level above 1

    [Header("Passive – Cooling (plants in range)")]
    public float coolingPerSecond = 2f; // temperature reduced per second

    [Header("Skill – Blizzard")]
    public float baseBlizzardDamage       = 0f;
    public float blizzardDamagePerLevel   = 15f;  // DPS added per Path 3 level
    public float baseBlizzardDuration     = 5f;   // base duration in seconds
    public float blizzardDurationPerLevel = 1f;   // seconds added per Path 3 level
    public float blizzardChillMultiplier  = 1.5f; // how much stronger Blizzard Chill is vs passive
    public float blizzardCoolingMultiplier = 2f;  // how many times stronger the Blizzard Cooling is vs passive

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
