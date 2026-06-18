using UnityEngine;

[CreateAssetMenu(fileName = "RhodiolaData", menuName = "Scriptable Objects/PlantData/Rhodiola")]
public class RhodiolaData : PlantData
{
    [Header("Path 1 Scaling")]
    public float path1AttackSpeedPerLevel  = 0.08f;
    public float path1AttackRangePerLevel  = 0.2f;
    public float path1HealingBonusPerLevel = 0.03f;

    [Header("Path 2 Scaling")]
    public float baseBurgeonHealPerTick = 2f;
    public float baseBurgeonDuration    = 4f;
    public float burgeonTickInterval    = 0.5f;
    public float path2HealPerLevel      = 1f;

    [Header("Path 3 Scaling")]
    public float revivalHealthPercent           = 0.2f;
    public float path3CooldownReductionPerLevel = 0f;
    public float verdantGuardianShield          = 200f;
    public float verdantGuardianRegen           = 20f;
    public float verdantGuardianDuration        = 8f;

    public override string GetAttackDescription() =>
        $"Instantly deals <color=green><b>{baseAttackDamage:F0}</b></color> {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage to the target.";

    public override string GetPassiveDescription() =>
        $"Attacks inflict <color=green><b>Rejuvenating Seed</b></color> on the target. When the target is attacked by a plant, that plant is granted <color=green><b>Rejuvenating Burgeon</b></color>, healing it over {baseBurgeonDuration:F0} seconds.";

    public override string GetSkillDescription() =>
        $"Target a tile where a plant has fallen to resurrect it. The plant is revived with <color=green><b>{revivalHealthPercent * 100f:F0}%</b></color> of its maximum health.";
}
