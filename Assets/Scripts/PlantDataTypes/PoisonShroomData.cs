using UnityEngine;

[CreateAssetMenu(fileName = "PoisonShroomData", menuName = "Scriptable Objects/PlantData/PoisonShroom")]
public class PoisonShroomData : PlantData
{
    public override string GetAttackDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Blows poisonous bubbles at his target, dealing <color=green><b>{s.attackDamage}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage.";
    }

    public override string GetPassiveDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Attacks apply a <color=purple>Poison</color> effect on hit for <color=green><b>{s.poisonDuration}</b></color> seconds, dealing <color=green><b>{s.poisonDamagePerSecond}</b></color> <color=purple>Poison</color> damage per second.";
    }

    public override string GetSkillDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>1</b></color> radius that lasts <color=green><b>{s.skillDuration}</b></color> seconds. Insects standing in the field take <color=green><b>12</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per second, and any debuffs on them are frozen in time.";
    }
}
