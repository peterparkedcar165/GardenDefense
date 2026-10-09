using UnityEngine;

[CreateAssetMenu(fileName = "DandelionData", menuName = "Scriptable Objects/PlantData/Dandelion")]
public class DandelionData : PlantData
{
    public float baseBeamWidth;
    public float baseGustSpeed = 2.5f;

    [Header("Path 1 Scaling")]
    public float path1AttackSpeedPerLevel = 0.05f;
    public float path1AttackRangePerLevel = 0.2f;
    public float path1MagicPowerPerLevel = 5f;

    [Header("Path 2 Scaling - Pollinator's Pulse")]
    public float basePassiveInterval = 16f;
    public float path2IntervalReductionPerLevel = 1f;
    public float basePassiveProcChance = 0.5f;
    public float path2ProcChancePerLevel = 0.05f;
    public float basePassiveTickReduction = 1f;
    public float path2TickReductionPerLevel = 0.25f;

    public float baseWindGustRange = 10f;

    [Header("Path 3 Scaling - Wind Gust / Pollen Haste")]
    public float baseGustDamage = 42f;
    public float path3GustDamagePerLevel = 12f;
    public float path3BeamWidthPerLevel = 0.25f;
    public float path3WindGustRangePerLevel = 0.5f;
    public float basePollenHasteBonus = 0.10f;
    public float path3HasteBonusPerLevel = 0.04f;
    // drives the generic Plant.baseSkillDuration/skillDuration stat (see OnPath3Upgrade) -
    // Pollen Haste's duration reads straight off skillDuration rather than its own field
    public float path3SkillDurationPerLevel = 2f;

    public override string GetAttackDescription() =>
        $"Fires a pollen seed at a target, dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        "Periodically reduces the Skill Cooldown of allied plants within her attack radius, and her attacks have a chance to speed this up.";

    public override string GetSkillDescription() =>
        $"Fires a slow, large pollen seed towards the targeted direction. On impact, deals {DamageTypeLabel(damageType)} to insects and sweeps them along with the wind, while granting allied plants touched by it <color=#B2EBF2><b>Pollen Haste</b></color>.";
}
