// lifesteal: Entity.Damage heals the damage source by its lifesteal share of every hit
public class LifesapMulchEffect : MulchEffect
{
    private readonly LifesapMulchData typedData;
    private float applied;

    public LifesapMulchEffect(LifesapMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.lifesteal);
        Owner.lifestealAdder += applied;
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.lifestealAdder -= applied;
        applied = 0f;
    }
}
