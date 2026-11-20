// a reference counted DotCanCrit token on the plant plus damage over time crit chance (read by
// Entity.Damage for DoT tagged hits)
public class CriticalRotMulchEffect : MulchEffect
{
    private readonly CriticalRotMulchData typedData;
    private float applied;
    private bool on;

    public CriticalRotMulchEffect(CriticalRotMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null || on) return;
        on = true;
        applied = V(typedData.dotCritChance);
        Owner.AddDotCanCrit();
        Owner.criticalDotChanceAdder += applied;
    }

    public override void Deactivate()
    {
        if (Owner == null || !on) return;
        on = false;
        Owner.RemoveDotCanCrit();
        Owner.criticalDotChanceAdder -= applied;
        applied = 0f;
    }
}
