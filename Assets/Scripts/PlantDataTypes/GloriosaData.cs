using UnityEngine;

[CreateAssetMenu(fileName = "GloriosaData", menuName = "Scriptable Objects/PlantData/Gloriosa")]
public class GloriosaData : PlantData
{
    [Header("Passive - Ember Impact")]
    public float healAmount        = 25f;
    public float healMP            = 0.3f;
    public float temperatureAmount = 2f;
    public float temperatureMP     = 0.02f;
    public float auraRadius        = 1.5f;

    [Header("Path 1 - Attack")]
    public float path1AttackSpeedPerLevel = 0.1f;
    public float path1AttackRangePerLevel = 0.15f;

    [Header("Path 2 - Passive")]
    public float path2HealPerLevel        = 5f;
    public float path2TemperaturePerLevel = 0.3f;

    [Header("Skill - Wisps")]
    public int   wispCount                    = 2;
    public float wispDuration                 = 20f;
    public float wispSpeed                    = 3f;
    public float wispEmergeSpeed              = 3f;
    public float wispSeekDelay               = 1f;
    public float wispRadius                   = 1.5f;
    public float wispHealPerSecond            = 4f;
    public float wispTemperaturePerSecond     = 1f;
    public float latchHealPerSecond           = 8f;
    public float latchFireDamageBonus         = 0.2f;
    public float latchDuration                = 2f;
    public float wispLightRadius              = 1.5f;
    public float wispLightIntensity           = 0.6f;
    public float wispTickInterval             = 1f;

    [Header("Skill - Magic Power Scaling")]
    public float wispHealMP             = 0.05f;
    public float latchHealMP            = 0.1f;
    public float latchFireDamageBonusMP = 0.05f; // decimal space, applied as multiplier * magicPower / 100

    [Header("Path 3 - Skill Upgrades (per level)")]
    public float path3DurationPerLevel             = 3f;
    public float path3HealPerSecondPerLevel        = 1f;
    public float path3TemperaturePerSecondPerLevel = 0.2f;
    public float path3LatchHealPerSecondPerLevel   = 2f;
    public float path3LatchFireDamageBonusPerLevel = 0.05f;
    public float path3LatchDurationPerLevel        = 0.5f;

    public override string GetAttackDescription() =>
        $"Sends embers of soothing fire towards its target, dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        "Targets injured plants before insects. Embers heal the plant and increase its temperature toward comfort. Plants nearby also receive a weaker version of the effect.";

    public override string GetSkillDescription() =>
        "Summons Fiery Wisps that fly across the map seeking the most injured plant. Wisps heal and warm plants near them. When a wisp reaches its target it latches, applying <color=orange>Fiery Wisp Latched</color>: healing the plant and increasing its <color=orange>Fire</color> damage. The effect lingers after the wisp detaches.";
}
