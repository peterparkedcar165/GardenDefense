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
        duration -= Time.deltaTime;
        if (duration <= 0f) { Destroy(gameObject); return; }

        tickTimer += Time.deltaTime;
        if (tickTimer < tickInterval) return;
        tickTimer -= tickInterval;

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) <= radius)
            {
                plant.Heal(healPerTick, source);
                plant.temperature = Mathf.Max(plant.temperature - tempReduction, 10f);
            }
        }
    }
}
