using UnityEngine;

[CreateAssetMenu(fileName = "AcornSproutData", menuName = "Scriptable Objects/PlantData/AcornSprout")]
public class AcornSproutData : PlantData
{
    public float stunChance;
    public float stunDuration;

    public override string GetAttackDescription() =>
        $"Shoots acorns towards his target, dealing <color=green><b>{baseAttackDamage}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage.";

    public override string GetPassiveDescription() =>
        $"Attacks have a <color=green><b>{stunChance * 100f}%</b></color> chance to stun targets for <color=green><b>{stunDuration}</b></color> second.";

    public override string GetSkillDescription()
    {
        float impactDamage = baseAttackDamage * baseSkillDamageMultiplier;
        return $"Hurls a giant acorn from the sky at a targeted location, dealing <color=green><b>{impactDamage:F0}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage and stunning all insects in the impact radius for <color=green><b>2</b></color> seconds. The acorn then sits on the ground for <color=green><b>{baseSkillDuration}</b></color> seconds, blocking ground insects who stop to gnaw at it. The acorn can sustain <color=green><b>{baseSkillHealth:F0}</b></color> damage.";
    }
}
