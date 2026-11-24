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
    // the same insects as affectedInsects, for a quick inside check every frame
    private readonly HashSet<Insect> affectedSet = new HashSet<Insect>();
    // reused copy of the insect list, refilled each frame instead of allocating a new one
    private readonly List<Insect> insectSnapshot = new List<Insect>();

    // how many poison fields each insect stands in right now. its debuffs stay frozen until it
    // has left every one of them, so leaving one of two overlapping fields no longer unfreezes it
    private static readonly Dictionary<Insect, int> fieldsHolding = new Dictionary<Insect, int>();

    // the source as a poison shroom and its lingering toxins unlock, read once in Initialize
    private PoisonShroom shroom;
    private bool lingeringToxins;

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

        shroom = source as PoisonShroom;
        delayedBloom = shroom != null && SkillTreeManager.HasUnlock(shroom, PoisonShroom.DelayedBloomUnlock);
        lingeringToxins = shroom != null && SkillTreeManager.HasUnlock(shroom, PoisonShroom.LingeringToxinsUnlock);
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

        Vector3 center = transform.position;
        float radiusSqr = radius * radius;

        insectSnapshot.Clear();
        insectSnapshot.AddRange(Insect.allInsects);
        for (int i = 0; i < insectSnapshot.Count; i++)
        {
            Insect insect = insectSnapshot[i];
            if (insect == null || !insect.IsAlive) continue;
            bool inside = (insect.transform.position - center).sqrMagnitude <= radiusSqr;
            bool tracked = affectedSet.Contains(insect);

            if (inside && !tracked)
            {
                affectedInsects.Add(insect);
                affectedSet.Add(insect);
                Hold(insect);
            }
            else if (!inside && tracked)
            {
                affectedInsects.Remove(insect);
                affectedSet.Remove(insect);
                Release(insect);
            }
        }

        for (int i = affectedInsects.Count - 1; i >= 0; i--)
        {
            if (affectedInsects[i] == null || affectedInsects[i].gameObject == null)
            {
                affectedSet.Remove(affectedInsects[i]);
                fieldsHolding.Remove(affectedInsects[i]);
                affectedInsects.RemoveAt(i);
            }
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
        ReleaseAll();
        Destroy(gameObject);
    }

    // a field removed any other way (scene unload) lets go of its insects too
    private void OnDestroy() => ReleaseAll();

    private void ReleaseAll()
    {
        foreach (Insect insect in affectedInsects)
        {
            if (insect != null) Release(insect);
            else fieldsHolding.Remove(insect);
        }
        affectedInsects.Clear();
        affectedSet.Clear();
    }

    private static void Hold(Insect insect)
    {
        fieldsHolding.TryGetValue(insect, out int count);
        fieldsHolding[insect] = count + 1;
        insect.debuffsFrozen = true;
    }

    private static void Release(Insect insect)
    {
        fieldsHolding.TryGetValue(insect, out int count);
        if (count > 1)
        {
            fieldsHolding[insect] = count - 1;
            return;
        }
        fieldsHolding.Remove(insect);
        insect.debuffsFrozen = false;
    }
}
