using UnityEngine;
using System.Collections.Generic;

public class PoisonField : MonoBehaviour
{
    private float radius;
    private float duration;
    private Plant source;

    private float damagePerSecond;
    private const float tickInterval = 0.5f;
    private float tickTimer = 0f;

    private readonly List<Insect> affectedInsects = new List<Insect>();

    // Delayed Bloom: the field starts at `startRadius` and ramps up to `endRadius` over the first
    // 60% of `totalDuration`, then holds - both captured once in Initialize since `radius` itself
    // gets overwritten every frame once ramping starts, and `duration` counts down every frame
    private bool delayedBloom;
    private float startRadius;
    private float endRadius;
    private float totalDuration;

    // Lingering Toxins: remembers each frozen debuff's duration the moment it was first observed
    // this field-tick, so the +10%-of-original bonus below stays proportional to that effect's
    // real duration instead of the ever-growing frozen value
    private readonly Dictionary<StatusEffect, float> frozenBaselineDurations = new Dictionary<StatusEffect, float>();

    private static readonly DamageTag[] damageTags = { DamageTag.AoE, DamageTag.SkillDamage, DamageTag.DoT };

    public void Initialize(float radius, float duration, Plant source, float damagePerSecond)
    {
        this.radius = radius;
        this.duration = duration;
        this.source = source;
        this.damagePerSecond = damagePerSecond;
        totalDuration = duration;

        PoisonShroom shroom = source as PoisonShroom;
        delayedBloom = shroom != null && SkillTreeManager.HasUnlock(shroom, PoisonShroom.DelayedBloomUnlock);
        if (delayedBloom)
        {
            startRadius = radius;
            endRadius = radius * 1.5f;
        }

        float s = radius * 2f;
        transform.localScale = new Vector3(s, s, 1f);
    }

    private void Update()
    {
        if (source == null || !source.IsAlive) { Die(); return; }

        duration -= Time.deltaTime;
        if (duration <= 0f) { Die(); return; }

        if (delayedBloom)
        {
            float rampDuration = totalDuration * 0.6f;
            float t = rampDuration > 0f ? Mathf.Clamp01((totalDuration - duration) / rampDuration) : 1f;
            radius = Mathf.Lerp(startRadius, endRadius, t);
            float s = radius * 2f;
            transform.localScale = new Vector3(s, s, 1f);
        }

        PoisonShroom shroom = source as PoisonShroom;
        bool lingeringToxins = shroom != null && SkillTreeManager.HasUnlock(shroom, PoisonShroom.LingeringToxinsUnlock);

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            bool inside = Vector3.Distance(transform.position, insect.transform.position) <= radius;
            bool tracked = affectedInsects.Contains(insect);

            if (inside && !tracked)
            {
                affectedInsects.Add(insect);
                insect.debuffsFrozen = true;
            }
            else if (!inside && tracked)
            {
                affectedInsects.Remove(insect);
                insect.debuffsFrozen = false;
            }
        }

        for (int i = affectedInsects.Count - 1; i >= 0; i--)
        {
            if (affectedInsects[i] == null || affectedInsects[i].gameObject == null)
                affectedInsects.RemoveAt(i);
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            float tickDamage = damagePerSecond * tickInterval;
            bool applyToxicSpore = shroom != null && shroom.IsPath3Maxed;
            for (int i = affectedInsects.Count - 1; i >= 0; i--)
            {
                if (affectedInsects[i] == null) continue;

                affectedInsects[i].Damage(tickDamage, source.damageType, source.elementalType, source, false, damageTags);
                if (applyToxicSpore)
                    affectedInsects[i].ApplyEffect(new ToxicSporeEffect(affectedInsects[i], shroom.ToxicSporeDuration, 1, source));

                // Lingering Toxins: on top of the duration freeze above, every damage tick also
                // extends each frozen debuff's remaining time by 10% of its original duration
                if (lingeringToxins)
                {
                    foreach (StatusEffect effect in affectedInsects[i].activeEffects)
                    {
                        if (effect.effectType != StatusEffect.Type.negative) continue;
                        if (!frozenBaselineDurations.TryGetValue(effect, out float baseline))
                        {
                            baseline = effect.duration;
                            frozenBaselineDurations[effect] = baseline;
                        }
                        effect.duration += baseline * 0.10f;
                    }
                }
            }
        }
    }

    private void Die()
    {
        foreach (Insect insect in affectedInsects)
            if (insect != null) insect.debuffsFrozen = false;
        affectedInsects.Clear();
        Destroy(gameObject);
    }
}
