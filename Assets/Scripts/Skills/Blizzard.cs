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

    private float tickTimer;
    private const float tickInterval = 0.25f;

    public void Initialize(Vector2 origin, Vector2 direction, float width, float duration, float damage, int chillLevel, Plant source)
    {
        this.origin = origin;
        this.direction = direction.normalized;
        this.width = width;
        this.duration = duration;
        this.damage = damage;
        this.chillLevel = chillLevel;
        this.source = source;
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

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (!IsInBeam(insect.transform.position)) continue;
            insect.ApplyEffect(new ChillEffect(insect, 0.25f, chillLevel, source));
            if (tickTimer >= tickInterval)
                insect.Damage(damage * tickInterval, DamageType.Magic, ElementalType.Ice, source, false, new DamageTag[] { DamageTag.AoE, DamageTag.DoT });
        }

        if (tickTimer >= tickInterval)
            tickTimer -= tickInterval;
    }

    private bool IsInBeam(Vector2 point)
    {
        Vector2 toPoint = point - origin;
        float dot = Vector2.Dot(toPoint, direction);
        if (dot < 0f) return false;
        Vector2 perp = toPoint - direction * dot;
        return perp.magnitude <= width * 0.5f;
    }
}
