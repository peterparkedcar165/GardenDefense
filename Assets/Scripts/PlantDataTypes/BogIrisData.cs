using UnityEngine;

[CreateAssetMenu(fileName = "BogIrisData", menuName = "Scriptable Objects/PlantData/BogIris")]
public class BogIrisData : PlantData
{
    public float baseOpenDuration;
    public int baseSunGenerated;
    public float baseKnockUpHeight;
    public float baseGeyserDamage;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;

    [Header("Path 2 Scaling")]
    public float path2OpenDurationPerLevel = 2f;
    public int path2SunPerLevel = 1;

    [Header("Path 3 Scaling")]
    public float path3GeyserDamagePerLevel = 15f;
    public float path3KnockUpPerLevel = 1f;
    public float path3GeyserRadiusPerLevel = 0.15f;

    public override string GetAttackDescription() =>
        $"Fires a water bolt at a single target dealing <color=green><b>{baseAttackDamage:F0}</b></color> <color=#4FC3F7>Water</color> <color=#FFB6C1>Magic</color> damage.";

    public override string GetPassiveDescription() =>
        $"Cycles between an <b><color=#4FC3F7>open</color></b> (<color=green><b>{baseOpenDuration:F0}s</b></color>) and <b><color=#4FC3F7>closed</color></b> (<color=green><b>{basePassiveCooldown:F1}s</b></color>) state.\n\n" +
        $"In <b><color=#4FC3F7>open</color></b> form, she generates <color=green><b>{baseSunGenerated}</b></color> Sun every <color=green><b>2</b></color> seconds.\n\n" +
        $"In <b><color=#4FC3F7>closed</color></b> form, she regenerates <color=green><b>200</b></color> HP over <color=green><b>{basePassiveCooldown:F1}</b></color> seconds.";

    public override string GetSkillDescription() =>
        $"Target a location. After a brief delay, a geyser erupts, dealing <color=green><b>{baseGeyserDamage:F0}</b></color> <color=#4FC3F7>Water</color> <color=#FFB6C1>Magic</color> damage and knocking all insects airborne by <color=green><b>{baseKnockUpHeight:F0}</b></color> units.";
}
