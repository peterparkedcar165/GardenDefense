using UnityEngine;

[CreateAssetMenu(fileName = "WaterlilyData", menuName = "Scriptable Objects/PlantData/Waterlily")]
public class WaterlilyData : PlantData
{
    public float baseAoERange;
    public float baseBubblePrisonImpactDamage;

    [Header("Path 1 Scaling")]
    public float path1AttackRangePerLevel = 0.5f;
    public float path1AttackSpeedPerLevel = 0.3f;

    [Header("Path 2 Scaling")]
    public float path2AoERangePerLevel = 0.05f;
    public float path2SplashDamageScalingPerLevel = 0.05f;

    [Header("Path 3 Scaling")]
    public float path3BubbleDamagePerLevel = 12f;
    public float path3SkillDurationPerLevel = 2f;
    public float path3RadiusPerLevel = 0.2f;

    public override string GetAttackDescription() =>
        $"Blow little bubbles towards her target, dealing <color=green><b>{baseAttackDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic </color>damage.";

    public override string GetPassiveDescription() =>
        $"Attacks deal <color=green><b>{basePassiveDamage}</b></color> <color=#3399FF>Water</color> damage to surrounding insects within a <color=green><b>{baseAoERange}</b></color> radius.";

    public override string GetSkillDescription() =>
        $"Blow a large bubble onto a targetted area, trapping insects within the bubble while dealing <color=green><b>{baseBubblePrisonImpactDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic</color> damage upon impact, and keeping them airborne for <color=green><b>{baseSkillDuration}</b></color> seconds.";
}
