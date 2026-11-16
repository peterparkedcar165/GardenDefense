using UnityEngine;
using UnityEngine.Serialization;

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
    public float path1AttackRangePerLevel = 0.3f;
    public float maxMissingHealthPercent = 0.05f;
    // heals and shields given are increased by this share of her grass damage. moved from the
    // passive to the attack path (tommy, 2026-11-16); the per level value keeps its old saved number
    [Tooltip("share of her Grass Damage added to Heals & Shields she gives, e.g. 0.5 = 50%")]
    public float baseGrassConversion = 0.5f;
    [FormerlySerializedAs("path2GrassConversionPerLevel")]
    public float path1GrassConversionPerLevel = 0.1f;

    [Header("Golden Sap (passive)")]
    [Tooltip("healing past a plant's full health she stores, up to this much at passive level 0")]
    public float baseSapCap = 50f;
    public float path2SapCapPerLevel = 15f;
    [Tooltip("share of the healing past full health that becomes sap, e.g. 0.5 = 50%")]
    [Range(0f, 1f)] public float sapStoreShare = 0.5f;
    [Tooltip("a plant in her range below this share of its max health gets all the stored sap at once, e.g. 0.25 = 25%")]
    public float sapReleaseThreshold = 0.25f;
    [Tooltip("how strong the glow at her root gets when the sap is full")]
    [Range(0f, 1f)] public float sapGlowMaxAlpha = 0.6f;

    [Header("Path 2 Max Level Bonus")]
    public float maxLevelGrassDamageBonus = 0.25f;
    public float maxLevelHealingReturn = 0.33f;

    [Header("Rejuvenating Burgeon (baseline passive)")]
    public float burgeonHealPerSecond = 12f;
    public float path2BurgeonHealPerLevel = 2f;
    public float baseBurgeonDuration  = 4f;
    public float burgeonTickInterval  = 0.5f;
    [Tooltip("armor a plant holds while Rejuvenating Burgeon is on it, before her Grass Damage multiplies it (+100% grass damage doubles it)")]
    public float burgeonBaseArmor = 16f;
    public float path2BurgeonArmorPerLevel = 8f;

    [Header("Path 3 Scaling")]
    public float revivalBaseHeal                = 40f;
    public float revivalHealPerLevel            = 20f;
    public float skillHealMPScaling             = 0.30f;
    public float path3CooldownReductionPerLevel = 0f;
    public float verdantGuardianShield          = 200f;
    public float verdantGuardianRegen           = 20f;

    public override string GetAttackDescription() =>
        "Fires a rejuvenating seed at the most injured nearby plant, healing it and leaving behind a regenerating bloom. Heals & Shields given are increased by a portion of <color=green><b>Grass Damage</b></color>.";

    public override string GetPassiveDescription() =>
        "Healing applies Rejuvenating Burgeon, a heal over time that also grants Armor while it lasts. Half of the healing past full health is stored as <color=#FFD700><b>Golden Sap</b></color>, released all at once into a plant in range that drops below 25% health.";

    public override string GetSkillDescription() =>
        "Target a tile where a plant has fallen to resurrect it and restore a portion of its Health.";
}
