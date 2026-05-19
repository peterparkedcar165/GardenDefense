using UnityEngine;

[CreateAssetMenu(fileName = "PoisonShroomData", menuName = "Scriptable Objects/PlantData/PoisonShroom")]
public class PoisonShroomData : PlantData
{
    public float basePoisonDPS;

    public override string GetAttackDescription() =>
        $"Blows poisonous bubbles at his target, dealing <color=green><b>{baseAttackDamage}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage.";

    public override string GetPassiveDescription() =>
        $"Attacks apply a <color=purple>Poison</color> effect on hit for <color=green><b>{basePassiveDuration}</b></color> seconds, dealing <color=green><b>{basePoisonDPS}</b></color> <color=purple>Poison</color> damage per second.";

    public override string GetSkillDescription() =>
        $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>{baseSkillRadius}</b></color> radius that lasts <color=green><b>{baseSkillDuration}</b></color> seconds. Insects standing in the field take <color=green><b>{basePoisonDPS}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per second, and any debuffs on them are frozen in time.";
}
