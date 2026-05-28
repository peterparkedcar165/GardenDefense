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
        $"Each seed deals <color=green><b>{baseAttackDamage}</b></color> <color=#B2EBF2>Wind</color> <color=#A0522D>Physical</color> damage.";

    public override string GetPassiveDescription() =>
        $"Fires <color=green><b>{baseSeedCount}</b></color> seeds per attack, targeting the <color=green><b>{baseSeedCount}</b></color> highest-priority insects in range.";

    public override string GetSkillDescription() =>
        $"Blows a powerful gust of pollen wind <color=green><b>{baseBeamWidth}</b></color> units wide towards the targeted direction, crossing the entire map, lasting <color=green><b>{baseSkillDuration}</b></color> seconds. Insects caught in the gust take <color=green><b>{baseAttackDamage}</b></color> <color=#B2EBF2>Wind</color> <color=#FFB6C1>Magic</color> damage per second, are pushed in the wind's direction, and are <color=#E0E0E0>Displaced</color>.";
}
