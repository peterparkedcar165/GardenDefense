// a reference counted HealCanCrit token on the plant (Entity.Heal already rolls a +50% crit for any
// healer that has it) plus heal crit chance
public class CriticalRejuvenationMulchEffect : MulchEffect
{
    private readonly CriticalRejuvenationMulchData typedData;
    private float applied;
    private bool on;

    public CriticalRejuvenationMulchEffect(CriticalRejuvenationMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null || on) return;
        on = true;
        applied = V(typedData.healCritChance);
        Owner.AddHealCanCrit();
        Owner.criticalHealChanceAdder += applied;
    }

    public override void Deactivate()
    {
        if (Owner == null || !on) return;
        on = false;
        Owner.RemoveHealCanCrit();
        Owner.criticalHealChanceAdder -= applied;
        applied = 0f;
    }
}
