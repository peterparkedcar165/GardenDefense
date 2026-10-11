using UnityEngine;
using System.Collections.Generic;

// Snowdrop's Ice Beam skill - a permanent child of her prefab, not something spawned/destroyed
// per shot. Snowdrop toggles it active/inactive based on whether she currently has the
// IceBeamEffect status and a target, and while active calls UpdateBeam every frame so it
// continuously rotates to track her current target and keeps its length up to date; actual
// damage is only dealt when Snowdrop's own attack cooldown fires (DealDamage), using whatever
// the beam is touching at that exact moment - so DPS still matches her attack speed, even
// though the visual itself is continuous
public class IceBeam : MonoBehaviour
{
    private static readonly DamageTag[] attackTags = { DamageTag.Attack, DamageTag.SingleTarget, DamageTag.SkillDamage };

    [SerializeField] private SpriteRenderer beamSprite;
    [SerializeField] private float beamWidth = 0.5f;
    [SerializeField] private float turnSpeedDegreesPerSecond = 720f;

    private readonly List<Insect> _currentHits = new List<Insect>();

    // rotates smoothly toward targetPosition, re-scans along the (possibly still turning)
    // facing direction for every hostile insect within beamWidth, and resizes the visual to
    // stop at the first one in the way (no piercing - the beam always stops on its first
    // target), or reach maxRange/the target otherwise. call every frame while the beam is active
    // snaps rotation instantly instead of smoothly turning - call once, the moment the beam
    // transitions from inactive to active, so it doesn't visibly sweep in from whatever stale
    // direction it was left facing the last time it was active
    public void SnapToTarget(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - transform.position;
        if (toTarget.sqrMagnitude <= 0.0001f) return;
        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void UpdateBeam(Vector3 targetPosition, float maxRange)
    {
        Vector3 origin = transform.position;
        Vector3 toTarget = targetPosition - origin;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            float desiredAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            Quaternion desiredRotation = Quaternion.Euler(0f, 0f, desiredAngle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, turnSpeedDegreesPerSecond * Time.deltaTime);
        }

        Vector3 direction = transform.right;
        float range = Mathf.Max(0f, maxRange);

        // every hostile insect within beamWidth of the beam's CURRENT facing line, ordered by
        // how far along it sits - may not be the same insects the beam was aimed at a moment
        // ago if it's still mid-turn, which is correct: it only hits what it's actually touching
        List<(Insect insect, float dist)> inPath = new List<(Insect, float)>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly) continue;
            // GetAimPoint(), not transform.position - a flying/levitating insect's visual sits
            // well above its logical root position, and the beam rotates to aim at that same
            // visual point (see Snowdrop.Update), so the hit-scan has to measure insects the
            // same way or it'll aim at one position and scan for hits around a different one
            Vector3 toInsect = insect.GetAimPoint() - origin;
            float along = Vector3.Dot(toInsect, direction);
            if (along < 0f || along > range) continue;
            float perp = (toInsect - direction * along).magnitude;
            if (perp > beamWidth * 0.5f) continue;
            inPath.Add((insect, along));
        }
        inPath.Sort((a, b) => a.dist.CompareTo(b.dist));

        // no piercing - the beam only ever hits the first thing in its path
        const int maxHits = 1;
        int hitCount = Mathf.Min(inPath.Count, maxHits);

        _currentHits.Clear();
        for (int i = 0; i < hitCount; i++)
            _currentHits.Add(inPath[i].insect);

        // stops short only if there were MORE insects on the line than the beam could pierce.
        // otherwise it reaches exactly as far as the farthest thing it's actually hitting - not
        // blindly out to maxRange, since maxRange can now be unlimited (Path2 max removes the
        // cap entirely) and a literal infinite-length sprite would break the visual
        bool stoppedShort = inPath.Count > maxHits;
        float beamLength;
        if (stoppedShort)
            beamLength = inPath[maxHits - 1].dist;
        else if (inPath.Count > 0)
            beamLength = inPath[inPath.Count - 1].dist;
        else
            beamLength = Mathf.Min(range, toTarget.magnitude);

        if (beamSprite != null)
        {
            Transform t = beamSprite.transform;
            t.localScale = new Vector3(beamLength, beamWidth, t.localScale.z);
            t.localPosition = new Vector3(beamLength * 0.5f, 0f, 0f);
        }
    }

    // damages whatever the beam is touching right now (as of the most recent UpdateBeam call) -
    // called once per actual attack, not every frame
    public void DealDamage(float damage, DamageType damageType, ElementalType elementalType, Snowdrop source)
    {
        foreach (Insect insect in _currentHits)
        {
            if (insect == null || !insect.IsAlive) continue;
            insect.Damage(damage, damageType, elementalType, source, true, attackTags);
            if (insect.IsAlive) source.OnIceAttackHit(insect);
        }
    }
}
