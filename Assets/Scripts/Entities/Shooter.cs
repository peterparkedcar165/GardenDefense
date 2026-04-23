using UnityEngine;

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
        GameObject[] insects = GameObject.FindGameObjectsWithTag("Insect");

            switch (targeting)
            {
                case TARGETING.First:
                return FindFirst(insects);
                case TARGETING.Nearest:
                return FindNearest(insects);
                default:
                return null;
            }
    }

// amongst all insects in the math, takes distance, between them and the plant itself.
// then if within range, and if distance between the plant and the insect is smaller than
// any other insect that have gotten the "nearest" will be overriden
// return the nearest to the method caller
    private GameObject FindNearest(GameObject[] insects)
    {
        GameObject nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject insectObject in insects)
        {
            float distance = Vector3.Distance(transform.position, insectObject.transform.position);
            if (distance <= attackRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = insectObject;
            }
        }
        return nearest;
    }

    private GameObject FindFirst(GameObject[] insects)
    {
        GameObject furthest = null;
        int highestWaypointIndex = -1;
        float closestDistanceToNextWaypoint = Mathf.Infinity;

        foreach (GameObject insectObject in insects)
        {
            float distance = Vector3.Distance(transform.position, insectObject.transform.position);
            if (distance > attackRange) continue;

            Insect insect = insectObject.GetComponent<Insect>();
            if (insect == null) continue;

            if (insect.currentWaypointIndex > highestWaypointIndex)
            {
                highestWaypointIndex = insect.currentWaypointIndex;
                closestDistanceToNextWaypoint = Vector3.Distance(insectObject.transform.position, insect.GetCurrentWaypoint().position);
                furthest = insectObject;
            }
            else if (insect.currentWaypointIndex == highestWaypointIndex)
            {
                float distanceToNext = Vector3.Distance(insectObject.transform.position, insect.GetCurrentWaypoint().position);
                if (distanceToNext < closestDistanceToNextWaypoint)
                {
                    closestDistanceToNextWaypoint = distanceToNext;
                    furthest = insectObject;
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
        Vector3 predictedPosition = target.transform.position + insect.GetVelocity() * 0.5f *travelTime;
        
        return predictedPosition;
    }
    
}
