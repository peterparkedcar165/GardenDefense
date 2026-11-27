using UnityEngine;

[CreateAssetMenu(fileName = "RhodiolaData", menuName = "Scriptable Objects/PlantData/Rhodiola")]
public class RhodiolaData : PlantData
{
    // her kit (Tommy, 2026-11-27): a drain attack on insects that heals her and fills her golden sap,
    // and a channel that spends the sap healing a plant. numbers marked proposal are Claude's, the rest
    // are Tommy's
    [Header("Attack: Drain")]
    [Tooltip("share of the damage her hit really dealt (after the insect's armor and resistances) that heals her when the orb reaches her staff, e.g. 0.5 = 50% (Tommy)")]
    public float drainHealShare = 0.5f;
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
    [Tooltip("unused since 2026-11-27 (Tommy's tooltips): the Attack path gave this share of Grass Damage per level")]
    public float path1GrassDamagePerLevel = 0.05f;
    [Tooltip("added per Attack path level to how much her channel heals for each point of sap it spends (the healing to sap depletion ratio, 1 at level 0), e.g. 0.1 = +10% (Tommy)")]
    public float path1SapEfficiencyPerLevel = 0.1f;
    [Tooltip("Attack path max level bonus: share of her Grass Damage added to Heals & Shields she gives, 1 = all of it (Tommy, 2026-11-27: her healing also benefits from Grass Damage; it was 50%, always on before)")]
    public float baseGrassConversion = 1f;

    [Header("Golden Sap (passive)")]
    [Tooltip("how much golden sap she can hold at passive level 0")]
    public float goldenSapCap = 200f;
    [Tooltip("added to her sap cap per passive level (Tommy, 2026-11-27: 20, 200 to 300 at level 5)")]
    public float path2GoldenSapCapPerLevel = 20f;
    [Tooltip("healing she gets from her attack becomes this much sap, e.g. 1 = 100%, even at full health (her Rejuvenating Burgeon no longer fills it, Tommy 2026-11-27)")]
    public float sapConversion = 1f;
    [Tooltip("added to the conversion per passive level, e.g. 0.1 = +10% (Tommy)")]
    public float path2SapConversionPerLevel = 0.1f;
    [Tooltip("she only starts a channel with at least this share of her cap, e.g. 0.1 = 10%")]
    [Range(0f, 1f)] public float sapToChannel = 0.1f;
    [Tooltip("a plant under this share of its max health makes her stop attacking and channel into it, e.g. 0.5 = 50%")]
    [Range(0f, 1f)] public float lowHealthThreshold = 0.5f;
    [Tooltip("how strong the glow at her root gets when the sap is full")]
    [Range(0f, 1f)] public float sapGlowMaxAlpha = 0.6f;

    [Header("Channel")]
    [Tooltip("health per second the channel heals, spending as much sap at Attack path level 0 (Tommy)")]
    public float channelHealPerSecond = 40f;
    [Tooltip("share of Magic Power added to the channel's heal per second, e.g. 0.05 = 5% (Tommy)")]
    public float channelHealMPScaling = 0.05f;
    [Tooltip("seconds between the channel's heals (proposal)")]
    public float channelTickInterval = 0.25f;
    [Tooltip("unused since her heal ray (2026-11-27): how fast the life energy her channel sent flew, units per second")]
    public float channelMoteSpeed = 5f;

    [Header("Path 2 Max Level Bonus")]
    [Tooltip("unused since 2026-11-27 (Tommy: the max level bonus is now her Rejuvenating Burgeon filling her Golden Sap)")]
    public float maxLevelGrassDamageBonus = 0.25f;
    [Tooltip("unused since 2026-11-27 (see above)")]
    public float maxLevelHealingReturn = 0.33f;

    [Header("Rejuvenating Burgeon")]
    public float burgeonHealPerSecond = 12f;
    public float path2BurgeonHealPerLevel = 2f;
    public float baseBurgeonDuration  = 4f;
    public float burgeonTickInterval  = 0.5f;
    [Tooltip("armor a plant holds while Rejuvenating Burgeon is on it, before her Magic Power multiplies it (100 Magic Power doubles it; Tommy 2026-11-27, it was her Grass Damage)")]
    public float burgeonBaseArmor = 16f;
    public float path2BurgeonArmorPerLevel = 8f;

    [Header("Path 3 Scaling")]
    [Tooltip("her skill's heal on the revived plant (Tommy, 2026-11-27: 100); past its max health it becomes Verdant Guard")]
    public float revivalBaseHeal                = 100f;
    public float revivalHealPerLevel            = 20f;
    [Tooltip("share of Magic Power added to the revival heal, e.g. 0.5 = 50% (Tommy)")]
    public float skillHealMPScaling             = 0.5f;
    [Tooltip("seconds off her skill cooldown per skill level, never under 10 s (Tommy, 2026-11-27: 10, from a base of 120)")]
    public float path3CooldownReductionPerLevel = 10f;
    [Tooltip("how long Verdant Guard (the revival heal past max health, as a shield) lasts, seconds (Tommy)")]
    public float verdantGuardDuration           = 20f;
    [Tooltip("unused since 2026-11-27 (the old skill max level bonus, Verdant Guardian)")]
    public float verdantGuardianShield          = 200f;
    [Tooltip("unused since 2026-11-27 (see above)")]
    public float verdantGuardianRegen           = 20f;
}
