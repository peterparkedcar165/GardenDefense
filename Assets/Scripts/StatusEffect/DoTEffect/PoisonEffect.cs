using UnityEngine;

public class PoisonEffect : DoTEffect
{

    private ParticleSystem poisonParticles;
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.PassiveDamage };

    private float additionalDPS;

    public PoisonEffect(Entity target, float duration, int level, Entity source, float additionalDPS = 0f) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        tickInterval = 0.5f;
        this.additionalDPS = additionalDPS;
    }

    public override string GetName() => "<color=purple>Poison</color>";
    public override string GetDescription() => $"Deal <color=green><b>{damagePerSecond:F0}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per second.";

    public override void OnApply()
    {
        base.OnApply();
        damagePerSecond = 12 + (4 * (level - 1)) + additionalDPS;
        Debug.Log("Poison applied at level " + level);

        GameObject fx = Object.Instantiate(Resources.Load<GameObject>("PoisonBubbles"), target.transform.position, Quaternion.identity);
        fx.transform.SetParent(target.transform);
        fx.transform.localPosition = Vector3.zero;
        poisonParticles = fx.GetComponent<ParticleSystem>();
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer >= tickInterval)
        {
            if (source != null)
                target.Damage((damagePerSecond * tickInterval), DamageType.Magic, ElementalType.Poison, source, false, tickTags);
            else
                target.Damage((damagePerSecond * tickInterval), DamageType.Magic, ElementalType.Poison, tickTags);
            tickTimer -= tickInterval;
        }
    }

    public override void OnExpire()
    {
        Debug.Log("Poison expired");

        if (poisonParticles != null)
        {
            Object.Destroy(poisonParticles.gameObject);
        }
    }
}
