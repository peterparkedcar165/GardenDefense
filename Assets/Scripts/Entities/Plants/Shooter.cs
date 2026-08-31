using UnityEngine;
using System.Collections.Generic;

public abstract class Shooter : Plant
{
    public override bool UsesTargeting => true;
    public GameObject projectilePrefab;
    // base and final (projectileSpeed lives on Entity now)
    public float baseMaxRange, maxRange;
    public int basePiercing, piercing;
    // bonuses
    protected float maxRangeAdder, maxRangeMultiplier;
    public int piercingAdder;

    // fires every time this specific Shooter actually looses a shot (right alongside Shoot()/
    // OnShoot(), regardless of whether a subclass's OnShoot override calls base), carrying the
    // GameObject it aimed at. Carrot's Psionic Bond subscribes to a specific bound instance's
    // OnFired rather than a global broadcast, so it only ever reacts to that one plant shooting
    public event System.Action<GameObject> OnFired;

    public override void UpdateStats()
    {
        base.UpdateStats();
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
        else if (!IsStunned && !IsChanneling)
        {
            GameObject target = FindTarget();
            if (target != null)
            {
                attackCooldownTimer = 0;
                Vector3 predictedPosition = PredictTargetPosition(target);
                Shoot(predictedPosition);
                OnShoot();
                OnFired?.Invoke(target);
            }
        }

    }

    protected abstract void Shoot(Vector3 target);
    protected virtual void OnShoot() {}

    protected virtual GameObject FindTarget()
    {
        switch (targeting)
        {
            case TARGETING.First:     return FindFirst(Insect.allInsects);
            case TARGETING.Nearest:   return FindNearest(Insect.allInsects);
            case TARGETING.Last:      return FindLast(Insect.allInsects);
            case TARGETING.Strongest: return FindStrongest(Insect.allInsects);
            default:                  return null;
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
