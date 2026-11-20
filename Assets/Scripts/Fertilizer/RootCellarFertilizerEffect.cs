using UnityEngine;

// interest on whatever sun is banked the moment a wave ends, rounded down
public class RootCellarFertilizerEffect : FertilizerEffect
{
    private readonly RootCellarFertilizerData typedData;

    public RootCellarFertilizerEffect(RootCellarFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() {}
    public override void Deactivate() {}

    private int Interest => GameManager.instance != null ? Mathf.FloorToInt(GameManager.instance.SunCount * typedData.interestPercent) : 0;

    public override string GetStatus() => GameManager.instance != null ? $"Next wave end: +{Interest} Sun" : null;

    public override void OnWaveEnded(int wave)
    {
        int interest = Interest;
        if (interest > 0) GameManager.instance.AddSun(interest);
    }
}
