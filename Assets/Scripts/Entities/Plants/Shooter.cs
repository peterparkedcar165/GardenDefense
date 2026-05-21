using UnityEngine;
using System.Collections.Generic;

public abstract class Shooter : Plant
{
    public override bool UsesTargeting => true;
    public GameObject projectilePrefab;
    // base and final
    public float baseProjectileSpeed, projectileSpeed, baseMaxRange, maxRange;
    public int basePiercing, piercing;
    // bonuses
    protected float projectileSpeedAdder, projectileSpeedMultiplier, maxRangeAdder, maxRangeMultiplier;
    public int piercingAdder;

    public override void UpdateStats()
    {
        base.UpdateStats();
        projectileSpeed = baseProjectileSpeed + projectileSpeedAdder + (baseProjectileSpeed * projectileSpeedMultiplier);
        maxRange = Mathf.Max(baseMaxRange + maxRangeAdder + (baseMaxRange * maxRangeMultiplier), attackRange);
        piercing = basePiercing + piercingAdder;
    }

    protected override void Awake()
    {
        base.Awake();
        if (data != null)
        {
            baseProjectileSpeed = data.baseProjectileSpeed;
            baseMaxRange        = data.baseMaxRange;
            basePiercing        = data.basePiercing;
        }
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
                attackCooldownTimer = 0;
                Vector3 predictedPosition = PredictTargetPosition(target);
                Shoot(predictedPosition);
            }
        }

    }

    protected abstract void Shoot(Vector3 target);

    protected virtual GameObject FindTarget()
    {
        switch (targeting)
        {
            case TARGETING.First:   return FindFirst(Insect.allInsects);
            case TARGETING.Nearest: return FindNearest(Insect.allInsects);
            case TARGETING.Last:    return FindLast(Insect.allInsects);
            default:                return null;
        }
    }

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
