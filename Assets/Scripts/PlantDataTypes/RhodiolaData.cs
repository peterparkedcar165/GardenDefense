using UnityEngine;

[CreateAssetMenu(fileName = "RhodiolaData", menuName = "Scriptable Objects/PlantData/Rhodiola")]
public class RhodiolaData : PlantData
{
    // her kit (Tommy, 2026-11-27): a drain attack on insects that heals her and fills her golden sap,
    // and a channel that spends the sap healing a plant. numbers marked proposal are Claude's, the rest
    // are Tommy's
    [Header("Attack: Drain")]
    [Tooltip("share of the damage her hit really dealt (after the insect's armor and resistances) that heals her when the orb reaches her staff, e.g. 0.25 = 25% (proposal)")]
    public float drainHealShare = 0.25f;
    [Tooltip("how fast the drained life flies back to her, units per second along the straight line (Tommy: half the first 5)")]
    public float drainMoteSpeed = 2.5f;
    [Tooltip("how high the orb's arc rises for every unit between the insect and her staff (proposal)")]
    public float drainArcPerUnit = 0.3f;
    [Tooltip("the lowest the orb's arc rises, units (proposal)")]
    public float drainArcMin = 0.4f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 2f;
    [Tooltip("added to her base attack range per level (Tommy: as her old path had it)")]
    public float path1AttackRangePerLevel = 0.3f;
    [Tooltip("share of Grass Damage per level, e.g. 0.05 = 5% (proposal)")]
    public float path1GrassDamagePerLevel = 0.05f;
    [Tooltip("share of her Grass Damage added to Heals & Shields she gives, e.g. 0.5 = 50%")]
    public float baseGrassConversion = 0.5f;

    [Header("Golden Sap (passive)")]
    [Tooltip("how much golden sap she can hold at passive level 0")]
    public float goldenSapCap = 200f;
    public float path2GoldenSapCapPerLevel = 40f;
    [Tooltip("healing she gets from her attack and from Rejuvenating Burgeon on herself becomes this much sap, e.g. 1 = 100%, even at full health")]
    public float sapConversion = 1f;
    [Tooltip("added to the conversion per passive level, e.g. 0.5 = +50%")]
    public float path2SapConversionPerLevel = 0.5f;
    [Tooltip("she only starts a channel with more than this share of her cap, e.g. 0.1 = 10%")]
    [Range(0f, 1f)] public float sapToChannel = 0.1f;
    [Tooltip("a plant under this share of its max health makes her stop attacking and channel into it, e.g. 0.5 = 50%")]
    [Range(0f, 1f)] public float lowHealthThreshold = 0.5f;
    [Tooltip("how strong the glow at her root gets when the sap is full")]
    [Range(0f, 1f)] public float sapGlowMaxAlpha = 0.6f;

    [Header("Channel")]
    [Tooltip("health per second the channel heals, spending as much sap (proposal)")]
    public float channelHealPerSecond = 30f;
    [Tooltip("share of Magic Power added to the channel's heal per second (proposal)")]
    public float channelHealMPScaling = 0.1f;
    [Tooltip("seconds between the channel's heals (proposal)")]
    public float channelTickInterval = 0.25f;
    [Tooltip("how fast the life energy she sends flies, units per second (proposal)")]
    public float channelMoteSpeed = 5f;

    [Header("Path 2 Max Level Bonus")]
    public float maxLevelGrassDamageBonus = 0.25f;
    public float maxLevelHealingReturn = 0.33f;

    [Header("Rejuvenating Burgeon")]
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
}
