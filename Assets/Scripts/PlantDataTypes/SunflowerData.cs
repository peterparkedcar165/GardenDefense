using UnityEngine;

[CreateAssetMenu(fileName = "SunflowerData", menuName = "Scriptable Objects/PlantData/Sunflower")]
public class SunflowerData : PlantData
{
    public override string GetAttackDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Briefly charges up a solar-powered energy orb then shoots it towards her target, dealing <color=green><b>{s.attackDamage}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic </color>damage.";
    }

    public override string GetPassiveDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Passively generates <color=green><b>{s.sunGenerated}</b></color> <color=yellow>Sun</color> for the garden every <color=green><b>{s.passiveCooldown}</b></color> seconds. Attacks reduce the cooldown by <color=green><b>1</b></color> second.";
    }

    public override string GetSkillDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Gathers a large burst of energy from the sun, calling down a scorching beam from above that deals <color=green><b>{s.attackDamage}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage per second to insects within the designated area for <color=green><b>{s.skillDuration}</b></color> seconds.";
    }
}
