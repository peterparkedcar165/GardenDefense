// Plant.mulchRegenerationBonus, read by Entity.Heal for regeneration tagged heals this plant gives
// (MulchManager.RegenerationFactor). a plant's own self regeneration counts too
public class RejuvenatorMulchEffect : MulchEffect
{
    private readonly RejuvenatorMulchData typedData;
    private float applied;
    private bool listening;
    // the extra healing its share of the bonus gave this level
    private float extra;

    public RejuvenatorMulchEffect(RejuvenatorMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.effectivenessBonus);
        Owner.mulchRegenerationBonus += applied;
        if (!listening) { listening = true; Entity.OnHeal += HandleHeal; }
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.mulchRegenerationBonus -= applied;
        applied = 0f;
        if (listening) { listening = false; Entity.OnHeal -= HandleHeal; }
    }

    public override string GetStatus() => $"Extra healing: {extra:0}";

    // a regeneration heal its plant received: the total bonus is the received factor minus 1, and
    // this copy's part of it is applied over that factor
    private void HandleHeal(EntityEventData heal)
    {
        if (Owner == null || heal.target != Owner || !Entity.HasHealTag(heal.healTags, HealTag.Regeneration)) return;
        float factor = MulchManager.RegenerationReceivedFactor(Owner);
        if (factor > 0f) extra += heal.amount * applied / factor;
    }
}
