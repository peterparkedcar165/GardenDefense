using UnityEngine;

[CreateAssetMenu(fileName = "AcornKnightData", menuName = "Scriptable Objects/PlantData/AcornKnight")]
public class AcornKnightData : PlantData
{
    [Header("Semicircle Swing And Bash")]
    // both the sword swing and the shield bash hit any number of insects they geometrically
    // reach - no cap on either - and share this same 10%-per-extra-target falloff, each with its
    // own floor (see AcornKnight.SwingMinDamageMultiplier/BashMinDamageMultiplier)
    public float coneFalloffPerTarget = 0.10f;
    // one attack is two sword strikes that split his Attack Damage between them: the down strike
    // deals this share of it and the up strike the other, so a full set deals 1x Attack Damage
    public float downStrikeDamagePercent = 0.6f;
    public float upStrikeDamagePercent = 0.4f;

    [Header("Guard Stance And Bash")]
    public float baseDefensiveArmor = 20f;
    public float path2DefensiveArmorPerLevel = 6f;
    public float defensiveAttackSpeedPenalty = 0.75f;
    // each Path2 (passive) level takes this much off the Attack Speed penalty (not the range one)
    public float path2DefensiveAttackSpeedPenaltyReductionPerLevel = 0.03f;
    public float defensiveAttackRangePenalty = 0.25f;
    // grace period after no insect is targeting the Knight in melee, before the guard drops
    public float stanceExitDelay = 1f;
    // how long Shield Bash stuns each insect it hits - Path2 max only (see Level5Section), flat,
    // no per-level scaling, independent of the skill's shield-throw stun
    public float bashStunDuration = 0.15f;
    // Shield Bash's damage is this percent of Armor (for the closest insect hit), separate from
    // the per extra insect falloff (coneFalloffPerTarget) that still applies on top for every insect
    // beyond that one. 50% plus 5% per Path 2 level (Tommy, 2026-11-22; was 10% a level)
    public float baseBashDamagePercent = 0.5f;
    public float path2BashDamagePercentPerLevel = 0.05f;
    // skill tree node 5a (Vengeful Guard): Attack Cooldown refunded per Physical hit taken while
    // in Guard Stance
    public float counterStanceCooldownReduction = 0.2f;
    // skill tree node 5b (Evasive Guard): flat Evasion while in Guard Stance
    public float defensiveEvasionBonus = 0.15f;

    [Header("Passive Regen - percent of max health, ticks once per second, no level scaling")]
    public float baseRegenPercentPerSecond = 0.005f;
    public float boostedRegenPercentPerSecond = 0.02f;
    // seconds without taking damage before regen jumps from base to boosted
    public float regenBoostDelay = 6f;

    [Header("Skill: Shield Throw")]
    // flat line skill shot range, independent of melee attackRange
    public float skillThrowRange = 7.5f;
    public float shieldThrowSpeed = 10f;
    public float shieldStunDuration = 1.5f;
    public float baseSkillAttackSpeedBonus = 0.2f;
    public float shieldReequipDelay = 1f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;
    public float path1AttackSpeedPerLevel = 0.05f;
    public int path1ArmorPerLevel = 4;

    [Header("Path 3 Scaling")]
    public float path3SkillDurationPerLevel = 2f;
    public float path3HealthPerLevel = 50f;
    public float path3AttackSpeedBonusPerLevel = 0.05f;
    // seconds of shield lifetime granted per 1 point of Magic Power
    public float skillDurationMPMultiplier = 0.10f;
}
