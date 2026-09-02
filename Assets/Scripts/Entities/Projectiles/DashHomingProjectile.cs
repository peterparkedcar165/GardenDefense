using System.Collections.Generic;
using UnityEngine;

// shared movement for OldCarrot's attack and Psionic OldCarrot (the retired kit kept as a
// placeholder - current Carrot doesn't use a projectile at all). homes in on a target and stays
// locked onto it until it dies (only then does it look for the next nearest valid insect).
// once close enough it commits to a straight dash through the target; on each hit, piercing
// means "extra return passes on the SAME target" rather than "how many different targets to
// pass through": 0 = a single hit then destroy, higher values keep re-engaging (up to
// piercing+1 total hits) before finally destroying.
//
// each re-engagement is a coast-and-return, not an arc: after a hit it keeps drifting forward
// in a straight line while decelerating to a near-stop (a small overshoot), then accelerates
// back toward the target (re-aiming as it goes, since the target may have moved) for another
// dash-through. hits are registered by DISTANCE to the target rather than Unity's trigger
// collider enter/exit: relying on the collider meant a short overshoot could decelerate and
// reverse without the projectile ever actually exiting the target's collider, so it never
// generated a fresh OnTriggerEnter2D and the "return" pass silently dealt no damage. a plain
// distance check every frame has no such failure mode
public abstract class DashHomingProjectile : Projectile
{
    protected virtual float DashTriggerRange   => 1f;
    protected virtual float HitRadius          => 0.3f;
    protected virtual float DashSpeed          => projectileSpeed * 2f;
    protected virtual float RotationSpeed      => 1080f; // degrees/sec the sprite turns to face travel direction
    protected virtual float OvershootDecelTime => 0.5f;
    protected virtual float ReturnAccelTime    => 0.15f;
    // no radius cap: once a projectile loses its target, it should be able to pick up the
    // nearest valid insect anywhere on the field, not just nearby
    protected virtual float ReacquireRadius    => Mathf.Infinity;
    // losing a target never brings it to a full stop: it eases down to this floor speed over
    // at least SeekDecelDuration, then keeps cruising forward at that floor speed (still
    // searching every frame) until something valid is found or it flies out of the tile grid
    protected virtual float SeekDecelDuration  => 0.25f;
    protected virtual float SeekMinSpeed       => projectileSpeed * 0.5f;

    private enum DashState { Approaching, Dashing, Decelerating, Returning, Seeking }
    private DashState state = DashState.Approaching;

    private Insect currentTarget;
    private int passesCompleted;
    private float currentSpeed;
    private float seekStartSpeed;
    private readonly HashSet<Insect> incidentalHits = new HashSet<Insect>();

    protected override void Move()
    {
        // lazily seed from whatever SetTarget assigned, the first time Move() runs
        if (currentTarget == null && trackedInsect != null && trackedInsect.IsAlive)
            currentTarget = trackedInsect;

        // lost its target: ease down towards SeekMinSpeed rather than instantly snapping onto a
        // new one, but never fully stop - it keeps flying and searching the whole time.
        // passesCompleted is left untouched here - it's a lifetime total, not reset per-target,
        // so piercing stays a fixed budget of piercing+1 hits for the whole flight even if it's
        // split across several targets, and the projectile is still guaranteed to eventually
        // destroy itself (either by exhausting that budget or flying out of the tile grid)
        bool targetInvalid = currentTarget == null || !currentTarget.IsAlive || currentTarget.team == Team.Friendly;
        if (targetInvalid && state != DashState.Seeking)
        {
            float enterSpeed = state switch
            {
                DashState.Dashing => DashSpeed,
                DashState.Decelerating => currentSpeed,
                DashState.Returning => currentSpeed,
                _ => projectileSpeed,
            };
            currentTarget = null;
            currentSpeed = enterSpeed;
            seekStartSpeed = Mathf.Max(enterSpeed, SeekMinSpeed);
            state = DashState.Seeking;
        }

        switch (state)
        {
            case DashState.Approaching:  MoveApproaching(); break;
            case DashState.Dashing:      MoveDashing(); break;
            case DashState.Decelerating: MoveDecelerating(); break;
            case DashState.Returning:    MoveReturning(); break;
            case DashState.Seeking:      MoveSeeking(); break;
        }

        // sprite is authored pointing right (tip = 0 degrees), so aligning its rotation to the
        // current travel direction keeps the tip leading through every approach/dash/turn.
        // rotates towards the target angle at a fast but finite rate rather than snapping, so a
        // ~180 degree flip (e.g. the moment it starts returning to the target) swings around
        // visibly instead of instantly flipping in place
        if (direction.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, RotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }

        // same despawn rule every other projectile in the game uses now: gone once it leaves
        // the playable tile grid, regardless of how much back-and-forth dashing it took to get there
        if (!Tile.IsInsideGrid(transform.position))
            Destroy(gameObject);
    }

    private Insect FindNearestValidTarget()
    {
        Insect nearest = null;
        float nearestDist = ReacquireRadius;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly) continue;
            float dist = Vector3.Distance(transform.position, insect.GetAimPoint());
            if (dist < nearestDist) { nearestDist = dist; nearest = insect; }
        }
        return nearest;
    }

    private void MoveApproaching()
    {
        if (currentTarget == null)
        {
            transform.position += direction * projectileSpeed * Time.deltaTime;
            return;
        }

        Vector3 aimPos = currentTarget.GetAimPoint();
        direction = (aimPos - transform.position).normalized;
        transform.position += direction * projectileSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, aimPos) <= DashTriggerRange)
            state = DashState.Dashing;
    }

    private void MoveDashing()
    {
        transform.position += direction * DashSpeed * Time.deltaTime;
        CheckForHit();
    }

    // the post-hit overshoot: straight line along the current direction, ramping speed down to
    // zero over OvershootDecelTime. returns true once fully stopped. (MoveSeeking, losing a
    // target entirely, uses its own SeekMinSpeed-floored deceleration instead - it never stops)
    private bool DecelerateToStop()
    {
        float decelRate = DashSpeed / Mathf.Max(0.01f, OvershootDecelTime);
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decelRate * Time.deltaTime);
        transform.position += direction * currentSpeed * Time.deltaTime;
        return currentSpeed <= 0f;
    }

    // the "little overshoot" after a hit
    private void MoveDecelerating()
    {
        if (DecelerateToStop())
            state = DashState.Returning;
    }

    // eases from whatever speed it lost its target at down to SeekMinSpeed over
    // SeekDecelDuration, in a straight line, and never goes any slower than that floor - it
    // keeps cruising and searching every frame until something valid turns up (snapping
    // straight to Approaching) or it flies out of the tile grid
    private void MoveSeeking()
    {
        if (currentSpeed > SeekMinSpeed)
        {
            float decelRate = (seekStartSpeed - SeekMinSpeed) / Mathf.Max(0.01f, SeekDecelDuration);
            currentSpeed = Mathf.Max(SeekMinSpeed, currentSpeed - decelRate * Time.deltaTime);
        }
        transform.position += direction * currentSpeed * Time.deltaTime;

        Insect found = FindNearestValidTarget();
        if (found == null) return;

        currentTarget = found;
        state = DashState.Approaching;

        // OldCarrot's Path1 max (the retired kit kept as a placeholder - current Carrot no
        // longer uses this class at all): switching onto a genuinely new target grants extra
        // hits, since the fresh target starts with no Psionic Mark stacks built up against it yet
        if (source is OldCarrot oldCarrot && oldCarrot.IsPath1Maxed)
            passesCompleted = Mathf.Max(0, passesCompleted - oldCarrot.TargetSwitchBonusHits);
    }

    // accelerates back from a standstill toward the target, re-aiming each frame in case it
    // moved while we were overshooting, until it's close enough to hit it again
    private void MoveReturning()
    {
        if (currentTarget != null)
            direction = (currentTarget.GetAimPoint() - transform.position).normalized;

        float accelRate = DashSpeed / Mathf.Max(0.01f, ReturnAccelTime);
        currentSpeed = Mathf.MoveTowards(currentSpeed, DashSpeed, accelRate * Time.deltaTime);
        transform.position += direction * currentSpeed * Time.deltaTime;
        CheckForHit();
    }

    private void CheckForHit()
    {
        if (currentTarget == null) return;
        if (Vector3.Distance(transform.position, currentTarget.GetAimPoint()) > HitRadius) return;

        OnHit(currentTarget);
        passesCompleted++;

        if (passesCompleted > piercing)
        {
            Destroy(gameObject);
            return;
        }

        // keep coasting forward through/past the target, decelerating to a stop, before
        // accelerating back for the next pass
        currentSpeed = DashSpeed;
        state = DashState.Decelerating;
    }

    // any insect that physically overlaps the projectile in flight gets hit too, even if it
    // isn't the locked-on target - it just doesn't consume piercing or count toward passesCompleted,
    // since that budget belongs entirely to the engagement with currentTarget. each incidental
    // insect is only ever hit once (tracked here), since the projectile may cross paths with the
    // same one again during a later overshoot/return
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Insect"))
        {
            Insect insect = other.GetComponentInParent<Insect>();
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly) return;
            if (insect == currentTarget || incidentalHits.Contains(insect)) return;

            incidentalHits.Add(insect);
            OnHit(insect);
        }

        if (other.gameObject.CompareTag("Border"))
            Destroy(gameObject);
    }
}
