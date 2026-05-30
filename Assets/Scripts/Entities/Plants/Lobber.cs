using UnityEngine;

public abstract class Lobber : Plant
{
    public override bool UsesTargeting => true;
    public GameObject projectilePrefab;

    [Header("Lobber")]
    public float baseProjectileSpeed = 4f;
    public float projectileSpeed;
    public float baseAoERadius = 1.5f;
    public float aoERadius;
    public float minFlightDuration = 0.5f;
    public float projectileHeight = 1.2f;

    protected float projectileSpeedAdder;
    protected float aoERadiusAdder;

    protected override void Awake()
    {
        base.Awake();
        if (data != null)
        {
            baseProjectileSpeed = data.baseProjectileSpeed;
            baseAoERadius       = data.baseAoERadius;
            minFlightDuration   = data.minFlightDuration;
            projectileHeight    = data.projectileHeight;
        }
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        projectileSpeed = baseProjectileSpeed + projectileSpeedAdder;
        aoERadius = baseAoERadius + aoERadiusAdder;
    }

    protected override void Update()
    {
        base.Update();

        attackCooldown = 1f / attackSpeed;
        if (attackCooldownTimer < attackCooldown)
        {
            attackCooldownTimer += Time.deltaTime;
        }
        else if (!IsStunned)
        {
            GameObject target = FindLobberTarget();
            if (target != null)
            {
                attackCooldownTimer = 0f;
                Fire(target, GetLandingPosition(target));
            }
        }
    }

    protected virtual GameObject FindLobberTarget()
    {
        switch (targeting)
        {
            case TARGETING.First:   return FindFirst(Insect.allInsects);
            case TARGETING.Nearest: return FindNearest(Insect.allInsects);
            case TARGETING.Last:    return FindLast(Insect.allInsects);
            default:                return null;
        }
    }

    protected virtual Vector3 GetLandingPosition(GameObject target)
    {
        Insect insect = target.GetComponent<Insect>();
        if (insect != null && insect.IsAlive)
        {
            float dist = Vector3.Distance(transform.position, insect.transform.position);
            float travelTime = dist / Mathf.Max(projectileSpeed, 0.01f);
            return insect.GetAimPoint() + insect.GetVelocity() * 0.75f * travelTime;
        }
        return target.transform.position;
    }

    protected abstract void Fire(GameObject target, Vector3 landingPos);
}
