using UnityEngine;
using System.Collections.Generic;

// Holly's skill: a beam that extends from her toward the targeted point and lingers there,
// dealing Physical damage per second to insects within it and afflicting plants within it with
// her Snow Ward (shield/cooling/Sandstorm immunity, all handled by the effect itself) at a
// boosted power. recreated from the old Snowdrop-era Blizzard (deleted in 2b7c04c) - the
// chill/slow side of that version is gone entirely, and plants get SnowWardEffect (routed
// through Holly.ApplySnowWard, same exclusivity rule as her passive) instead of the old
// standalone BlizzardCoolingEffect
public class Blizzard : MonoBehaviour
{
    private Vector2 origin;
    private Vector2 direction;
    private float width;
    private float duration;
    private float damagePerSecond;
    private Holly source;

    private float auraRampPerSecond;
    private float auraShieldMax;
    private float auraCoolingPerSecond;
    private float auraTickInterval;
    private float auraMagicResistanceBonus;
    private float auraLingerDuration;

    private float tickTimer;
    private float tickInterval = 0.5f;
    private float maxRange;
    private const float extendDuration  = 0.6f;
    private const float retractDuration = 1f;
    private float currentLength = 0f;
    private float beamStart = 0f;
    private float beamEnd   = 0f;

    [SerializeField] private SpriteRenderer visualRenderer;

    private static readonly DamageTag[] blizzardDamageTags = { DamageTag.AoE, DamageTag.DoT, DamageTag.SkillDamage };

    public void Initialize(Vector2 origin, Vector2 direction, float width, float duration, float maxRange,
        float damagePerSecond, float tickInterval, Holly source,
        float auraRampPerSecond, float auraShieldMax, float auraCoolingPerSecond, float auraTickInterval,
        float auraMagicResistanceBonus, float auraLingerDuration)
    {
        this.origin           = origin;
        this.direction        = direction.normalized;
        this.width            = width;
        this.duration         = duration;
        this.maxRange         = maxRange;
        this.damagePerSecond  = damagePerSecond;
        this.tickInterval     = Mathf.Max(0.05f, tickInterval);
        this.source           = source;
        this.auraRampPerSecond        = auraRampPerSecond;
        this.auraShieldMax            = auraShieldMax;
        this.auraCoolingPerSecond     = auraCoolingPerSecond;
        this.auraTickInterval         = auraTickInterval;
        this.auraMagicResistanceBonus = auraMagicResistanceBonus;
        this.auraLingerDuration       = auraLingerDuration;

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
        if (source == null || !source.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        duration -= Time.deltaTime;
        if (duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        tickTimer += Time.deltaTime;
        bool dealTick = tickTimer >= tickInterval;

        List<Insect> insectSnapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in insectSnapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (!IsInBeam(insect.transform.position)) continue;
            if (dealTick)
                insect.Damage(damagePerSecond * tickInterval, source.damageType, source.elementalType, source, false, blizzardDamageTags);
        }

        if (dealTick)
            tickTimer -= tickInterval;

        List<Plant> plantSnapshot = new List<Plant>(Plant.allPlants);
        foreach (Plant plant in plantSnapshot)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (!IsInBeam(plant.transform.position)) continue;
            Holly.ApplySnowWard(plant, source, auraLingerDuration, auraRampPerSecond, auraShieldMax, auraCoolingPerSecond, auraTickInterval, auraMagicResistanceBonus);
        }

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
