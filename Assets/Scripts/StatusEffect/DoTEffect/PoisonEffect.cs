using UnityEngine;

public class PoisonedEffect : DoTEffect, IElementalAffinityEffect
{
    private ParticleSystem poisonParticles;
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    private float currentHealthPercent = 0.01f;
    private float currentFlatDamage = 1f;
    private float cachedElementalAffinity;

    public float AffinityPower => source?.elementalAffinity ?? 0f;

    public PoisonedEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
        tickInterval = 0.5f;
    }

    public override string GetName() => "<color=purple>Poisoned</color>";
    public override string GetDescription() =>
        $"Take escalating damage over time (<color=purple><b>{ComputeDamage():F0}</b></color>).";

    private float ComputeDamage() =>
        (target.maxHealth * currentHealthPercent + currentFlatDamage) * (1f + 0.33f * cachedElementalAffinity);

    public override void OnReapply(StatusEffect previous)
    {
        if (previous is PoisonedEffect old)
        {
            currentHealthPercent = old.currentHealthPercent + 0.01f;
            currentFlatDamage    = old.currentFlatDamage + 1f;
        }
    }

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

        currentHealthPercent += 0.001f;
        currentFlatDamage    += 1f;
        tickTimer            -= tickInterval;
    }

    public override void OnExpire()
    {
        if (poisonParticles != null)
            Object.Destroy(poisonParticles.gameObject);
    }
}
