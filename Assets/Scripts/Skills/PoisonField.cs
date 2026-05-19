using UnityEngine;
using System.Collections.Generic;

public class PoisonField : MonoBehaviour
{
    private float radius;
    private float duration;
    private Plant source;

    private float damagePerSecond;
    private const float tickInterval = 0.5f;
    private float tickTimer = 0f;

    private readonly List<Insect> affectedInsects = new List<Insect>();

    private static readonly DamageTag[] damageTags = { DamageTag.AoE, DamageTag.SkillDamage, DamageTag.DoT };

    public void Initialize(float radius, float duration, Plant source, float damagePerSecond)
    {
        this.radius = radius;
        this.duration = duration;
        this.source = source;
        this.damagePerSecond = damagePerSecond;
        float s = radius * 2f;
        transform.localScale = new Vector3(s, s, 1f);
    }

    private void Update()
    {
        duration -= Time.deltaTime;
        if (duration <= 0f) { Die(); return; }

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            bool inside = Vector3.Distance(transform.position, insect.transform.position) <= radius;
            bool tracked = affectedInsects.Contains(insect);

            if (inside && !tracked)
            {
                affectedInsects.Add(insect);
                insect.debuffsFrozen = true;
            }
            else if (!inside && tracked)
            {
                affectedInsects.Remove(insect);
                insect.debuffsFrozen = false;
            }
        }

        for (int i = affectedInsects.Count - 1; i >= 0; i--)
        {
            if (affectedInsects[i] == null || affectedInsects[i].gameObject == null)
                affectedInsects.RemoveAt(i);
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            float tickDamage = damagePerSecond * tickInterval;
            for (int i = affectedInsects.Count - 1; i >= 0; i--)
            {
                if (affectedInsects[i] != null)
                    affectedInsects[i].Damage(tickDamage, DamageType.Magic, ElementalType.Poison, source, false, damageTags);
            }
        }
    }

    private void Die()
    {
        foreach (Insect insect in affectedInsects)
            if (insect != null) insect.debuffsFrozen = false;
        affectedInsects.Clear();
        Destroy(gameObject);
    }
}
