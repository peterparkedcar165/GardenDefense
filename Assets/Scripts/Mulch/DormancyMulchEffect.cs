// a damage penalty on its plant from the moment it's equipped, flipping to a bonus once its waves
// have passed (Plant.mulchDamageBonus). the wave count lives here, so a merge or a revive with the
// mulch carries on where it was
public class DormancyMulchEffect : MulchEffect
{
    private readonly DormancyMulchData typedData;
    private readonly WaveCounter waves = new WaveCounter();
    private float applied;
    private bool awake;

    public DormancyMulchEffect(DormancyMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
        awake = data.dormantWaves <= 0;
    }

    public override void Activate() => SetContribution(awake ? V(typedData.damageBonus) : -V(typedData.damagePenalty));
    public override void Deactivate() => SetContribution(0f);

    public override string GetStatus()
    {
        if (awake) return $"Bonus active: +{V(typedData.damageBonus) * 100f:0.#}% damage";
        int left = waves.Left(typedData.dormantWaves);
        return $"Penalty: {left} more {(left == 1 ? "wave" : "waves")}";
    }

    public override void OnWaveStarted(int wave) => waves.WaveStarted(wave);

    public override void OnWaveEnded(int wave)
    {
        if (awake || !waves.WaveEnded(wave) || waves.Completed < typedData.dormantWaves) return;
        awake = true;
        SetContribution(V(typedData.damageBonus));
    }

    private void SetContribution(float value)
    {
        if (Owner != null) Owner.mulchDamageBonus += value - applied;
        applied = Owner != null ? value : 0f;
    }
}
