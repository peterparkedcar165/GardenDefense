using UnityEngine;

public class PoisonEffect : StatusEffect
{
    private float tickTimer = 0f;
    private const float tickInterval = 1f;
    public float damagePerSecond;
    public PoisonEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {
        damagePerSecond = 1 + level;
        Debug.Log("Poison applied at level " + level);
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer >= tickInterval)
        {
        target.Damage((damagePerSecond * tickInterval), DamageType.Magic, ElementalType.Poison, source, false, new DamageTag[] {DamageTag.DoT});
        tickTimer -= tickInterval;
        }
    }

    public override void OnExpire()
    {
        Debug.Log("Poison expired");
    }
}
