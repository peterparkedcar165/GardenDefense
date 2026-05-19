using UnityEngine;

[CreateAssetMenu(fileName = "BegoniaData", menuName = "Scriptable Objects/PlantData/Begonia")]
public class BegoniaData : PlantData
{
    public float baseElementalPowerBonus;
    public float baseNatureDamageBonus;
    public float baseAttackSpeedBonus;
    public float basePassiveMultiplier;
    public float baseSkillMultiplier;

    public override string GetAttackDescription() =>
        $"Fire a magical bolt dealing <color=green><b>{baseAttackDamage:F0}</b></color> <color=green>Nature</color> <color=#FFB6C1>Magic</color> damage.";

    public override string GetPassiveDescription() =>
        $"Plants within her attack radius are granted <color=green><b>Begonia's Blessing</b></color>, increasing Elemental Power by <color=green><b>{baseElementalPowerBonus * 100f:F0}%</b></color>. " +
        $"Scales with <color=#FFB6C1><b>{basePassiveMultiplier * 100f:F0}%</b></color> Magic Power.";

    public override string GetSkillDescription() =>
        $"Target an area on the field. Plants within the selected area are granted <color=green><b>Blossoming</b></color> for <color=green><b>{baseSkillDuration:F0}s</b></color>, " +
        $"increasing Nature Power by <color=green><b>{baseNatureDamageBonus * 100f:F0}%</b></color> and Attack Speed by <color=green><b>{baseAttackSpeedBonus * 100f:F0}%</b></color>. " +
        $"Scales with <color=#FFB6C1><b>{baseSkillMultiplier * 100f:F0}%</b></color> Magic Power.";
}
