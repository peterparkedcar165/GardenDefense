using UnityEngine;

[CreateAssetMenu(fileName = "SnowdropData", menuName = "Scriptable Objects/PlantData/Snowdrop")]
public class SnowdropData : PlantData
{
    public override string GetAttackDescription() =>
        $"Freezes the ground around her continuously dealing <color=green><b>{baseAttackDamage}</b></color> <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage per second to insects.";

    public override string GetPassiveDescription() =>
        $"The frosty aura applies a <color=#00FFFF>Chill</color> effect, slowing down insects by <color=green><b>24%</b></color>.";

    public override string GetSkillDescription() =>
        $"Summon a strong blizzard, aiming it towards the targeted area. The blizzard deals <color=green><b>{baseSkillDamage}</b></color> <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage per second to insects caught in the area, and applies <color=#00FFFF>Chill</color> with an additional level.";
}
