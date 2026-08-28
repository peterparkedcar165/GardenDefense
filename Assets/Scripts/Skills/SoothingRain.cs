using UnityEngine;
using System.Collections.Generic;

public class SoothingRain : MonoBehaviour
{
    private float radius;
    private float duration;
    private float healPerTick;
    private float tickInterval;
    private float tempReduction;
    private AloeVera source;
    private float tickTimer = 0f;

    public void Initialize(float radius, float duration, float healPerTick, float tickInterval, float tempReduction, AloeVera source)
    {
        this.radius        = radius;
        this.duration      = duration;
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

            // this is literally rain, apply the same exposure effect real rain weather grants.
            // short duration so it fades shortly after a plant leaves the rain, refreshed every
            // tick while still inside it
            RainExposedEffect rain = new RainExposedEffect(plant, source, rainLevel);
            rain.duration = tickInterval + 0.5f;
            plant.ApplyEffect(rain);
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
