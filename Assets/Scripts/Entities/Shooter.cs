using System;
using UnityEngine;

public abstract class Shooter : Plant
{
    public GameObject projectilePrefab;
    public float projectileSpeed;

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
                Shoot(target);
            }
        }

    }

    public void Shoot(GameObject target)
    {
        attackCooldownTimer = 0; // RESET TIMER
        Debug.Log("Shoot: ");
    }

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
        Debug.Log("Nearest is: " + nearest);
        return nearest;
    }

    
}
