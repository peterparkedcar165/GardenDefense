using UnityEngine;

public class LeafRangerProjectile : Projectile
{
    // True Flight (skill tree): +3% damage per unit of distance already traveled when it hits
    private const float TrueFlightBonusPerUnit = 0.03f;

    // bounce chain, same feel as NeriumOleanderProjectile - minus its sprout mechanic, and minus
    // its Path1-max "can rebounce onto an already-hit target" exception: this one never revisits
    // an insect it's already hit, no matter what (enforced for free - it only ever searches
    // among insects _alreadyHit doesn't contain). "piercing" is reused directly as the bounce
    // counter (how many extra targets beyond the first it can still redirect to). detection
    // itself goes through the base class's normal Base+Visual dual check (HandleInsectHits(),
    // UseAutomaticInsectHits stays true/default) rather than Collider2D/OnTriggerEnter2D, same as
    // every other projectile - only what happens after a hit (OnAfterHit) differs
    private const float BounceHitPause = 0.05f;
    private float pauseTimer = 0f;
    private bool awaitingRetarget = false;
    private bool hasBounced = false;
    private float bounceRadius = 2f;
    private float bounceSpeedMultiplier = 0.66f;

    // both set by LeafRanger right after Initialize(), straight from LeafRangerData - how far a
    // bounce can seek its next target (scaled by Passive level rather than reusing maxRange), and
    // how much the arrow slows down from the first bounce onward
    public void SetBounceRadius(float radius) => bounceRadius = radius;
    public void SetBounceSpeedMultiplier(float multiplier) => bounceSpeedMultiplier = multiplier;

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Move()
    {
        // freezes the arrow in place at the point of impact; the actual retarget (and the
        // destroy-if-nothing-found fallback) happens once the pause elapses, same as Oleander.
        // base.Move() (and therefore HandleInsectHits()) simply doesn't run while paused, so no
        // new hit can register mid-freeze
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f && awaitingRetarget)
            {
                awaitingRetarget = false;
                if (!RetargetNextBounce())
                {
                    OnBeforeDestroy();
                    Destroy(gameObject);
                }
            }
            return;
        }

        base.Move();

        // sprite art faces left by default (angle 180), same convention as SunflowerProjectile/
        // SnowdropProjectile - the rotation needed to point it along the current heading is the
        // heading's own angle minus that 180 offset. only Visual rotates - Base stays unrotated
        // so Shadow (also a child of Base) doesn't spin along with it. reads VisualHeading (not
        // "direction") so the sprite tilts up/down while climbing or diving toward a flying
        // target instead of staying flat
        Vector3 heading = VisualHeading;
        float angle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
        visual.rotation = Quaternion.Euler(0f, 0f, angle - 180f);
    }

    protected override bool DisablePierceFalloff =>
        source is LeafRanger lr && SkillTreeManager.HasUnlock(lr, LeafRanger.TrueFlightUnlock);

    protected override void OnHit(Insect insect)
    {
        float damage = projectileDamage;
        if (DisablePierceFalloff)
        {
            float distanceTraveled = Vector3.Distance(spawnPosition, transform.position);
            damage *= 1f + TrueFlightBonusPerUnit * distanceTraveled;
        }
        insect.Damage(damage, damageType, elementalType, source, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
        PlaySound(hit);
    }

    // called after every hit (not just once piercing/bounces run out) - redirects toward the
    // nearest un-hit target as long as any bounces remain, instead of the base class's default
    // "just keep flying straight until piercing runs out, then destroy"
    protected override void OnAfterHit(Insect insect)
    {
        if (piercing > 0)
        {
            piercing--;
            // slows to bounceSpeedMultiplier of the ORIGINAL speed starting on the first bounce -
            // applied once (not re-multiplied on every later bounce too, which would compound
            // into a crawl)
            if (!hasBounced)
            {
                hasBounced = true;
                projectileSpeed *= bounceSpeedMultiplier;
            }
            pauseTimer = BounceHitPause;
            awaitingRetarget = true;
            return;
        }
        OnBeforeDestroy();
        Destroy(gameObject);
    }

    // nearest live, un-hit, hostile insect within bounceRadius - returns false (nothing
    // redirected) once none remain, so the caller destroys the arrow instead
    private bool RetargetNextBounce()
    {
        Insect nearest = null;
        float nearestDist = bounceRadius;
        foreach (Insect i in Insect.allInsects)
        {
            if (i == null || !i.IsAlive || i.team == Team.Friendly) continue;
            if (_alreadyHit.Contains(i)) continue;

            // bounceRadius is the hypotenuse of a right triangle whose two legs are the
            // ground-plane distance to this candidate and however high it's currently flying -
            // NOT a plain Vector3.Distance to its elevated GetAimPoint(), which would fold
            // height straight into the same axis ground-plane lane position already uses
            // (wrongly treating "2 lanes over" and "2 units up" as the same kind of distance)
            float groundDist = Vector3.Distance(transform.position, i.transform.position);
            float height = i.GetAimPoint().y - i.transform.position.y;
            float dist = Mathf.Sqrt(groundDist * groundDist + height * height);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = i;
            }
        }
        if (nearest == null) return false;

        spawnPosition = transform.position;
        trackedTarget = nearest.gameObject;
        trackedInsect = nearest;
        direction = (nearest.GetAimPoint() - transform.position).normalized;
        return true;
    }
}
