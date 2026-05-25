using UnityEngine;

[CreateAssetMenu(fileName = "AloeVeraData", menuName = "Scriptable Objects/PlantData/AloeVera")]
public class AloeVeraData : PlantData
{
    [Header("Aloe Vera")]
    public float baseHealAmount = 24f;
    public float baseTempReduction = 4.5f;

    [Header("Soothing Rain")]
    public float baseSkillHealPerTick = 10f;
    public float baseSkillHealInterval = 1f;
    public float baseSkillTempReduction = 2f;

    [Header("Path 1 Scaling")]
    public float path1AttackSpeedPerLevel = 0.02f;
    public float path1AttackRangePerLevel = 0.2f;

    [Header("Path 2 Scaling")]
    public float path2HealPerLevel = 8f;
    public float path2TempReductionPerLevel = 0.5f;

    [Header("Path 3 Scaling")]
    public float path3SkillHealPerLevel = 2f;
    public float path3SkillDurationPerLevel = 1f;
    public float path3RadiusPerLevel = 0.3f;

    public override string GetAttackDescription() =>
        $"Lob a water droplet that bursts on landing, dealing <color=green><b>{baseAttackDamage:F0}</b></color> <color=#4FC3F7>Water</color> <color=#FFB6C1>Magic</color> damage in an area.";

    public override string GetPassiveDescription() =>
        $"Water droplets also heal plants, restoring <color=green><b>{baseHealAmount:F0}</b></color> Health and reducing temperature by <color=#4FC3F7><b>{baseTempReduction:F1}</b></color>, until comfort. If an injured plant is within range, switch targetting to the one with the lowest Health.";

    public override string GetSkillDescription()
    {
        float totalHealing = baseSkillHealPerTick * Mathf.Floor(baseSkillDuration / baseSkillHealInterval);
        return $"Channels briefly, then calls down a Soothing Rain on a targeted area, healing all plants within <color=green><b>{baseSkillRadius:F1}</b></color> radius for <color=green><b>{baseSkillHealPerTick:F0}</b></color> Health and reducing temperature by <color=#4FC3F7><b>{baseSkillTempReduction:F1}</b></color>, until comfort, every <color=green><b>{baseSkillHealInterval:F1}s</b></color> over <color=green><b>{baseSkillDuration:F0}</b></color> seconds. " +
               $"For a total of <color=green><b>{totalHealing:F0}</b></color> Health.";
    }
}
