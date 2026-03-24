using System;
using UnityEngine;

public abstract class Shooter : Plant
{
    public GameObject projectilePrefab;
    public float projectileSpeed, maxRange;
    public int piercing;

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
        GameObject nearest = null;

        float nearestDistance = Mathf.Infinity;

        foreach (GameObject insect in insects)
        {
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance <= attackRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = insect;
            }
        }
        return nearest;
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
        Vector3 predictedPosition = target.transform.position + insect.GetVelocity() * travelTime;
        
        return predictedPosition;
    }
    
}
