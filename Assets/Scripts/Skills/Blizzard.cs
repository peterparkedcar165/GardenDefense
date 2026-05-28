using UnityEngine;
using System.Collections.Generic;

public class Blizzard : MonoBehaviour
{
    private Vector2 origin;
    private Vector2 direction;
    private float width;
    private float duration;
    private float damage;
    private int chillLevel;
    private Plant source;
    private float baseSlow;
    private float slowPerLevel;
    private float coolingPerSecond;

    private float tickTimer;
    private const float tickInterval    = 0.25f;
    private float maxRange;
    private const float extendDuration  = 0.6f;
    private const float retractDuration = 1f;
    private float currentLength = 0f;
    private float beamStart = 0f;
    private float beamEnd   = 0f;

    [SerializeField] private SpriteRenderer visualRenderer;

    public void Initialize(Vector2 origin, Vector2 direction, float width, float duration,
                           float damage, int chillLevel, Plant source,
                           float baseSlow, float slowPerLevel, float coolingPerSecond, float maxRange)
    {
        this.origin           = origin;
        this.direction        = direction.normalized;
        this.width            = width;
        this.duration         = duration;
        this.damage           = damage;
        this.chillLevel       = chillLevel;
        this.source           = source;
        this.baseSlow         = baseSlow;
        this.slowPerLevel     = slowPerLevel;
        this.coolingPerSecond = coolingPerSecond;
        this.maxRange         = maxRange;

        if (visualRenderer != null)
        {
            float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
            visualRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            visualRenderer.transform.localPosition = Vector3.zero;
            visualRenderer.transform.localScale    = new Vector3(0f, width, 1f);
        }
    }

    private void Update()
    {
        duration -= Time.deltaTime;
        if (duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        tickTimer += Time.deltaTime;

        // ── Insects ────────────────────────────────────────────────────────
        List<Insect> insectSnapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in insectSnapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (!IsInBeam(insect.transform.position)) continue;

            insect.ApplyEffect(new ChillEffect(insect, 4f, chillLevel, source, baseSlow, slowPerLevel));

            if (tickTimer >= tickInterval)
                insect.Damage(damage * tickInterval, DamageType.Magic, ElementalType.Ice, source, false,
                    new DamageTag[] { DamageTag.AoE, DamageTag.DoT, DamageTag.SkillDamage });
        }

        // ── Plants ─────────────────────────────────────────────────────────
        List<Plant> plantSnapshot = new List<Plant>(Plant.allPlants);
        foreach (Plant plant in plantSnapshot)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (!IsInBeam(plant.transform.position)) continue;

            plant.ApplyEffect(new CoolingEffect(plant, 0.5f, 2, source, coolingPerSecond));
        }

        if (tickTimer >= tickInterval)
            tickTimer -= tickInterval;

        // ── Visual ─────────────────────────────────────────────────────────
        if (visualRenderer != null)
        {
            if (duration > retractDuration)
            {
                currentLength = Mathf.MoveTowards(currentLength, maxRange, (maxRange / extendDuration) * Time.deltaTime);
                beamStart = 0f;
                beamEnd   = currentLength;
                visualRenderer.transform.localPosition = (Vector3)(direction * currentLength * 0.5f);
                visualRenderer.transform.localScale    = new Vector3(currentLength, width, 1f);
            }
            else
            {
                float remainingLength = (duration / retractDuration) * maxRange;
                float nearEdge = maxRange - remainingLength;
                beamStart = nearEdge;
                beamEnd   = maxRange;
                visualRenderer.transform.localPosition = (Vector3)(direction * (nearEdge + remainingLength * 0.5f));
                visualRenderer.transform.localScale    = new Vector3(remainingLength, width, 1f);
            }

            Color c = visualRenderer.color;
            c.a = duration <= retractDuration ? (duration / retractDuration) * 0.5f : 0.5f;
            visualRenderer.color = c;
        }
    }

    private bool IsInBeam(Vector2 point)
    {
        Vector2 toPoint = point - origin;
        float dot = Vector2.Dot(toPoint, direction);
        if (dot < beamStart || dot > beamEnd) return false;
        Vector2 perp = toPoint - direction * dot;
        return perp.magnitude <= width * 0.5f;
    }
}
