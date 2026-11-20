// pays its sun once, on pick
public class SunGrantFertilizerEffect : FertilizerEffect
{
    private readonly SunGrantFertilizerData typedData;

    public SunGrantFertilizerEffect(SunGrantFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (typedData.sunAmount > 0)
            GameManager.instance?.AddSun(typedData.sunAmount);
    }

    public override void Deactivate() {}
}
