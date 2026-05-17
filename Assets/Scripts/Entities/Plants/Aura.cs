using UnityEngine;
using System.Collections.Generic;

public abstract class Aura : Plant
{
    public override void UpdateStats()
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
        

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float distance = Vector2.Distance(transform.position, insect.transform.position);
            if (distance <= attackRange && IsValidNightTarget(insect, distance))
                result.Add(insect);
        }
        return result;
    }
}
