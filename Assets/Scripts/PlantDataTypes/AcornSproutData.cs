using UnityEngine;

[CreateAssetMenu(fileName = "AcornSproutData", menuName = "Scriptable Objects/PlantData/AcornSprout")]
public class AcornSproutData : PlantData
{
    [Header("Semicircle Swing And Bash")]
    // both the sword swing and the shield bash hit up to 5 insects (fixed constant on the plant
    // script, not tunable here, since no node or stat is ever meant to raise it) and share this
    // same 10%-per-extra-target falloff
    public float coneFalloffPerTarget = 0.10f;
    // seconds from the attack starting to the hit actually landing - stands in for an Animation
    // Event on the swing clip's hit frame until real art exists. should stay comfortably under
    // 1/attackSpeed so a windup never overruns into the next attack
    public float attackWindupTime = 0.1f;

    [Header("Defensive Stance And Bash")]
    public float baseDefensiveArmor = 20f;
    public float path2DefensiveArmorPerLevel = 6f;
    public float defensiveAttackSpeedPenalty = 0.3f;
    public float defensiveAttackRangePenalty = 0.25f;
    // grace period after no insect is targeting the Knight in melee, before the guard drops
    public float stanceExitDelay = 2.5f;

    [Header("Skill: Shield Throw")]
    public float throwRangeMultiplier = 1.5f;
    public float shieldThrowSpeed = 10f;
    public float shieldStunDuration = 1.5f;
    public float skillAttackSpeedBonus = 0.4f;
    public float shieldReequipDelay = 1f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;
    public float path1AttackSpeedPerLevel = 0.05f;
    public int path1ArmorPerLevel = 4;

    [Header("Path 3 Scaling")]
    // impact damage is a flat 50% of Attack Damage (see baseSkillDamageMultiplier on the SO) and no
    // longer scales with level - instead each level adds this much flat bonus damage on top
    public float path3FlatDamagePerLevel = 30f;
    public float path3SkillDurationPerLevel = 2f;
    public float path3HealthPerLevel = 50f;
    public float path3RadiusPerLevel = 0.15f;
    // seconds of shield lifetime granted per 1 point of Magic Power
    public float skillDurationMPMultiplier = 0.10f;

    public override string GetAttackDescription() =>
        $"Swings its sword in a cone, dealing {DamageTypeLabel(damageType)} to nearby insects.";

    public override string GetPassiveDescription() =>
        "Entering combat with an attacker raises its guard, trading attack speed for armor and shield bashes back.";

    public override string GetSkillDescription() =>
        "Arms a shield throw. The next attack hurls the shield at the target, stunning it, then the shield falls to the ground, blocking the path and taunting insects.";
}
