using UnityEngine;

[CreateAssetMenu(fileName = "MorningGloryData", menuName = "Scriptable Objects/PlantData/MorningGlory")]
public class MorningGloryData : PlantData
{
    [Header("Attack")]
    public float projectileSpeedDamageScale = 2.5f;   // damage = attackDamage + this * projectileSpeed

    [Header("Path 1 Attack")]
    public float path1AttackDamagePerLevel = 4f;
    public float path1AttackRangePerLevel  = 0.2f;

    [Header("Passive Aura (multipliers)")]
    public float baseAttackSpeedBonus     = 0.15f;
    public float baseProjectileSpeedBonus = 0.15f;
    public float path2AttackSpeedBonusPerLevel     = 0.03f;
    public float path2ProjectileSpeedBonusPerLevel = 0.03f;
    public float attackSpeedBonusMPMultiplier     = 0.16f;   // +16% attack speed bonus per 100 Magic Power
    public float projectileSpeedBonusMPMultiplier = 0.12f;   // +12% projectile speed bonus per 100 Magic Power

    [Header("Skill Updraft Field")]
    public float baseFieldDuration          = 4f;
    public float path3FieldDurationPerLevel = 0.5f;
    public float fieldRadius                = 2f;
    public float path3RadiusPerLevel        = 0.15f;
    public float liftForce                  = 2.5f;   // per-kick rise ~0.32; tuned with maxHeight for a ~1.8-2.2 hover
    public float liftMaxHeight              = 1.85f;  // updraft stops pushing above this height (Levitating still applies)
    public float baseLevitateCritBonus      = 0.25f;
    public float path3CritBonusPerLevel     = 0.05f;
    public float critBonusMPMultiplier      = 0.24f;   // +24% Levitating crit chance per 100 Magic Power

    public override string GetAttackDescription() =>
        $"Sends a wind blade at the first target, dealing damage that scales with Projectile Speed.";

    public override string GetPassiveDescription() =>
        $"Plants within range (including herself) gain increased Attack Speed and Projectile Speed.";

    public override string GetSkillDescription() =>
        $"Summons an updraft field that suspends insects helplessly in the air and makes them take extra critical hits.";
}
