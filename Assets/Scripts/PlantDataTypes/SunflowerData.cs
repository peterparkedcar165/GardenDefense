using UnityEngine;

[CreateAssetMenu(fileName = "SunflowerData", menuName = "Scriptable Objects/PlantData/Sunflower")]
public class SunflowerData : PlantData
{
    public int baseSunGenerated;
    public float baseSunrayDPS;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackSpeedPerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    public int path2SunPerLevel = 2;

    [Header("Path 3 Scaling")]
    public float path3SunrayDPSPerLevel = 15f;
    public float path3SkillDurationPerLevel = 0.5f;

    public override string GetAttackDescription() =>
        $"Briefly charges up a solar-powered energy orb then shoots it towards her target, dealing <color=green><b>{baseAttackDamage}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic </color>damage.";

    public override string GetPassiveDescription() =>
        $"Passively generates <color=green><b>{baseSunGenerated}</b></color> <color=yellow>Sun</color> for the garden every <color=green><b>{basePassiveCooldown}</b></color> seconds. Attacks reduce the cooldown by <color=green><b>1</b></color> second.";

    public override string GetSkillDescription() =>
        $"Gathers a large burst of energy from the sun, calling down a scorching beam from above that deals <color=green><b>{baseSunrayDPS}</b></color> (+{baseSkillDamageMultiplier * 100f:F0}% <color=#FFB6C1>Magic Power</color>) <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage per second to insects within the designated area for <color=green><b>{baseSkillDuration}</b></color> seconds.";
}
