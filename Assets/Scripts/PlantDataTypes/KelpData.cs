using UnityEngine;

[CreateAssetMenu(fileName = "KelpData", menuName = "Scriptable Objects/PlantData/Kelp")]
public class KelpData : PlantData
{
    [Header("Attack")]
    public float attackSplashRadius = 1f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackRangePerLevel  = 0.2f;

    [Header("Passive: Oxygen")]
    public float baseOxygenReplenish       = 5f;
    public float path2OxygenReplenishPerLevel = 1f;
    public float baseOxygenRequirement     = 100f;
    public float path2OxygenRequirementReductionPerLevel = 10f;
    public float path2AttackSpeedPerLevel  = 0.05f;

    [Header("Skill: Air Bubble")]
    public float bubbleTravelSpeed  = 6f;
    public float bubbleMaxRange     = 15f;
    public float baseBubbleWidth               = 0.8f;
    public float path3BubbleWidthPerLevel       = 0.15f;
    public float baseBubbleInitialOxygen          = 20f;
    public float path3BubbleInitialOxygenPerLevel = 5f;
    public float baseBubbleRegenPerSecond         = 2f;
    public float path3BubbleRegenPerLevel         = 0.5f;
    public float path3DurationPerLevel            = 2f;

    public override string GetAttackDescription() =>
        $"Whips the target insect, dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        "Kelp's own Oxygen never depletes. Releases air bubbles when it hits a target, granting nearby plants Oxygen and producing Sun.";

    public override string GetSkillDescription() =>
        "Fires a bubble in a chosen direction. Plants it touches are enveloped in an Air Bubble, restoring Oxygen instantly and over time.";
}
