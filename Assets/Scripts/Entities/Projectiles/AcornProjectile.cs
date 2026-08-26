using UnityEngine;
using System.Collections.Generic;

public class AcornProjectile : Projectile
{
    private int _bouncesRemaining;
    private bool _spent;
    private readonly List<Insect> _alreadyHit = new List<Insect>();
    private const float bounceSearchRadius = 3f;
    private const float bounceDamageReduction = 0.1f;

    // brief freeze on every bounce hit before jumping to the next target, matching the
    // Oleander's bounce pacing
    private const float BounceHitPause = 0.05f;
    private float pauseTimer = 0f;
    private bool awaitingRetarget = false;

    public void SetBounces(int count) { _bouncesRemaining = count; }

    // below max level, Piercing behaves like any other shooter's piercing (straight-line pass
    // through, base class handles it). only once Path2 is maxed does Piercing instead bounce
    // to nearby targets, handled entirely by this class
    private bool Maxed => source is AcornSprout acorn && acorn.IsPath2Maxed;

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

        if (!Maxed)
        {
            base.OnTriggerEnter2D(other);
            return;
        }

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
                pauseTimer = BounceHitPause;
                awaitingRetarget = true;
                return;
            }

            _spent = true;
            Destroy(gameObject);
        }

        if (other.gameObject.CompareTag("Border"))
            Destroy(gameObject);
    }

    protected override void Move()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f && awaitingRetarget)
            {
                awaitingRetarget = false;
                Insect next = FindNextBounceTarget();
                if (next != null)
                {
                    spawnPosition = transform.position;
                    trackedTarget = next.gameObject;
                    trackedInsect = next;
                    direction = (next.GetAimPoint() - transform.position).normalized;
                }
                else
                {
                    _spent = true;
                    Destroy(gameObject);
                }
            }
            return;
        }
        base.Move();
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

        // bounce damage falloff only applies in bounce mode; in piercing mode the base class
        // already halves projectileDamage itself from the second hit onward
        float effectiveDamage = projectileDamage;
        if (Maxed)
        {
            int bouncesDone = _alreadyHit.Count - 1;
            effectiveDamage = projectileDamage * Mathf.Max(0f, 1f - bouncesDone * bounceDamageReduction);
        }
        insect.Damage(effectiveDamage, damageType, elementalType, source, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });

        if (source != null && source is AcornSprout acorn)
        {
            float procChance = acorn.stunChance * (1 + acorn.bonusEffectChance);
            if (Random.value < procChance)
                insect.ApplyEffect(new StunEffect(insect, acorn.passiveDuration, 1, source));
        }
    }
}
