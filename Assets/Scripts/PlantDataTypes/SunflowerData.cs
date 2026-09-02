using UnityEngine;

[CreateAssetMenu(fileName = "SunflowerData", menuName = "Scriptable Objects/PlantData/Sunflower")]
public class SunflowerData : PlantData
{
    public int baseSunGenerated;
    public float baseSunrayDPS;
    public float sunProcChance = 0.35f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackSpeedPerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    public int path2SunPerLevel = 2;
    public float path2ProcChancePerLevel = 0.03f;

    [Header("Path 3 Scaling")]
    public float path3SunrayDPSPerLevel = 15f;
    public float path3SkillDurationPerLevel = 0.5f;

    public override string GetAttackDescription() =>
        $"Briefly charges up a solar-powered energy orb then shoots it towards her target, dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        "Passively generates <color=yellow>Sun</color> for the garden periodically. Attacks have a chance to reduce the generation cooldown.";

    public override string GetSkillDescription() =>
        $"Gathers a large burst of energy from the sun, calling down a scorching beam from above that deals {DamageTypeLabel(damageType)} per second to insects within the designated area. Scales with <color=#FFB6C1>Magic Power</color>.";
}
