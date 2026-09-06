using UnityEngine;

[CreateAssetMenu(fileName = "DandelionData", menuName = "Scriptable Objects/PlantData/Dandelion")]
public class DandelionData : PlantData
{
    public float baseBeamWidth;
    public float basePushPower;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 4f;
    public float path1AttackRangePerLevel = 0.25f;

    [Header("Path 2 Scaling - Blinding Pollen")]
    public float baseBlindingChance = 0.5f;
    public float path2BlindingChancePerLevel = 0.05f;
    public float baseBlindingDuration = 2f;
    public float path2BlindingDurationPerLevel = 1f;
    // path2 max: Blinding Pollen also inflicts a brief Stun on flying insects
    public float minorStunDuration = 1f;

    public float baseWindGustRange = 10f;

    [Header("Path 3 Scaling")]
    public float path3BeamWidthPerLevel = 0.25f;
    public float path3SkillDurationPerLevel = 1f;
    public float path3WindGustRangePerLevel = 0.5f;

    public override string GetAttackDescription() =>
        $"Blows a slow moving wind of pollen at a target, dealing {DamageTypeLabel(damageType)}. The wind pierces through everything in its path.";

    public override string GetPassiveDescription() =>
        $"Attacks have a chance to apply <color=#B2EBF2><b>Blinding Pollen</b></color>, reducing the target's Accuracy.";

    public override string GetSkillDescription() =>
        $"Blows a powerful gust of pollen wind towards the targeted direction, crossing the entire map. Insects caught in the gust take {DamageTypeLabel(damageType)} per second, are pushed in the wind's direction, are <color=#E0E0E0>Displaced</color>, and afflicted with <color=#B2EBF2><b>Blinding Pollen</b></color>.";
}
