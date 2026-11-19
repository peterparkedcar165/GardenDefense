// pays its sun once, on pick. it can't be swapped (that would hand back a fresh draft for a mulch
// whose sun was already spent), but seed sell can still sell it, and the sun stays
public class SunGrantMulchEffect : MulchEffect
{
    private readonly SunGrantMulchData typedData;

    public SunGrantMulchEffect(SunGrantMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (typedData.sunAmount > 0)
            GameManager.instance?.AddSun(typedData.sunAmount);
    }

    public override void Deactivate() {}

    public override bool CanBeRemoved => false;
    public override bool CanBeSold => true;
}
