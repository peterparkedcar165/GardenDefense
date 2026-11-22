using UnityEngine;

// MulchManager reads FertilizerManager.EpicMulchEveryWaves when a wave's guaranteed mulch drops,
// and drops an extra Epic one with it every that many waves
public class CleanGardenFertilizerEffect : FertilizerEffect
{
    private readonly CleanGardenFertilizerData typedData;

    public CleanGardenFertilizerEffect(CleanGardenFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => FertilizerManager.EpicMulchEveryWaves = Mathf.Max(0, typedData.everyWaves);
    public override void Deactivate() => FertilizerManager.EpicMulchEveryWaves = 0;

    public override string GetStatus()
    {
        int every = typedData.everyWaves;
        if (every <= 0 || GameManager.instance == null) return null;
        int wave = Mathf.Max(1, GameManager.instance.currentWave);
        int next = ((wave + every - 1) / every) * every;
        return $"Next extra Epic mulch: end of wave {next}";
    }
}
