// pays its sun once, on pick. nothing to undo afterwards, so it can't be sold or swapped either:
// that would hand back a mulch whose sun was already spent
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
}
