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
        tags = new EffectTag[] { EffectTag.DoT };
    }

    public override void OnApply()
    {
        GameObject indicator = Object.Instantiate(
            Resources.Load<GameObject>("DamageIndicator"),
            target.transform.position + new Vector3(0.4f, 0f, 0f),
            Quaternion.identity);
        indicator.GetComponent<DamageIndicator>()?.Initialize("Venom", new Color(0.6f, 0.2f, 0.8f));
    }

    public override void OnTick(float deltaTime)
    {
        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;
        tickTimer += deltaTime;
        if (tickTimer < tickInterval) return;
        tickTimer -= tickInterval;
        plant.Damage(damagePerSecond * tickInterval, DamageType.Magic, ElementalType.Poison, source, false,
            new DamageTag[] { DamageTag.DoT });
    }

    public override string GetName() => "<color=#9B30D0>Venom</color>";
    public override string GetDescription() => $"Inflicts <color=green><b>{damagePerSecond:F0}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per second.";
}
