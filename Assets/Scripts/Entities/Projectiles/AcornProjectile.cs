using UnityEngine;
using System.Collections.Generic;

public class AcornProjectile : Projectile
{
    private int _bouncesRemaining;
    private bool _spent;
    private readonly List<Insect> _alreadyHit = new List<Insect>();
    private const float bounceSearchRadius = 3f;
    private const float bounceDamageReduction = 0.1f;

    public void SetBounces(int count) { _bouncesRemaining = count; }

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (_spent) return;

        if (other.CompareTag("Insect"))
        {
            Insect insect = other.GetComponentInParent<Insect>();
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly || _alreadyHit.Contains(insect)) return;

            _alreadyHit.Add(insect);
            OnHit(insect);

            trackedTarget = null;
            trackedInsect = null;

            if (_bouncesRemaining > 0)
            {
                _bouncesRemaining--;
                Insect next = FindNextBounceTarget();
                if (next != null)
                {
                    spawnPosition = transform.position;
                    trackedTarget = next.gameObject;
                    trackedInsect = next;
                    direction = (next.GetAimPoint() - transform.position).normalized;
                    return;
                }
            }

            _spent = true;
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
            if (i == null || !i.IsAlive || _alreadyHit.Contains(i)) continue;
            float dist = Vector3.Distance(transform.position, i.GetAimPoint());
            if (dist < nearestDist) { nearestDist = dist; nearest = i; }
        }
        return nearest;
    }

    protected override void OnHit(Insect insect)
    {
        PlaySound(hit);

        int bouncesDone = _alreadyHit.Count - 1;
        float effectiveDamage = projectileDamage * Mathf.Max(0f, 1f - bouncesDone * bounceDamageReduction);
        insect.Damage(effectiveDamage, damageType, elementalType, source, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });

        if (source != null && source is AcornSprout acorn)
        {
            float procChance = acorn.stunChance * (1 + acorn.bonusEffectChance);
            if (Random.value < procChance)
                insect.ApplyEffect(new StunEffect(insect, acorn.passiveDuration, 1, source));
        }
    }
}
