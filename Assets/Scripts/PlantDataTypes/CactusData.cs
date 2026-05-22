using UnityEngine;

[CreateAssetMenu(fileName = "CactusData", menuName = "Scriptable Objects/PlantData/Cactus")]
public class CactusData : PlantData
{
    public override string GetAttackDescription() =>
        $"Fires needles in all directions, dealing <color=green><b>{baseAttackDamage}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage per needle hit. Each hit applies <color=#A0522D>Punctured</color>.";

    public override string GetPassiveDescription() =>
        $"Insects that attack the Cactus take damage equal to <color=green><b>150%</b></color> of their Attack Damage as <color=green>Nature</color> <color=#A0522D>Physical</color> damage. Applies <color=#A0522D>Punctured</color>.";

    public override string GetSkillDescription() =>
        $"Gains a <color=grey><b>100</b></color> shield and taunts insects within range for <color=green><b>12</b></color> seconds. Healing received increased by <color=green><b>16%</b></color> during the effect.";
}
