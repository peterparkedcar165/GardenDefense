using UnityEngine;

[CreateAssetMenu(fileName = "BogIrisData", menuName = "Scriptable Objects/PlantData/BogIris")]
public class BogIrisData : PlantData
{
    public override string GetAttackDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Fires a water bolt at a single target dealing <color=green><b>{s.attackDamage:F0}</b></color> <color=#4FC3F7>Water</color> <color=#FFB6C1>Magic</color> damage.";
    }

    public override string GetPassiveDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Cycles between an <b><color=#4FC3F7>open</color></b> (<color=green><b>{s.openDuration:F0}s</b></color>) and <b><color=#4FC3F7>closed</color></b> (<color=green><b>{s.passiveCooldown:F1}s</b></color>) state.\n\n" +
               $"In <b><color=#4FC3F7>open</color></b> form, she generates <color=green><b>{s.sunGenerated}</b></color> Sun every <color=green><b>{s.sunInterval:F1}</b></color> seconds.\n\n" +
               $"In <b><color=#4FC3F7>closed</color></b> form, she regenerates <color=green><b>140</b></color> HP over <color=green><b>{s.passiveCooldown:F1}</b></color> seconds.";
    }

    public override string GetSkillDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Target a location. After a brief delay, a geyser erupts, dealing <color=green><b>{s.geyserDamage:F0}</b></color> <color=#4FC3F7>Water</color> <color=#FFB6C1>Magic</color> damage and knocking all insects airborne by <color=green><b>{s.knockUpHeight:F0}</b></color> units.";
    }
}
