using UnityEngine;

// lifesteal: Entity.Damage heals the damage source by its lifesteal share of every hit
public class LifesapMulchEffect : MulchEffect
{
    private readonly LifesapMulchData typedData;
    private float applied;
    private bool listening;
    // health its share of the plant's lifesteal gave back this level
    private float healed;

    public LifesapMulchEffect(LifesapMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.lifesteal);
        Owner.lifestealAdder += applied;
        if (!listening) { listening = true; Entity.OnHeal += HandleHeal; }
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.lifestealAdder -= applied;
        applied = 0f;
        if (listening) { listening = false; Entity.OnHeal -= HandleHeal; }
    }

    public override string GetStatus() => $"Healed: {healed:0}";

    private void HandleHeal(EntityEventData heal)
    {
        if (Owner == null || heal.target != Owner || !Entity.HasHealTag(heal.healTags, HealTag.Lifesteal)) return;
        if (Owner.lifesteal > 0f) healed += heal.amount * Mathf.Clamp01(applied / Owner.lifesteal);
    }
}
