using System.Collections.Generic;
using UnityEngine;

// Kelp's skill: a bubble that travels in a straight line from where it was fired, applying
// AirBubbleEffect to every plant within its width along the way (including Kelp itself, since
// it spawns right on top of her). destroys itself once it's traveled its max range or its source dies
public class KelpBubbleProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxRange;
    private float width;
    private float initialOxygen;
    private float regenPerSecond;
    private float effectDuration;
    private Kelp source;

    private Vector3 spawnPosition;
    private readonly HashSet<Plant> _affected = new HashSet<Plant>();

    public void Initialize(Vector2 direction, float speed, float maxRange, float width, float initialOxygen,
                           float regenPerSecond, float effectDuration, Kelp source)
    {
        this.direction      = direction.normalized;
        this.speed          = speed;
        this.maxRange       = maxRange;
        this.width          = width;
        this.initialOxygen  = initialOxygen;
        this.regenPerSecond = regenPerSecond;
        this.effectDuration = effectDuration;
        this.source         = source;
        spawnPosition = transform.position;

        SpriteRenderer visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null) visual.transform.localScale = Vector3.one * width;
    }

    private void Update()
    {
        if (source == null) { Destroy(gameObject); return; }

        CheckForPlants();

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Vector3.Distance(spawnPosition, transform.position) >= maxRange)
            Destroy(gameObject);
    }

    private void CheckForPlants()
    {
        float hitRadius = width * 0.5f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || _affected.Contains(plant)) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > hitRadius) continue;
            _affected.Add(plant);
            plant.ApplyEffect(new AirBubbleEffect(plant, effectDuration, source, initialOxygen, regenPerSecond, source.IsPath3Maxed));
        }
    }
}
