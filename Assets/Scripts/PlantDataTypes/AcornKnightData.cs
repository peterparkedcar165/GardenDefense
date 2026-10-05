using UnityEngine;

[CreateAssetMenu(fileName = "AcornKnightData", menuName = "Scriptable Objects/PlantData/AcornKnight")]
public class AcornKnightData : PlantData
{
    [Header("Semicircle Swing And Bash")]
    // both the sword swing and the shield bash hit any number of insects they geometrically
    // reach - no cap on either - and share this same 10%-per-extra-target falloff, each with its
    // own floor (see AcornKnight.SwingMinDamageMultiplier/BashMinDamageMultiplier)
    public float coneFalloffPerTarget = 0.10f;
    // seconds from the attack starting to the hit actually landing - stands in for an Animation
    // Event on the swing clip's hit frame until real art exists. should stay comfortably under
    // 1/attackSpeed so a windup never overruns into the next attack
    public float attackWindupTime = 0.1f;

    [Header("Defensive Stance And Bash")]
    public float baseDefensiveArmor = 20f;
    public float path2DefensiveArmorPerLevel = 6f;
    public float defensiveAttackSpeedPenalty = 0.75f;
    public float defensiveAttackRangePenalty = 0.25f;
    // grace period after no insect is targeting the Knight in melee, before the guard drops
    public float stanceExitDelay = 1f;
    // Shield Bash's own chance to stun each insect it hits - Path2 max only (see Level5Section),
    // flat, no per-level scaling, independent of the skill's shield-throw stun
    public float maxLevelBashStunChance = 0.75f;
    public float bashStunDuration = 1f;
    // Shield Bash's damage is this percent of Armor (for the closest insect hit), separate from
    // the 10%-per-extra-insect falloff that still applies on top for every insect beyond that one
    public float baseBashDamagePercent = 0.5f;
    public float path2BashDamagePercentPerLevel = 0.10f;

    [Header("Passive Regen - flat, no level scaling")]
    public float baseRegenPerSecond = 2f;
    public float boostedRegenPerSecond = 6f;
    // seconds without taking damage before regen jumps from base to boosted
    public float regenBoostDelay = 6f;

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
        "Regenerates health over time, faster once it's gone a while without being hit. Being targeted by a physical attacker also puts it into Guard Stance, trading attack speed and range for armor and an omnidirectional shield bash.";

    public override string GetSkillDescription() =>
        "Arms a shield throw. The next attack hurls the shield at the target, stunning it, then the shield falls to the ground, blocking the path and taunting insects.";
}
