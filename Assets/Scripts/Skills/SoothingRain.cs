using UnityEngine;
using System.Collections.Generic;

public class SoothingRain : MonoBehaviour
{
    // hidden passive: same anti-Scorch hook as Aloe Vera's own heal splash, but weaker per tick
    // since the rain ticks repeatedly over its whole duration
    private const float ScorchReductionPerTick = 0.10f;

    // Monsoon (skill tree): the rain grows from its base radius up to 1.3x over the first half
    // of its duration, then holds at 1.3x for the back half
    private const float MonsoonMaxRadiusMultiplier = 1.3f;

    // Cleansing Rain (skill tree): debuffs on a plant standing in the rain deplete faster -
    // this is EXTRA depletion on top of the normal per-frame countdown every effect already
    // gets, so "2x" / "4x" total means +1x / +3x extra here
    private const float CleansingRainExtraRate = 1f;
    private const float CleansingRainFireExtraRate = 3f;

    private float radius;
    private float baseRadius;
    private float duration;
    private float totalDuration;
    private float healPerTick;
    private float tickInterval;
    private float tempReduction;
    private AloeVera source;
    private float tickTimer = 0f;

    public void Initialize(float radius, float duration, float healPerTick, float tickInterval, float tempReduction, AloeVera source)
    {
        this.radius        = radius;
        this.baseRadius     = radius;
        this.duration       = duration;
        this.totalDuration  = duration;
        this.healPerTick   = healPerTick;
        this.tickInterval  = tickInterval;
        this.tempReduction = tempReduction;
        this.source        = source;
        float s = radius * 2f;
        transform.localScale = new Vector3(s, s, 1f);
    }

    private void Update()
    {
        if (source == null || !source.IsAlive) { Destroy(gameObject); return; }

        duration -= Time.deltaTime;
        if (duration <= 0f) { Destroy(gameObject); return; }

        bool monsoon = SkillTreeManager.HasUnlock(source, AloeVera.MonsoonUnlock);
        if (monsoon)
        {
            float elapsed = totalDuration - duration;
            float halfDuration = totalDuration * 0.5f;
            float growthFraction = halfDuration > 0f ? Mathf.Clamp01(elapsed / halfDuration) : 1f;
            radius = baseRadius * Mathf.Lerp(1f, MonsoonMaxRadiusMultiplier, growthFraction);
            float s = radius * 2f;
            transform.localScale = new Vector3(s, s, 1f);
        }

        // Cleansing Rain (skill tree): applied every frame, independent of the heal-tick cadence
        // below, so the extra depletion is smooth for as long as a plant stays inside the rain
        if (SkillTreeManager.HasUnlock(source, AloeVera.CleansingRainUnlock))
        {
            foreach (Plant plant in new List<Plant>(Plant.allPlants))
            {
                if (plant == null || !plant.IsAlive) continue;
                if (Vector3.Distance(transform.position, plant.transform.position) > radius) continue;
                foreach (StatusEffect effect in new List<StatusEffect>(plant.activeEffects))
                {
                    if (effect.effectType != StatusEffect.Type.negative) continue;
                    float extraRate = effect.elementalType == ElementalType.Fire ? CleansingRainFireExtraRate : CleansingRainExtraRate;
                    effect.duration -= extraRate * Time.deltaTime;
                }
            }
        }

        tickTimer += Time.deltaTime;
        if (tickTimer < tickInterval) return;
        tickTimer -= tickInterval;

        bool path3Maxed = source != null && source.IsPath3Maxed;
        int rainLevel = (source != null ? source.effectivePath3Level : 0) + 1;
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > radius) continue;
            float scaledHeal = healPerTick * (1f + plant.healingReceived) * (1f + plant.healingBonus);
            float overflow = path3Maxed ? Mathf.Max(0f, scaledHeal - plant.MissingHealth) : 0f;
            plant.Heal(healPerTick, source);
            plant.temperature = Mathf.Max(plant.temperature - tempReduction, 10f);
            plant.GetEffect<ScorchEffect>()?.ReduceByFraction(ScorchReductionPerTick);

            // this is literally rain, apply the same exposure effect real rain weather grants.
            // short duration so it fades shortly after a plant leaves the rain, refreshed every
            // tick while still inside it
            RainExposedEffect rain = new RainExposedEffect(plant, source, rainLevel);
            rain.duration = tickInterval + 0.5f;
            plant.ApplyEffect(rain);

            if (monsoon && plant.IsAlive)
            {
                MonsoonSurgeEffect surge = new MonsoonSurgeEffect(plant, source);
                surge.duration = tickInterval + 0.5f;
                plant.ApplyEffect(surge);
            }

            if (plant.IsAlive)
            {
                DrizzleBarrierEffect shield = plant.GetEffect<DrizzleBarrierEffect>();
                if (overflow > 0f)
                {
                    if (shield == null)
                    {
                        shield = new DrizzleBarrierEffect(plant, source);
                        plant.ApplyEffect(shield);
                        shield = plant.GetEffect<DrizzleBarrierEffect>();
                    }
                    shield?.AddShield(overflow);
                }
                shield?.RefreshDuration();
            }
        }

        // friendly minions are also healed by the rain
        foreach (Insect ally in new List<Insect>(Insect.friendlyInsects))
        {
            if (ally == null || !ally.IsAlive) continue;
            if (Vector3.Distance(transform.position, ally.transform.position) <= radius)
                ally.Heal(healPerTick, source);
        }
    }
}
