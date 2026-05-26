using UnityEngine;
using System.Collections.Generic;

public class NeriumOleanderProjectile : Projectile
{
    private int bouncesRemaining;
    private int bouncesHit = 0;
    private float bounceDamageReduction = 0.1f;
    private float toxinDuration;
    private int toxinLevel;
    private float bounceSearchRadius = 6f;
    private List<Insect> alreadyHit = new List<Insect>();

    public void SetBounceData(int bounces, float toxinDuration, int toxinLevel, float bounceSearchRadius, float bounceDamageReduction)
    {
        this.bouncesRemaining = bounces;
        this.toxinDuration = toxinDuration;
        this.toxinLevel = toxinLevel;
        this.bounceSearchRadius = bounceSearchRadius;
        this.bounceDamageReduction = bounceDamageReduction;
    }

    protected override void OnHit(Insect insect)
    {
        alreadyHit.Add(insect);
        float effectiveDamage = projectileDamage * Mathf.Max(0f, 1f - bouncesHit * bounceDamageReduction);
        bouncesHit++;
        insect.Damage(effectiveDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
        ApplyToxin(insect);
    }

    private void ApplyToxin(Insect insect)
    {
        if (source == null) return;
        OleandicToxinEffect existing = insect.GetEffect<OleandicToxinEffect>();
        if (existing != null)
            existing.RefreshAndStack(source);
        else
            insect.ApplyEffect(new OleandicToxinEffect(insect, toxinDuration, toxinLevel, source));
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Insect"))
        {
            Insect insect = other.GetComponentInParent<Insect>();
            if (insect == null || !insect.IsAlive || alreadyHit.Contains(insect)) return;

            OnHit(insect);
            source?.GetEffect<FloralGlowEffect>()?.OnProjectileHit(insect);
            trackedTarget = null;
            trackedInsect = null;

            if (bouncesRemaining > 0)
            {
                bouncesRemaining--;
                Insect next = FindNextBounceTarget();
                if (next != null)
                {
                    // reset travel origin so maxRange applies per-bounce, not total
                    spawnPosition = transform.position;
                    trackedTarget = next.gameObject;
                    trackedInsect = next;
                    direction = (next.GetAimPoint() - transform.position).normalized;
                    return;
                }
            }
            Destroy(gameObject);
        }

        if (other.gameObject.CompareTag("Border"))
            Destroy(gameObject);
    }

    private Insect FindNextBounceTarget()
    {
        Insect nearest = null;
        float nearestDist = bounceSearchRadius;
        foreach (Insect i in Insect.allInsects)
        {
            if (i == null || !i.IsAlive || alreadyHit.Contains(i)) continue;
            float dist = Vector3.Distance(transform.position, i.GetAimPoint());
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = i;
            }
        }
        return nearest;
    }
}
