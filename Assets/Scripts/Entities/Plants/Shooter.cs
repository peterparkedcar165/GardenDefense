using UnityEngine;
using System.Collections.Generic;

public enum TARGETING
{
    Nearest,
    First
}

public abstract class Shooter : Plant
{
    public GameObject projectilePrefab;
    // base and final
    public float baseProjectileSpeed, projectileSpeed, baseMaxRange, maxRange;
    public int basePiercing, piercing;
    // bonuses
    protected float projectileSpeedAdder, projectileSpeedMultiplier, maxRangeAdder, maxRangeMultiplier; 
    protected int piercingAdder;


    public TARGETING targeting = TARGETING.First;

    protected override void UpdateStats()
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
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance <= attackRange && distance < nearestDistance)
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
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance > attackRange) continue;

            if (insect.currentWaypointIndex > highestWaypointIndex)
            {
                highestWaypointIndex = insect.currentWaypointIndex;
                closestDistanceToNextWaypoint = Vector3.Distance(insect.transform.position, insect.GetCurrentWaypoint().position);
                furthest = insect.gameObject;
            }
            else if (insect.currentWaypointIndex == highestWaypointIndex)
            {
                float distanceToNext = Vector3.Distance(insect.transform.position, insect.GetCurrentWaypoint().position);
                if (distanceToNext < closestDistanceToNextWaypoint)
                {
                    closestDistanceToNextWaypoint = distanceToNext;
                    furthest = insect.gameObject;
                }
            }
        }
        return furthest;
    }


    // movement prediction for shooter

    protected virtual Vector3 PredictTargetPosition(GameObject target)
    {
        Insect insect = target.GetComponent<Insect>();
        
        if (insect == null)
        return target.transform.position; // fallback to current position

        // how far the target is away from the shooter
        float distance = Vector3.Distance(transform.position, target.transform.position);

        // how long the projectile will take to reach that distance
        float travelTime = distance / projectileSpeed;

        // where the target be after that time
        // insect moves in its current direction at its current speed
        Vector3 predictedPosition = target.transform.position + insect.GetVelocity() * 0.75f *travelTime;
        
        return predictedPosition;
    }
    
}
