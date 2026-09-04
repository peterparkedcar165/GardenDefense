using UnityEngine;

[CreateAssetMenu(fileName = "BirdOfParadiseData", menuName = "Scriptable Objects/PlantData/BirdOfParadise")]
public class BirdOfParadiseData : PlantData
{
    [Header("Path 1 Scaling (Talon Focus)")]
    public float path1AttackDamagePerLevel = 4f;
    public float path1AttackRangePerLevel = 0.2f;
    public float baseTalonFocusASPerStack = 0.04f;
    public float path1TalonFocusASPerStackPerLevel = 0.02f;
    [Tooltip("flat stack cap at every level - Talon Focus no longer scales its cap with Path1 level")]
    public int baseTalonFocusStackCap = 10;
    [Tooltip("Path1 max: on-hit damage dealt equal to this fraction of the target's missing health, only while at full Talon Focus stacks")]
    public float maxStacksHealthPercentDamage = 0.04f;
    [Tooltip("Path1 max: Total Attack Speed granted while sitting at full Talon Focus stacks")]
    public float maxStacksAttackSpeedBonus = 0.15f;

    [Header("Path 2 Scaling (Passive)")]
    public float basePassiveDamagePerStack = 3f;
    public float path2PassiveDamagePerStackPerLevel = 1f;
    [Tooltip("Path2 max: Armor Shred granted per Talon Focus stack")]
    public float maxLevelArmorShredPerStack = 0.02f;
    [Tooltip("Path2 max: every this many attack hits against the same target, the on-hit effect is applied an additional time after a short delay")]
    public int maxLevelExtraProcEveryNHits = 3;
    public float maxLevelExtraProcDelay = 0.15f;

    [Header("Path 3 Scaling (Three Talon Strike)")]
    public float baseSkillAttackSpeedBonus = 0.08f;
    public float path3AttackSpeedBonusPerLevel = 0.04f;
    public float path3SkillDurationPerLevel = 2f;

    public override string GetAttackDescription() =>
        $"Attacks a single target dealing {DamageTypeLabel(damageType)}, gaining a stack of Talon Focus.";

    public override string GetPassiveDescription() =>
        "Each stack of Talon Focus increases damage dealt on hit.";

    public override string GetSkillDescription() =>
        "Grants self Three Talon Strike, increasing Total Attack Speed and allowing attacks to hit additional nearest enemies.";
}
