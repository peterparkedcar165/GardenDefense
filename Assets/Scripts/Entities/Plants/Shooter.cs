using UnityEngine;
using System.Collections.Generic;

public enum TARGETING
{
    Nearest,
    First,
    Last
}

public abstract class Shooter : Plant
{
    public GameObject projectilePrefab;
    // base and final
    public float baseProjectileSpeed, projectileSpeed, baseMaxRange, maxRange;
    public int basePiercing, piercing;
    // bonuses
    protected float projectileSpeedAdder, projectileSpeedMultiplier, maxRangeAdder, maxRangeMultiplier; 
    public int piercingAdder;


    public TARGETING targeting = TARGETING.First;

    public override void UpdateStats()
    {
        base.UpdateStats();
        projectileSpeed = baseProjectileSpeed + projectileSpeedAdder + (baseProjectileSpeed * projectileSpeedMultiplier);
        maxRange = baseMaxRange + maxRangeAdder + (baseMaxRange * maxRangeMultiplier);
        piercing = basePiercing + piercingAdder;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();

        attackCooldown = 1 / attackSpeed;

        if (attackCooldownTimer < attackCooldown)
        {
            attackCooldownTimer += Time.deltaTime;
        }
        else
        {
            GameObject target = FindTarget();
            if (target != null)
            {
                attackCooldownTimer = 0; // RESET TIMER
                Vector3 predictedPosition = PredictTargetPosition(target);
                Shoot(predictedPosition);
            }
        }

    }

    protected abstract void Shoot(Vector3 target);


// constantly looks in the map for anybody with the tag Insect
// puts them into an array
// looks through each, search for nearest
// takes nearest, sets it to insect. target locked
    protected virtual GameObject FindTarget()
    {
        switch (targeting)
        {
            case TARGETING.First:
            return FindFirst(Insect.allInsects);
            case TARGETING.Nearest: 
            return FindNearest(Insect.allInsects);
            case TARGETING.Last:
            return FindLast(Insect.allInsects);
            default:                
            return null;
        }
    }

    protected GameObject FindNearest(List<Insect> insects)
    {
        GameObject nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance <= attackRange && distance < nearestDistance && IsValidNightTarget(insect, distance))
            {
                nearestDistance = distance;
                nearest = insect.gameObject;
            }
        }
        return nearest;
    }

    protected GameObject FindFirst(List<Insect> insects)
    {
        GameObject furthest = null;
        int highestWaypointIndex = -1;
        float closestDistanceToNextWaypoint = Mathf.Infinity;

        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance > attackRange) continue;
            if (!IsValidNightTarget(insect, distance)) continue;

            Transform waypoint = insect.GetCurrentWaypoint();
            if (waypoint == null) continue;

            if (insect.currentWaypointIndex > highestWaypointIndex)
            {
                highestWaypointIndex = insect.currentWaypointIndex;
                closestDistanceToNextWaypoint = Vector3.Distance(insect.transform.position, waypoint.position);
                furthest = insect.gameObject;
            }
            else if (insect.currentWaypointIndex == highestWaypointIndex)
            {
                float distanceToNext = Vector3.Distance(insect.transform.position, waypoint.position);
                if (distanceToNext < closestDistanceToNextWaypoint)
                {
                    closestDistanceToNextWaypoint = distanceToNext;
                    furthest = insect.gameObject;
                }
            }
        }
        return furthest;
    }

    protected GameObject FindLast(List<Insect> insects)
    {
        GameObject last = null;
        int lowestWaypointIndex = int.MaxValue;
        float furthestDistanceToNext = -1f;

        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance > attackRange) continue;
            if (!IsValidNightTarget(insect, distance)) continue;

            Transform waypoint = insect.GetCurrentWaypoint();
            if (waypoint == null) continue;

            if (insect.currentWaypointIndex < lowestWaypointIndex)
            {
                lowestWaypointIndex = insect.currentWaypointIndex;
                furthestDistanceToNext = Vector3.Distance(insect.transform.position, waypoint.position);
                last = insect.gameObject;
            }
            else if (insect.currentWaypointIndex == lowestWaypointIndex)
            {
                float distanceToNext = Vector3.Distance(insect.transform.position, waypoint.position);
                if (distanceToNext > furthestDistanceToNext)
                {
                    furthestDistanceToNext = distanceToNext;
                    last = insect.gameObject;
                }
            }
        }
        return last;
    }



    // movement prediction for shooter

    protected virtual Vector3 PredictTargetPosition(GameObject target)
    {
        Insect insect = target.GetComponent<Insect>();

        if (insect == null)
        return target.transform.position;

        Vector3 aimPos = insect.GetAimPoint();
        float distance = Vector3.Distance(transform.position, aimPos);
        float travelTime = distance / projectileSpeed;
        Vector3 predictedPosition = aimPos + insect.GetVelocity() * 0.75f * travelTime;

        return predictedPosition;
    }
    
}
