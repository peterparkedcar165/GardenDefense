using UnityEngine;
using System.Collections.Generic;

public class WindGust : MonoBehaviour
{
    private Vector2 origin;
    private Vector2 direction;
    private float width;
    private float duration;
    private float damage;
    private float pushForce;
    private Plant source;

    private float tickTimer;
    private const float tickInterval = 0.25f;
    private const float visualLength = 30f;
    private const float extendDuration = 0.6f;
    private const float retractDuration = 1f;
    private float currentLength = 0f;
    private float beamStart = 0f;
    private float beamEnd = 0f;

    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private LayerMask obstacleLayer;

    private static readonly DamageTag[] damageTags = { DamageTag.AoE, DamageTag.DoT, DamageTag.SkillDamage };

    public void Initialize(Vector2 origin, Vector2 direction, float width, float duration, float damage, float pushForce, Plant source)
    {
        this.origin = origin;
        this.direction = direction.normalized;
        this.width = width;
        this.duration = duration;
        this.damage = damage;
        this.pushForce = pushForce;
        this.source = source;

        if (visualRenderer != null)
        {
            float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
            visualRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            visualRenderer.transform.localPosition = Vector3.zero;
            visualRenderer.transform.localScale = new Vector3(0f, width, 1f);
        }
    }

    private void Update()
    {
        duration -= Time.deltaTime;
        if (duration <= 0f) { Destroy(gameObject); return; }

        tickTimer += Time.deltaTime;

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (!IsInBeam(insect.transform.position)) continue;

            float force = insect.isFlying ? pushForce * 1.25f : pushForce;
            insect.windVelocity += direction * force;

            if (tickTimer >= tickInterval)
            {
                insect.Damage(damage * tickInterval, DamageType.Magic, ElementalType.Wind, source, false, damageTags);
                insect.ApplyEffect(new DisplacedEffect(insect, 0.5f, 1, source));
            }
        }

        if (tickTimer >= tickInterval)
            tickTimer -= tickInterval;

        if (visualRenderer != null)
        {
            if (duration > retractDuration)
            {
                currentLength = Mathf.MoveTowards(currentLength, visualLength, (visualLength / extendDuration) * Time.deltaTime);
                beamStart = 0f;
                beamEnd = currentLength;
                visualRenderer.transform.localPosition = (Vector3)(direction * currentLength * 0.5f);
                visualRenderer.transform.localScale = new Vector3(currentLength, width, 1f);
            }
            else
            {
                float remainingLength = (duration / retractDuration) * visualLength;
                float nearEdge = visualLength - remainingLength;
                beamStart = nearEdge;
                beamEnd = visualLength;
                visualRenderer.transform.localPosition = (Vector3)(direction * (nearEdge + remainingLength * 0.5f));
                visualRenderer.transform.localScale = new Vector3(remainingLength, width, 1f);
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
