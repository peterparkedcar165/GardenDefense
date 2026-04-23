using UnityEngine;
using System.Collections.Generic;

public abstract class Aura : Plant
{
    protected override void UpdateStats()
    {
        base.UpdateStats();
        
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();

        attackCooldown = 1 / attackSpeed;

    }
    
    protected virtual void Attack()
    {
        attackCooldownTimer = 0; // reset attack timer
    }

    protected List<Insect> GetInsectsInRange()
    {
        List<Insect> result = new List<Insect>();
        GameObject[] insects = GameObject.FindGameObjectsWithTag("Insect");

        foreach (GameObject obj in insects)
        {
            if (Vector2.Distance(transform.position, obj.transform.position) <= attackRange)
            {
                Insect insect = obj.GetComponent<Insect>();
                if (insect != null)
                {
                    result.Add(insect);
                }
            }
        }
        return result;
    }
}
