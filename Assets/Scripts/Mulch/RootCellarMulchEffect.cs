using UnityEngine;

// interest on whatever sun is banked the moment a wave ends, rounded down
public class RootCellarMulchEffect : MulchEffect
{
    private readonly RootCellarMulchData typedData;

    public RootCellarMulchEffect(RootCellarMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() {}
    public override void Deactivate() {}

    public override void OnWaveEnded(int wave)
    {
        if (GameManager.instance == null) return;
        int interest = Mathf.FloorToInt(GameManager.instance.SunCount * typedData.interestPercent);
        if (interest > 0) GameManager.instance.AddSun(interest);
    }
}
