// starts as a damage penalty the moment it's picked, then flips to a bonus once its waves have
// passed. applied keeps what this mulch currently adds to MulchManager.PlantDamageMultiplier, so
// removing it takes back exactly that, in either phase
public class DormancyMulchEffect : MulchEffect
{
    private readonly DormancyMulchData typedData;
    private readonly MulchWaveCounter waves = new MulchWaveCounter();
    private float applied;
    private bool awake;

    public DormancyMulchEffect(DormancyMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        awake = typedData.dormantWaves <= 0;
        SetContribution(awake ? typedData.damageBonus : -typedData.damagePenalty);
    }

    public override void Deactivate() => SetContribution(0f);

    public override string GetStatus() => awake
        ? $"Bonus active: +{typedData.damageBonus * 100f:F0}% damage"
        : $"Penalty: {waves.Left(typedData.dormantWaves)} more {Waves(waves.Left(typedData.dormantWaves))}";

    private static string Waves(int n) => n == 1 ? "wave" : "waves";

    public override void OnWaveStarted(int wave) => waves.WaveStarted(wave);

    public override void OnWaveEnded(int wave)
    {
        if (awake || !waves.WaveEnded(wave)) return;
        if (waves.Completed < typedData.dormantWaves) return;
        awake = true;
        SetContribution(typedData.damageBonus);
    }

    private void SetContribution(float value)
    {
        MulchManager.PlantDamageMultiplier += value - applied;
        applied = value;
    }
}
