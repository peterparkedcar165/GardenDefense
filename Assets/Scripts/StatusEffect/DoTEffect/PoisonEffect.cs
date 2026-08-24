using UnityEngine;

public class PoisonedEffect : DoTEffect, IElementalAffinityEffect
{
    private ParticleSystem poisonParticles;
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    private float currentHealthPercent = 0.02f;
    private float currentFlatDamage = 4f;
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
        $"Deals escalating <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage each second, starting at <color=green><b>2%</b></color> Max Health + <color=green><b>4</b></color>, increasing by <color=green><b>0.5%</b></color> Max Health and <color=green><b>2</b></color> flat per tick.";

    public override void OnReapply(StatusEffect previous)
    {
        if (previous is PoisonedEffect old)
        {
            currentHealthPercent = old.currentHealthPercent + 0.02f;
            currentFlatDamage    = old.currentFlatDamage + 4f;
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

        float damage = target.maxHealth * currentHealthPercent + currentFlatDamage * (1f + cachedElementalAffinity);
        if (source != null)
            target.Damage(damage, DamageType.Magic, ElementalType.Poison, source, source.DotCanCrit || source.ElementalReactionCanCrit, tickTags);
        else
            target.Damage(damage, DamageType.Magic, ElementalType.Poison, tickTags);

        currentHealthPercent += 0.005f;
        currentFlatDamage    += 2f;
        tickTimer            -= tickInterval;
    }

    public override void OnExpire()
    {
        if (poisonParticles != null)
            Object.Destroy(poisonParticles.gameObject);
    }
}
