using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BubblePrison : MonoBehaviour
{
    private float trapRadius;
    private float duration;
    private float damagePerSecond;
    private Plant source;
    private Vector3 targetPosition;
    private Transform visual;
    private const float travelSpeed = 4f;
    private const float minTravelTime = 1f;
    private const float arcPeakHeight = 1.1f;
    private const float arcRiseEnd = 0.5f;
    private const float spawnScaleDuration = 0.3f;

    public void Initialize(Vector3 target, float trapRadius, float duration, float damagePerSecond, Plant source)
    {
        this.targetPosition = target;
        this.trapRadius = trapRadius;
        this.duration = duration;
        this.damagePerSecond = damagePerSecond;
        this.source = source;
        visual = transform.Find("Visual");
        StartCoroutine(BubbleRoutine());
        StartCoroutine(SpawnScale());
    }

    private IEnumerator SpawnScale()
    {
        Vector3 fullScale = transform.localScale;
        transform.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < spawnScaleDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, fullScale, elapsed / spawnScaleDuration);
            yield return null;
        }
        transform.localScale = fullScale;
    }

    private IEnumerator BubbleRoutine()
    {
        float totalDistance = Vector3.Distance(transform.position, targetPosition);
        float speed = Mathf.Min(travelSpeed, totalDistance / minTravelTime);
        float startY = visual != null ? visual.localPosition.y : 0f;

        // travel with parabolic arc
        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (visual is not null && totalDistance > 0f)
            {
                float progress = 1f - (Vector3.Distance(transform.position, targetPosition) / totalDistance);
                float arcY;
                if (progress < arcRiseEnd)
                    arcY = Mathf.Sin(progress / arcRiseEnd * Mathf.PI * 0.5f) * arcPeakHeight;
                else
                    arcY = Mathf.Cos((progress - arcRiseEnd) / (1f - arcRiseEnd) * Mathf.PI * 0.5f) * arcPeakHeight;
                float baseY = Mathf.Lerp(startY, 0f, progress);
                Vector3 pos = visual.localPosition;
                pos.y = arcY + baseY;
                visual.localPosition = pos;
            }

            yield return null;
        }

        transform.position = targetPosition;
        if (visual != null)
            visual.localPosition = new Vector3(visual.localPosition.x, 0f, visual.localPosition.z);

        // impact — one-time check
        ImpactInsects();

        Destroy(gameObject);
    }

    private void ImpactInsects()
    {
        Transform center = visual ?? transform;
        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(center.position, insect.transform.position) <= trapRadius)
            {
                insect.Damage(damagePerSecond, DamageType.Magic, ElementalType.Water, source, true,
                    new DamageTag[] { DamageTag.AoE, DamageTag.SkillDamage });
                insect.ApplyEffect(new BubblePrisonEffect(insect, duration, 1, source));
            }
        }
    }
}
