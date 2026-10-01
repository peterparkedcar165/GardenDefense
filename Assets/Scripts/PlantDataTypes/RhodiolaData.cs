using UnityEngine;

[CreateAssetMenu(fileName = "RhodiolaData", menuName = "Scriptable Objects/PlantData/Rhodiola")]
public class RhodiolaData : PlantData
{
    // healing is driven by the standard AttackDamage/AttackSpeed stats (see Rhodiola.HealAmount and
    // Update()) rather than a bespoke heal-per-second/tick-interval pair, so generic Attack Damage
    // and Attack Speed sources (fertilizers, skill tree, etc.) affect her healing for free
    [Header("Attack")]
    public float attackHealMPScaling = 0.05f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 2f;
    public float path1ProjectileSpeedPerLevel = 0.5f;
    public float maxMissingHealthPercent = 0.05f;

    [Header("Path 2 Scaling")]
    public float baseGrassConversion = 0.5f;
    public float path2GrassConversionPerLevel = 0.1f;
    public float baseHealingReturn = 0.15f;
    public float path2HealingReturnPerLevel = 0.03f;

    [Header("Rejuvenating Burgeon (baseline passive)")]
    public float burgeonHealPerSecond = 12f;
    public float path2BurgeonHealPerLevel = 2f;
    public float baseBurgeonDuration  = 4f;
    public float burgeonTickInterval  = 0.5f;

    [Header("Path 3 Scaling")]
    public float revivalBaseHeal                = 40f;
    public float revivalHealPerLevel            = 20f;
    public float skillHealMPScaling             = 0.30f;
    public float path3CooldownReductionPerLevel = 0f;
    public float verdantGuardianShield          = 200f;
    public float verdantGuardianRegen           = 20f;
    public float verdantGuardianDuration        = 8f;

    public override string GetAttackDescription() =>
        "Fires a rejuvenating seed at the most injured nearby plant, healing it and leaving behind a regenerating bloom.";

    public override string GetPassiveDescription() =>
        "Heals & Shields given are increased by a portion of <color=green><b>Grass Damage</b></color>, and part of the healing given to others is returned to the Rhodiola.";

    public override string GetSkillDescription() =>
        "Target a tile where a plant has fallen to resurrect it and restore a portion of its Health.";
}
