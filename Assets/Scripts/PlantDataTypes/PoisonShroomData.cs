using UnityEngine;

[CreateAssetMenu(fileName = "PoisonShroomData", menuName = "Scriptable Objects/PlantData/PoisonShroom")]
public class PoisonShroomData : PlantData
{
    public float basePoisonDuration;
    public float basePoisonDPS;

    [Header("Path 1 Scaling")]
    public float path1AttackSpeedPerLevel = 0.08f;
    public float path1AttackRangePerLevel = 0.1f;

    [Header("Path 2 Scaling")]
    public float path2PoisonDurationPerLevel = 1f;
    public float path2PoisonDPSPerLevel = 6f;

    [Header("Path 3 Scaling")]
    public float path3SkillDurationPerLevel = 1f;
    public float path3RadiusPerLevel = 0.2f;

    public override string GetAttackDescription() =>
        $"Blows poisonous bubbles at his target, dealing <color=green><b>{baseAttackDamage}</b></color> {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        $"Attacks apply a <color=purple>Poison</color> effect on hit for <color=green><b>{basePoisonDuration}</b></color> seconds, dealing <color=green><b>{basePoisonDPS}</b></color> {ElementalTag(elementalType)} damage per second.";

    public override string GetSkillDescription() =>
        $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>{baseSkillRadius}</b></color> radius that lasts <color=green><b>{baseSkillDuration}</b></color> seconds. Insects standing in the field take <color=green><b>{basePoisonDPS}</b></color> {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage per second, and any debuffs on them are frozen in time.";
}
