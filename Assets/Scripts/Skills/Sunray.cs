using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sunray : MonoBehaviour
{
    private float damagePerSecond;
    private float aoeRadius;
    private float duration;
    private float channelDuration;
    private Plant source;
    private const float tickInterval = 0.33f;

    private static readonly DamageTag[] damageTags = new DamageTag[] { DamageTag.SkillDamage, DamageTag.AoE };

    public void Initialize(float damagePerSecond, float aoeRadius, float duration, float channelDuration, Plant source)
    {
        this.damagePerSecond = damagePerSecond;
        this.aoeRadius = aoeRadius;
        this.duration = duration;
        this.channelDuration = channelDuration;
        this.source = source;
        StartCoroutine(SunrayRoutine());
    }

    private IEnumerator SunrayRoutine()
    {
        yield return new WaitForSeconds(channelDuration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            List<Insect> snapshot = new List<Insect>(Insect.allInsects);
            foreach (Insect insect in snapshot)
            {
                if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius)
                    insect.Damage(damagePerSecond * tickInterval, DamageType.Magic, ElementalType.Fire, source, true, damageTags);
            }
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        Destroy(gameObject);
    }
}
