using UnityEngine;

[CreateAssetMenu(fileName = "AloeVeraData", menuName = "Scriptable Objects/PlantData/AloeVera")]
public class AloeVeraData : PlantData
{
    [Header("Aloe Vera")]
    public float baseHealAmount = 24f;
    public float baseTempReduction = 4.5f;

    public override string GetAttackDescription() =>
        $"Lob a water droplet that bursts on landing, dealing <color=green><b>{baseAttackDamage:F0}</b></color> <color=#4FC3F7>Water</color> <color=#FFB6C1>Magic</color> damage in an area.";

    public override string GetPassiveDescription() =>
        $"Water droplets also heal plants, restoring <color=green><b>{baseHealAmount:F0}</b></color> Health and reducing temperature by <color=#4FC3F7><b>{baseTempReduction:F1}</b></color>, until comfort. If an injured plant is within range, switch targetting to the one with the lowest Health.";

    public override string GetSkillDescription() =>
        $"To be determined.";
}
