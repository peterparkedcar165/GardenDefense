// a normal tick of its plant's own sun production (Plant.ExtraSunTick: sunflower, bog iris, kelp),
// plus one tick of every sun sap on it, so yield, drought and solar flare all apply. timers untouched
public class SunsetMulchEffect : MulchEffect
{
    private readonly SunsetMulchData typedData;

    public SunsetMulchEffect(SunsetMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate() {}
    public override void Deactivate() {}

    public override void OnWaveEnded(int wave)
    {
        if (!OwnerAlive) return;
        int ticks = VI(typedData.extraTicks);
        for (int i = 0; i < ticks; i++)
        {
            Owner.ExtraSunTick();
            foreach (MulchEffect effect in Owner.mulches)
                if (effect is SunSapMulchEffect sap) sap.Tick();
        }
    }
}
