// Plant.mulchAreaBonus (a lobber's splash, Lobber.UpdateStats) and the plant's skill radius
// (skillRadiusMultiplier). plants whose areas aren't either of those don't change
public class WideCanopyMulchEffect : MulchEffect
{
    private readonly WideCanopyMulchData typedData;
    private float applied;

    public WideCanopyMulchEffect(WideCanopyMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.areaBonus);
        Owner.mulchAreaBonus += applied;
        Owner.skillRadiusMultiplier += applied;
    }

    public override void Deactivate()
    {
        if (Owner != null)
        {
            Owner.mulchAreaBonus -= applied;
            Owner.skillRadiusMultiplier -= applied;
        }
        applied = 0f;
    }
}
