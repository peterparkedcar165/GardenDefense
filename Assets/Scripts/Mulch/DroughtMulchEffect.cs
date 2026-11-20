// dormancy's two phases on its plant's own sun production (Plant.mulchSunProductionBonus, read by
// Plant.GenerateSun). only does anything on a plant that makes sun (sun sap counts)
public class DroughtMulchEffect : MulchEffect
{
    private readonly DroughtMulchData typedData;
    private readonly WaveCounter waves = new WaveCounter();
    private float applied;
    private bool over;

    public DroughtMulchEffect(DroughtMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
        over = data.droughtWaves <= 0;
    }

    public override void Activate() => SetMultiplier(over ? V(typedData.afterSunMultiplier) : typedData.droughtSunMultiplier);
    public override void Deactivate() => SetBonus(0f);

    public override string GetStatus()
    {
        if (over) return $"Drought over: {V(typedData.afterSunMultiplier):0.##}x Sun";
        int left = waves.Left(typedData.droughtWaves);
        return $"Drought: {left} more {(left == 1 ? "wave" : "waves")}";
    }

    public override void OnWaveStarted(int wave) => waves.WaveStarted(wave);

    public override void OnWaveEnded(int wave)
    {
        if (over || !waves.WaveEnded(wave) || waves.Completed < typedData.droughtWaves) return;
        over = true;
        SetMultiplier(V(typedData.afterSunMultiplier));
    }

    // stored as what it adds on top of 1, so it adds up with anything else on the same plant
    private void SetMultiplier(float multiplier) => SetBonus(multiplier - 1f);

    private void SetBonus(float value)
    {
        if (Owner != null) Owner.mulchSunProductionBonus += value - applied;
        applied = Owner != null ? value : 0f;
    }
}
