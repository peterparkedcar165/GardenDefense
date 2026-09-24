using UnityEngine;
using System.Collections.Generic;

public class FloralGlowEffect : StatusEffect
{
    private const float ExplosionRadius = 2f;

    private readonly Calendula calendula;
    private float cachedLightRange;
    private float healTickTimer;
    private const float HealInterval = 0.5f;
    // flat + percent max health healed per second - scaled by HealInterval at each tick so
    // ticking more often (smoother health bar movement) doesn't change the actual heal rate
    private const float HealFlat = 6f;
    private const float HealPercent = 0.03f;

    public FloralGlowEffect(Entity target, float duration, int level, Entity source, Calendula calendula)
        : base(target, duration, level, source)
    {
        this.calendula  = calendula;
        effectType      = Type.positive;
        elementalType   = ElementalType.Fire;
        sourceStackable = true;
    }

    // 25% base, +5% per level, 50% at max level, defined on CalendulaData
    private float DamageScaling => calendula?.FloralGlowDamageScaling ?? 0.25f;

    private float BestRangeExcluding(FloralGlowEffect exclude)
    {
        float best = 0f;
        foreach (var e in target.activeEffects)
            if (e is FloralGlowEffect fg && fg != exclude && fg.cachedLightRange > best)
                best = fg.cachedLightRange;
        return best;
    }

    public override void OnApply()
    {
        Plant plant = target as Plant;
        if (plant == null) return;
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Floral Glow", new Color(1f, 0.6f, 0f));

        float prevBest = BestRangeExcluding(this);
        cachedLightRange = calendula?.baseLightEmissionRange ?? 0f;
        float delta = Mathf.Max(prevBest, cachedLightRange) - prevBest;
        if (delta > 0f)
        {
            plant.lightEmissionRangeAdder += delta;
            plant.UpdateStats();
        }
    }

    // Nurturing Glow: while active, Floral Glow also sustains its target with a flat + percent heal
    public override void OnTick(float deltaTime)
    {
        if (calendula == null || !calendula.NurturingGlowActive) return;
        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;

        healTickTimer += deltaTime;
        if (healTickTimer < HealInterval) return;
        healTickTimer -= HealInterval;
        plant.Heal((HealFlat + plant.maxHealth * HealPercent) * HealInterval, calendula);
    }

    public override void OnExpire()
    {
        Plant plant = target as Plant;
        if (plant == null) return;

        float currentBest = Mathf.Max(BestRangeExcluding(this), cachedLightRange);
        float nextBest = BestRangeExcluding(this);
        float delta = currentBest - nextBest;
        if (delta > 0f)
        {
            plant.lightEmissionRangeAdder -= delta;
            plant.UpdateStats();
        }
    }

    public void Trigger(Insect insect, float effectiveness)
    {
        if (calendula == null || insect == null || !insect.IsAlive) return;
        calendula.StartCoroutine(DelayedFireHit(insect, effectiveness));
    }

    private System.Collections.IEnumerator DelayedFireHit(Insect insect, float effectiveness)
    {
        yield return new UnityEngine.WaitForSeconds(0.03f);
        if (calendula == null || insect == null || !insect.IsAlive) yield break;
        float hitDamage = calendula.attackDamage * DamageScaling + calendula.skillDamageMultiplier * calendula.magicPower;
        DamageTag[] tags = new DamageTag[] { DamageTag.SkillDamage, DamageTag.Coordinated, DamageTag.OnHit };
        insect.Damage(hitDamage, DamageType.Magic, ElementalType.Fire, calendula, false, tags, false, effectiveness);

        // max level, the hit explodes, catching other insects within a small radius
        if (!calendula.IsPath3Maxed) yield break;
        Vector3 origin = insect.transform.position;
        Vector3 visualOrigin = insect.visual != null ? insect.visual.position : origin;
        calendula.SpawnFireBurst(visualOrigin, ExplosionRadius, 0.5f);
        DamageTag[] splashTags = new DamageTag[] { DamageTag.SkillDamage, DamageTag.Coordinated, DamageTag.AoE, DamageTag.OnHit };

        // snapshot every target in the blast before any of them take damage, then delay each
        // hit to match how long the visual burst takes to actually reach that distance
        const float burstLifetime = Calendula.FireBurstLifetime;
        List<Insect> splashTargets = new List<Insect>();
        foreach (Insect other in Insect.allInsects)
        {
            if (other == null || other == insect || !other.IsAlive || other.team == Team.Friendly) continue;
            if (Vector3.Distance(origin, other.transform.position) > ExplosionRadius) continue;
            splashTargets.Add(other);
        }
        foreach (Insect other in splashTargets)
        {
            float dist = Vector3.Distance(origin, other.transform.position);
            float delay = ExplosionRadius > 0f ? (dist / ExplosionRadius) * burstLifetime : 0f;
            // splash targets (anyone but the main hit insect) take half damage, flat - no falloff by distance
            calendula.StartCoroutine(DelayedSplashHit(other, hitDamage * 0.5f, splashTags, delay, effectiveness));
        }
    }

    private System.Collections.IEnumerator DelayedSplashHit(Insect other, float damage, DamageTag[] tags, float delay, float effectiveness)
    {
        if (delay > 0f) yield return new UnityEngine.WaitForSeconds(delay);
        if (other == null || !other.IsAlive) yield break;
        other.Damage(damage, DamageType.Magic, ElementalType.Fire, calendula, false, tags, false, effectiveness);
    }

    private float CoordinatedDamage =>
        ((calendula?.attackDamage ?? 0f) * DamageScaling + (calendula?.skillDamageMultiplier ?? 0f) * (calendula?.magicPower ?? 0f))
        * (1f + (calendula?.coordinatedDamage ?? 0f));

    public override string GetName() => "<color=orange>Floral Glow</color>";
    public override string GetDescription()
    {
        string desc = $"Attacks inflict a <color=orange><b>Coordinated</b></color> <color=green><b>{CoordinatedDamage:F0}</b></color> <color=orange><b>Fire</b></color> <color=#FFB6C1><b>Magic</b></color> damage hit from the <color=orange><b>Calendula</b></color>.";
        if (calendula != null && calendula.NurturingGlowActive)
        {
            Plant plant = target as Plant;
            float heal = HealFlat + (plant?.maxHealth ?? 0f) * HealPercent;
            desc += $"\n\nRegenerates <color=green><b>{heal:F0}</b></color> Health per second.";
        }
        return desc;
    }
}
