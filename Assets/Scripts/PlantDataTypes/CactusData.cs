using UnityEngine;

[CreateAssetMenu(fileName = "CactusData", menuName = "Scriptable Objects/PlantData/Cactus")]
public class CactusData : PlantData
{
    public float baseShieldAmount = 100f;

    [Header("Path 1 Scaling")]
    public int path1NeedlesBase = 8;
    public int path1NeedlesPerLevel = 3;

    [Header("Path 2 Scaling")]
    public float path2HealthPerLevel = 60f;

    [Header("Path 3 Scaling")]
    public float path3ShieldPerLevel = 10f;
    public float path3SkillDurationPerLevel = 2f;
    public float baseSkillHealBonus = 0.16f;
    public float path3HealBonusPerLevel = 0.04f;

    public override string GetAttackDescription() =>
        $"Fires needles in all directions, dealing <color=green><b>{baseAttackDamage}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage per needle hit. Each hit applies <color=#A0522D>Punctured</color>.";

    public override string GetPassiveDescription() =>
        $"Insects that attack the Cactus take damage equal to <color=green><b>150%</b></color> of their Attack Damage as <color=green>Nature</color> <color=#A0522D>Physical</color> damage. Applies <color=#A0522D>Punctured</color>.";

    public override string GetSkillDescription() =>
        $"Gains a <color=grey><b>{baseShieldAmount:F0}</b></color> shield and taunts insects within range for <color=green><b>{baseSkillDuration:F0}</b></color> seconds. Healing received increased by <color=green><b>{baseSkillHealBonus * 100f:F0}%</b></color> during the effect.";
}
