using UnityEngine;

public class VenomEffect : StatusEffect
{
    private readonly float damagePerSecond;
    private const float tickInterval = 0.5f;
    private float tickTimer = 0f;

    public VenomEffect(Entity target, float duration, int level, Entity source, float damagePerSecond)
        : base(target, duration, level, source)
    {
        this.damagePerSecond = damagePerSecond;
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
        sourceStackable = true;
        tags = new EffectTag[] { EffectTag.DoT };
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Venom", new Color(0.6f, 0.2f, 0.8f));
    }

    public override void OnTick(float deltaTime)
    {
        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;
        tickTimer += deltaTime;
        if (tickTimer < tickInterval) return;
        tickTimer -= tickInterval;
        plant.Damage(damagePerSecond * tickInterval, DamageType.Magic, ElementalType.Poison, source, source.DotCanCrit,
            new DamageTag[] { DamageTag.DoT });
    }

    public override string GetName() => "<color=purple>Venom</color>";
    public override string GetDescription() => $"Inflicts <color=green><b>{damagePerSecond:F0}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per second.";
}
