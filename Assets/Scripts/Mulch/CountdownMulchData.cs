using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CountdownMulchData", menuName = "Scriptable Objects/Mulch/Countdown")]
public class CountdownMulchData : MulchData
{
    [Tooltip("whole waves to wait before every plant gets its free levels. also the minimum " +
        "number of waves that must be left in the level for this mulch to be offered")]
    public int wavesUntilPayout = 10;

    public override MulchEffect CreateEffect() => new CountdownMulchEffect(this);

    // offered only if the payout can still land inside this level
    public override bool MeetsExtraRequirements(List<PlantData> loadout, int currentWave)
    {
        LevelConfig config = ProceduralLevel.CurrentConfig;
        return config != null && config.maxWaves - currentWave >= wavesUntilPayout;
    }

    public override string GetDescription() =>
        $"In <b><color=green>{wavesUntilPayout}</color></b> waves, every plant on the field gains <b><color=green>+1</color></b> level " +
        $"on all 3 upgrade paths for free, up to the level's max upgrade level.";
}
