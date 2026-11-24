using UnityEngine;

// applied by Mosquito's bite to the plant it attacks - deals flat True damage per second
// (bypassing armor/magic armor entirely) and reduces the plant's incoming healing while
// active. Mosquito's own self-heal (Bloodsucker) is a separate effect applied directly on
// the Mosquito alongside this one in Mosquito.Attack, not managed from here
public class MosquitoBiteEffect : StatusEffect
{
    // shared by every instance and every tick: nothing writes into them
    private static readonly EffectTag[] DotEffectTags = { EffectTag.DoT };
    private static readonly DamageTag[] TickTags = { DamageTag.DoT };

    private readonly float damagePerSecond;
    private readonly float healingReductionPercent;
    // the shared over time timing (OverTimeTicker): a tick every second
    private readonly OverTimeTicker ticks = new OverTimeTicker(1f);

    public MosquitoBiteEffect(Entity target, float duration, int level, Entity source, float damagePerSecond, float healingReductionPercent)
        : base(target, duration, level, source)
    {
        this.damagePerSecond = damagePerSecond;
        this.healingReductionPercent = healingReductionPercent;
        effectType = Type.negative;
        elementalType = ElementalType.Grass;
        tags = DotEffectTags;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Mosquito Bite", new Color(0.55f, 0.05f, 0.05f));
        target.healingReceivedAdder -= healingReductionPercent;
    }

    public override void OnTick(float deltaTime)
    {
        int due = ticks.Advance(deltaTime);
        for (int i = 0; i < due && target.health > 0f; i++) DealTick(1f);
    }

    public override void OnNaturalExpire()
    {
        float share = ticks.LeftoverShare(duration);
        if (share > 0.0001f && target.health > 0f) DealTick(share);
    }

    private void DealTick(float share) =>
        target.Damage(damagePerSecond * ticks.baseInterval * share, DamageType.True, ElementalType.Grass, source, source.DotCanCrit,
            TickTags);

    public override void OnExpire()
    {
        target.healingReceivedAdder += healingReductionPercent;
    }

    public override string GetName() => "<color=#8B0000>Mosquito Bite</color>";
    public override string GetDescription() =>
        $"Deals <color=green><b>{damagePerSecond:F0}</b></color> <color=#6B8E23>Grass</color> True damage per second and reduces incoming healing by <color=red><b>{healingReductionPercent * 100f:F0}%</b></color>.";
}
