using UnityEngine;

[CreateAssetMenu(fileName = "SunflowerData", menuName = "Scriptable Objects/PlantData/Sunflower")]
public class SunflowerData : PlantData
{
    public int baseSunGenerated;
    public float baseSunrayDPS;
    public float baseCooldownReductionPerHit = 0.35f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackSpeedPerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    // Sun generation interval is fixed (see basePassiveCooldown on the base SO) - Path 2 only
    // grows the amount generated per production, not the cadence
    public int path2SunPerLevel = 3;
    public float path2CooldownReductionPerLevel = 0.03f;

    [Header("Path 3 Scaling")]
    public float path3SunrayDPSPerLevel = 15f;
    public float path3SkillDurationPerLevel = 0.5f;
    // the Sunray's size is its damage area's diameter (Tommy, 2026-11-26: diameter, not radius), in tiles;
    // the beam drawn is always 1 tile narrower (a diameter of 2 draws a 1 tile beam, 3 a 2 tile beam). the
    // base PlantData.baseSkillRadius isn't used for Sunflower
    [Tooltip("the Sunray's damage area across, in tiles, at Skill level 0; its beam is 1 tile narrower")]
    public float baseSunrayDiameter = 2f;
    [Tooltip("tiles the damage area's diameter grows a Skill level (the beam grows as much)")]
    [UnityEngine.Serialization.FormerlySerializedAs("path3RadiusPerLevel")]
    public float path3DiameterPerLevel = 0.1f;
    // unused since 2026-11-26: the beam's width follows the damage area's diameter (always 1 tile less)
    [Tooltip("UNUSED: the beam is always 1 tile narrower than the damage area's diameter, so it grows by Diameter Per Level")]
    [UnityEngine.Serialization.FormerlySerializedAs("path3VisualRadiusPerLevel")]
    public float path3VisualWidthPerLevel = 0.3f;

    [Header("Sunray Audio")]
    [Tooltip("played this many seconds before the sunray actually appears (not the skill activation sound)")]
    public SoundEffect sunraySpawnSound;
    [Tooltip("how long before the sunray appears its spawn sound plays")]
    public float sunraySpawnLeadTime = 0.1f;
    [Tooltip("loops for as long as the sunray is active, until sunrayEndLeadTime before it expires")]
    public SoundEffect sunrayLoopSound;
    [Tooltip("played once, sunrayEndLeadTime before the sunray expires")]
    public SoundEffect sunrayEndSound;
    [Tooltip("how long before the sunray expires the loop sound stops and the end sound plays")]
    public float sunrayEndLeadTime = 0.2f;
}
