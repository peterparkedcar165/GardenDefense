using UnityEngine;

[CreateAssetMenu(fileName = "CalendulaData", menuName = "Scriptable Objects/PlantData/Calendula")]
public class CalendulaData : PlantData
{
    public override string GetAttackDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Releases flaming petals dealing <color=green><b>{s.attackDamage:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage to all insects within range.";
    }

    public override string GetPassiveDescription()
    {
        return "Illuminate the surrounding area with a radius equal to <color=green><b>1.5×</b></color> her Attack Range.";
    }

    public override string GetSkillDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Target a plant anywhere on the field to grant <color=orange>Fiery Infusion</color> for <color=green><b>{s.skillDuration:F0}s</b></color>. The plant's projectiles deal an additional <color=green><b>{s.attackDamage:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage on hit. Heals the plant for <color=green><b>{s.fieryInfusionHeal:F0}</b></color> health per second. Emits light equal to <b><color=orange>Calendula</color></b>'s range.";
    }
}
