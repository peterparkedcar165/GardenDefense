// read by MulchManager.PayReaction whenever a reaction Owner set off lands or takes effect, which
// makes Owner that much Sun (Plant.GenerateSun). counts the events and the Sun for its tooltip
public class BotanicalAlchemyMulchEffect : MulchEffect
{
    private readonly BotanicalAlchemyMulchData typedData;
    private bool on;
    private int events;
    private float sun;

    public BotanicalAlchemyMulchEffect(BotanicalAlchemyMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public int SunPerEvent => on ? VI(typedData.sunPerEvent) : 0;

    public override void Activate() => on = Owner != null;

    public override void Deactivate() => on = false;

    // its share of the Sun one event made
    public void CountEvent(float sunMade)
    {
        events++;
        sun += sunMade;
    }

    public override string GetStatus() => $"Reaction events paid: {events}\nSun made: {sun:0}";
}
