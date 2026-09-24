using UnityEngine;

public class PoisonedEffect : DoTEffect, IElementalAffinityEffect
{
    private ParticleSystem poisonParticles;
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    private const float missingHealthPercent = 0.04f;
    private const float flatDamage = 4f;
    private float cachedElementalAffinity;

    public float AffinityPower => source?.elementalAffinity ?? 0f;

    public PoisonedEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
        tickInterval = 1f;
    }

    public override string GetName() => "<color=purple>Poisoned</color>";
    public override string GetDescription() =>
        $"Deal <color=purple><b>{ComputeDamage():F0}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per second.";

    // reads missing health fresh every tick, so this naturally hits harder the more hurt the
    // target already is - no separate escalation state needed like the old version had
    private float ComputeDamage() =>
        ((target.maxHealth - target.health) * missingHealthPercent + flatDamage) * (1f + 0.33f * cachedElementalAffinity);

    public override void OnApply()
    {
        cachedElementalAffinity = source?.elementalAffinity ?? 0f;
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Poisoned", new Color(0.6f, 0.1f, 0.8f));
        GameObject fx = Object.Instantiate(Resources.Load<GameObject>("PoisonBubbles"), target.transform.position, Quaternion.identity);
        fx.transform.SetParent(target.transform);
        fx.transform.localPosition = Vector3.zero;
        poisonParticles = fx.GetComponent<ParticleSystem>();
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer < tickInterval) return;

        float damage = ComputeDamage();
        if (source != null)
            target.Damage(damage, DamageType.Magic, ElementalType.Poison, source, source.DotCanCrit || source.ElementalReactionCanCrit, tickTags);
        else
            target.Damage(damage, DamageType.Magic, ElementalType.Poison, tickTags);

        tickTimer -= tickInterval;
    }

    public override void OnExpire()
    {
        if (poisonParticles != null)
            Object.Destroy(poisonParticles.gameObject);
    }
}
