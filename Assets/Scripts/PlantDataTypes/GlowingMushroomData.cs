using UnityEngine;

[CreateAssetMenu(fileName = "GlowingMushroomData", menuName = "Scriptable Objects/PlantData/GlowingMushroom")]
public class GlowingMushroomData : PlantData
{
    [Header("Attack Splash")]
    public float splashRadius = 1.5f;
    public float splashDamageMultiplier = 0.5f;

    [Header("Passive Fungal Glow")]
    public float fungalGlowDuration = 6f;
    public float fungalGlowSpreadRadius = 2f;
    public float passiveCooldown = 4f;

    [Header("Skill Flash")]
    public float lightRadiusMultiplier = 3f;
    public float blindDuration = 3f;
    public float blindAccuracyPenalty = 1f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;
    public float path1AttackRangePerLevel  = 0.15f;

    [Header("Path 2 Scaling")]
    public float path2FungalGlowDurationPerLevel = 1f;
    public float path2SpreadRadiusPerLevel = 0.25f;

    [Header("Path 3 Scaling")]
    public float path3BlindDurationPerLevel = 0.5f;
    public float path3SkillDurationPerLevel = 1f;

    public override string GetAttackDescription() =>
        $"Fires a fungal bolt dealing <color=green><b>{baseAttackDamage:F0}</b></color> <color=green>Nature</color> <color=#FFB6C1>Magic</color> damage, splashing to nearby insects.";

    public override string GetPassiveDescription() =>
        $"Attacks inflict <color=#88FF88>Fungal Glow</color> on the target. Water damage spreads the glow to nearby insects.";

    public override string GetSkillDescription() =>
        $"Releases a blinding flash, tripling the illumination radius and <color=#DDDDDD>Blinding</color> all insects caught in the expanded area.";
}
