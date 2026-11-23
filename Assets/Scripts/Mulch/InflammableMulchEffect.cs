// read by every Burn that lands from Owner (MulchManager.BurnTickSpeedFrom), which ticks that much
// faster for as long as it lasts. counts the damage its extra ticks added
public class InflammableMulchEffect : MulchEffect
{
    private readonly InflammableMulchData typedData;
    private bool on;
    // what its Burns' extra ticks dealt this level
    private float extra;

    public InflammableMulchEffect(InflammableMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public float TickSpeed => on ? V(typedData.burnTickSpeed) : 0f;

    public override void Activate() => on = Owner != null;

    public override void Deactivate() => on = false;

    public void CountExtra(float damage) => extra += damage;

    public override string GetStatus() => $"Extra Burn damage: {extra:0}";
}
