// Hinder: Plant.mulchSlowBonus, which every slowing effect adds when this plant is its source
// (MulchManager.SlowBonusFrom: SlowEffect, SoakedEffect, MireSlowEffect, SnowMarkEffect's chilling
// grasp, FrostbiteEffect). baked in when a slow is applied, so slows already running keep their
// amount until they refresh
public class SlowBonusMulchEffect : MulchEffect
{
    private readonly SlowBonusMulchData typedData;
    private float applied;
    // slows its plant made this level while it was on (MulchManager.SlowBonusFrom counts them)
    private int slows;

    public SlowBonusMulchEffect(SlowBonusMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.flatBonus);
        Owner.mulchSlowBonus += applied;
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.mulchSlowBonus -= applied;
        applied = 0f;
    }

    public void CountSlow() => slows++;

    public override string GetStatus() => $"Slows made stronger: {slows}";
}
