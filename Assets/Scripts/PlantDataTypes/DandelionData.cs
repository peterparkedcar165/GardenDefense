using UnityEngine;

[CreateAssetMenu(fileName = "DandelionData", menuName = "Scriptable Objects/PlantData/Dandelion")]
public class DandelionData : PlantData
{
    public int baseSeedCount;
    public float baseBeamWidth;
    public float basePushPower;

    [Header("Path 1 Scaling")]
    public float path1elementalAffinityPerLevel = 0.06f;
    public float path1AttackRangePerLevel = 0.25f;

    public float baseWindGustRange = 10f;

    [Header("Path 3 Scaling")]
    public float path3BeamWidthPerLevel = 0.25f;
    public float path3SkillDurationPerLevel = 1f;
    public float path3WindGustRangePerLevel = 0.5f;

    public override string GetAttackDescription() =>
        $"Fires multiple seeds per attack, each dealing {DamageTypeLabel(damageType)} to a separate target.";

    public override string GetPassiveDescription() =>
        "Can be upgraded to fire additional seeds, striking more enemies with each volley.";

    public override string GetSkillDescription() =>
        $"Blows a powerful gust of pollen wind towards the targeted direction, crossing the entire map. Insects caught in the gust take {DamageTypeLabel(damageType)} per second, are pushed in the wind's direction, and are <color=#E0E0E0>Displaced</color>.";
}
