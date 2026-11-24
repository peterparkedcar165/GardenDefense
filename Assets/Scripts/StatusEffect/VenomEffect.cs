using UnityEngine;

public class VenomEffect : StatusEffect
{
    // shared by every instance and every tick: nothing writes into them
    private static readonly EffectTag[] DotEffectTags = { EffectTag.DoT };
    private static readonly DamageTag[] TickTags = { DamageTag.DoT };

    private readonly float damagePerSecond;
    // the shared over time timing (OverTimeTicker): a tick every half second
    private readonly OverTimeTicker ticks = new OverTimeTicker(0.5f);

    public VenomEffect(Entity target, float duration, int level, Entity source, float damagePerSecond)
        : base(target, duration, level, source)
    {
        this.damagePerSecond = damagePerSecond;
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
        sourceStackable = true;
        tags = DotEffectTags;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Venom", new Color(0.6f, 0.2f, 0.8f));
    }

    public override void OnTick(float deltaTime)
    {
        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;
        int due = ticks.Advance(deltaTime);
        for (int i = 0; i < due && plant.IsAlive; i++) DealTick(plant, 1f);
    }

    public override void OnNaturalExpire()
    {
        float share = ticks.LeftoverShare(duration);
        if (share > 0.0001f && target is Plant plant && plant.IsAlive) DealTick(plant, share);
    }

    private void DealTick(Plant plant, float share) =>
        plant.Damage(damagePerSecond * ticks.baseInterval * share, DamageType.Magic, ElementalType.Poison, source, source.DotCanCrit,
            TickTags);

    public override string GetName() => "<color=purple>Venom</color>";
    public override string GetDescription() => $"Inflicts <color=green><b>{damagePerSecond:F0}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per second.";
}
