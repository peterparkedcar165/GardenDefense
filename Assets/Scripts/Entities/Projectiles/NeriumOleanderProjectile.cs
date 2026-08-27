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
    private HashSet<OleanderSprout> hitSprouts = new HashSet<OleanderSprout>();
    private bool hasGainedSproutBounceBonus = false;

    // brief freeze on every hit (insect or sprout) before the petal jumps to its next target
    private const float BounceHitPause = 0.05f;
    private float pauseTimer = 0f;
    private Insect pendingJustHit = null;
    private OleanderSprout pendingJustHitSprout = null;
    private bool awaitingRetarget = false;

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

        if (source is NeriumOleander oleander && oleander.IsPath1Maxed)
        {
            float extend = oleander.OleanderData?.path1MaxPoisonExtendPerHit ?? 1f;
            foreach (StatusEffect e in insect.activeEffects)
                if (e.elementalType == ElementalType.Poison && e.effectType == StatusEffect.Type.negative)
                    e.duration += extend;
        }
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
        OleanderSprout sprout = other.GetComponentInParent<OleanderSprout>();
        if (sprout != null)
        {
            HandleSproutHit(sprout);
            return;
        }

        if (other.CompareTag("Insect"))
        {
            Insect insect = other.GetComponentInParent<Insect>();
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly) return;

            bool canRebounce = source is NeriumOleander oleander2 && oleander2.IsPath1Maxed;
            if (!canRebounce && alreadyHit.Contains(insect)) return;

            OnHit(insect);

            trackedTarget = null;
            trackedInsect = null;

            if (bouncesRemaining > 0)
            {
                bouncesRemaining--;
                BeginBouncePause(insect);
                return;
            }
            Destroy(gameObject);
        }

        if (other.gameObject.CompareTag("Border"))
            Destroy(gameObject);
    }

    // hitting a sprout is free: it doesn't cost a bounce charge, and can be rebounced off of
    // repeatedly (lets the petal ping-pong between sprouts when no insects are around instead
    // of despawning). any max-leveled sprout (not just this petal's own source's) grants
    // +bounces, but only the very first sprout hit of the petal's whole flight ever grants it
    private void HandleSproutHit(OleanderSprout sprout)
    {
        hitSprouts.Add(sprout);

        if (!hasGainedSproutBounceBonus && sprout.owner != null && sprout.owner.IsPath3Maxed)
        {
            bouncesRemaining += sprout.owner.OleanderData?.path3MaxBounceBonus ?? 3;
            hasGainedSproutBounceBonus = true;
        }

        trackedTarget = null;
        trackedInsect = null;
        BeginBouncePause(null, sprout);
    }

    // freezes the petal in place at the point of impact; the actual retarget (and the
    // destroy-if-nothing-found fallback) happens once the pause elapses, in Move()
    private void BeginBouncePause(Insect justHitInsect, OleanderSprout justHitSprout = null)
    {
        pauseTimer = BounceHitPause;
        pendingJustHit = justHitInsect;
        pendingJustHitSprout = justHitSprout;
        awaitingRetarget = true;
    }

    protected override void Move()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f && awaitingRetarget)
            {
                awaitingRetarget = false;
                if (!RetargetNextBounce(pendingJustHit))
                    Destroy(gameObject);
                pendingJustHit = null;
                pendingJustHitSprout = null;
            }
            return;
        }
        base.Move();
    }

    // aims the petal at its next waypoint: an un-touched, Path3-max-owned sprout takes
    // priority (smart targeting for the bonus bounces), otherwise the nearest valid insect.
    // if nothing is left to fight, falls back to any sprout at all so the petal can keep
    // ping-ponging in place instead of despawning. returns false if nothing was found at all
    private bool RetargetNextBounce(Insect justHit)
    {
        OleanderSprout sprout = FindBounceableSprout();
        if (sprout != null)
        {
            spawnPosition = transform.position;
            trackedTarget = sprout.gameObject;
            trackedInsect = null;
            direction = ((Vector3)sprout.transform.position - transform.position).normalized;
            return true;
        }

        Insect next = FindNextBounceTarget(justHit);
        if (next != null)
        {
            spawnPosition = transform.position;
            trackedTarget = next.gameObject;
            trackedInsect = next;
            direction = (next.GetAimPoint() - transform.position).normalized;
            return true;
        }

        OleanderSprout anySprout = FindAnySproutInRange(pendingJustHitSprout);
        if (anySprout != null)
        {
            spawnPosition = transform.position;
            trackedTarget = anySprout.gameObject;
            trackedInsect = null;
            direction = ((Vector3)anySprout.transform.position - transform.position).normalized;
            return true;
        }

        return false;
    }

    // mid-flight bounce priority only favors a sprout over a fresh insect target when the
    // sprout's owner is maxed: that's the only case where routing through it actually pays off
    // (the bonus bounces). an unmaxed sprout can still be hit incidentally (free pass-through,
    // handled in OnTriggerEnter2D), it just isn't sought out over a live insect. once the bonus
    // has already been secured once, there's nothing left to gain from seeking one out, so this
    // stops entirely and lets insect targeting take priority as normal
    private OleanderSprout FindBounceableSprout()
    {
        if (hasGainedSproutBounceBonus) return null;
        foreach (OleanderSprout s in OleanderSprout.allSprouts)
        {
            if (s == null || hitSprouts.Contains(s)) continue;
            if (s.owner == null || !s.owner.IsPath3Maxed) continue;
            if (Vector3.Distance(transform.position, s.transform.position) <= bounceSearchRadius)
                return s;
        }
        return null;
    }

    // last-resort fallback once no insect or bonus-eligible sprout remains: any sprout at all
    // (touched or not, any owner) so the petal can keep ping-ponging instead of despawning.
    // excludes the sprout it's physically sitting on right now to avoid a zero-distance self-loop
    private OleanderSprout FindAnySproutInRange(OleanderSprout justHitSprout)
    {
        OleanderSprout nearest = null;
        float nearestDist = bounceSearchRadius;
        foreach (OleanderSprout s in OleanderSprout.allSprouts)
        {
            if (s == null || s == justHitSprout) continue;
            float dist = Vector3.Distance(transform.position, s.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = s;
            }
        }
        return nearest;
    }

    // at Path 1 max level, older targets become valid again, but only as a fallback once no
    // fresh (un-hit) target remains in range — the bounce-back never preempts a fresh target.
    // the one just left is always excluded, to avoid an immediate zero-distance self-loop back
    // onto the target the petal is still physically sitting on
    private Insect FindNextBounceTarget(Insect justHit)
    {
        bool canRebounce = source is NeriumOleander oleander && oleander.IsPath1Maxed;

        Insect fresh = FindNearestInsectInRange(justHit, excludeAlreadyHit: true);
        if (fresh != null) return fresh;

        return canRebounce ? FindNearestInsectInRange(justHit, excludeAlreadyHit: false) : null;
    }

    private Insect FindNearestInsectInRange(Insect justHit, bool excludeAlreadyHit)
    {
        Insect nearest = null;
        float nearestDist = bounceSearchRadius;
        foreach (Insect i in Insect.allInsects)
        {
            if (i == null || !i.IsAlive) continue;
            if (i == justHit) continue;
            if (excludeAlreadyHit && alreadyHit.Contains(i)) continue;
            float dist = Vector3.Distance(transform.position, i.GetAimPoint());
            if (!NeriumOleander.IsVisibleToChain(i)) continue;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = i;
            }
        }
        return nearest;
    }
}
