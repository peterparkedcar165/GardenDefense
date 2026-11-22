// a reference counted HealCanCrit token on the plant (Entity.Heal already rolls a +50% crit for any
// healer that has it) plus heal crit chance
public class CriticalRejuvenationMulchEffect : MulchEffect
{
    private readonly CriticalRejuvenationMulchData typedData;
    private float applied;
    private bool on;
    // its plant's heal crits this level and the healing they added (a heal crit is 1.5 times, Entity.Heal)
    private int crits;
    private float extra;
    private const float HealCritMultiplier = 1.5f;

    public CriticalRejuvenationMulchEffect(CriticalRejuvenationMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null || on) return;
        on = true;
        Entity.OnHeal += HandleHeal;
        applied = V(typedData.healCritChance);
        Owner.AddHealCanCrit();
        Owner.criticalHealChanceAdder += applied;
    }

    public override void Deactivate()
    {
        if (Owner == null || !on) return;
        on = false;
        Entity.OnHeal -= HandleHeal;
        Owner.RemoveHealCanCrit();
        Owner.criticalHealChanceAdder -= applied;
        applied = 0f;
    }

    public override string GetStatus() => $"Heal crits: {crits} (+{extra:0} healing)";

    private void HandleHeal(EntityEventData heal)
    {
        if (Owner == null || heal.source != Owner || !heal.isCrit) return;
        crits++;
        extra += heal.amount - heal.amount / HealCritMultiplier;
    }
}
