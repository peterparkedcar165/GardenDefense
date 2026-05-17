using UnityEngine;

[CreateAssetMenu(fileName = "AcornSproutData", menuName = "Scriptable Objects/PlantData/AcornSprout")]
public class AcornSproutData : PlantData
{
    public override string GetAttackDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Shoots acorns towards his target, dealing <color=green><b>{s.attackDamage}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage.";
    }

    public override string GetPassiveDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Attacks have a <color=green><b>{s.stunChance * 100f}%</b></color> chance to stun targets for <color=green><b>{s.stunDuration}</b></color> second.";
    }

    public override string GetSkillDescription()
    {
        var s = plantPrefab.GetBaseStats();
        float impactDamage = s.attackDamage * s.skillDamageMultiplier;
        return $"Hurls a giant acorn from the sky at a targeted location, dealing <color=green><b>{impactDamage:F0}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage and stunning all insects in the impact radius for <color=green><b>2</b></color> seconds. The acorn then sits on the ground for <color=green><b>{s.skillDuration}</b></color> seconds, blocking ground insects who stop to gnaw at it. The acorn can sustain <color=green><b>250</b></color> damage.";
    }
}
