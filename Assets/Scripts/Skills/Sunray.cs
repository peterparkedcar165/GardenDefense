using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sunray : MonoBehaviour
{
    private float damagePerSecond;
    private float aoeRadius;
    private float duration;
    private Plant source;
    private const float tickInterval = 0.1f;
    private const float oscillationSpeed = 40f;
    private const float oscillationAngle = 40f;
    private const float shrinkDuration = 0.2f;

    private Vector3 targetScale;

    private static readonly DamageTag[] damageTags = new DamageTag[] { DamageTag.SkillDamage, DamageTag.AoE };

    private void Awake()
    {
        targetScale = transform.localScale;
    }

    public void Initialize(float damagePerSecond, float aoeRadius, float duration, Plant source)
    {
        this.damagePerSecond = damagePerSecond;
        this.aoeRadius = aoeRadius;
        this.duration = duration;
        this.source = source;
        StartCoroutine(SunrayRoutine());
    }

    private IEnumerator SunrayRoutine()
    {
        float elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rotY = Mathf.Sin(elapsed * oscillationSpeed) * oscillationAngle;
            transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            if (elapsed >= nextTick)
            {
                List<Insect> snapshot = new List<Insect>(Insect.allInsects);
                foreach (Insect insect in snapshot)
                {
                    if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius)
                        insect.Damage(damagePerSecond * tickInterval, DamageType.Magic, ElementalType.Fire, source, true, damageTags);
                }
                nextTick += tickInterval;
            }
            yield return null;
        }

        // shrink X to 0
        float t = 0f;
        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            float scaleX = Mathf.Lerp(targetScale.x, 0f, t / shrinkDuration);
            transform.localScale = new Vector3(scaleX, targetScale.y, targetScale.z);
            yield return null;
        }

        Destroy(gameObject);
    }
}
