using UnityEngine;

// homing heal projectile fired by Rhodiola. modeled on Gloriosa's EmberProjectile (no collider,
// detonates only once it reaches its tracked target) but heals only - Rhodiola never attacks
// insects with this kit. never retargets: if the target dies mid-flight it keeps flying to its
// last known position and still gets destroyed there normally, it just heals nothing. Widespread
// Bloom lets it bounce once more, to the next most injured plant/ally, after landing on its
// primary target
//
// deliberately not built on the Projectile base class, so it never reads piercing/basePiercing/
// piercingAdder at all - Rhodiola inherits those fields from Shooter (any fertilizer or skill
// tree node that grants generic Piercing still sets them), but this projectile is intentionally
// unaffected by that stat. see the "*Unaffected by Piercing" note on GetPath1Description
public class RhodiolaProjectile : MonoBehaviour
{
    private Rhodiola _source;
    private Entity   _target;
    private float    _speed;
    private float    _healAmount;
    private bool     _canBounce;
    private bool     _isBounce;
    private Vector3  _lastKnownTargetPosition;

    // brief freeze on impact before spawning the bounce, matching the Oleander/Gloriosa bounce pacing
    private const float BounceHitPause = 0.05f;
    private bool   _isPausedAfterHit;
    private float  _pauseTimer;
    private Entity _pendingBounceTarget;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    public void Initialize(Rhodiola source, Entity target, float speed, float healAmount, bool canBounce)
    {
        _source     = source;
        _target     = target;
        _speed      = speed;
        _healAmount = healAmount;
        _canBounce  = canBounce;
        _lastKnownTargetPosition = target != null ? target.transform.position : transform.position;
    }

    public void MarkAsBounce() => _isBounce = true;

    void Update()
    {
        if (_isPausedAfterHit)
        {
            _pauseTimer -= Time.deltaTime;
            if (_pauseTimer <= 0f)
            {
                if (_pendingBounceTarget != null)
                    _source.SpawnBounceProjectile(transform.position, _pendingBounceTarget, _healAmount);
                Destroy(gameObject);
            }
            return;
        }

        if (_target != null && _target.health > 0)
            _lastKnownTargetPosition = _target.transform.position;

        Vector3 targetPos = _lastKnownTargetPosition;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.15f)
            Detonate();
    }

    private void Detonate()
    {
        if (_target != null && _target.health > 0)
            _source.HealTick(_target, !_isBounce);

        if (!_isBounce && _canBounce)
        {
            Entity bounceTarget = FindBounceTarget();
            if (bounceTarget != null)
            {
                _pendingBounceTarget = bounceTarget;
                _isPausedAfterHit = true;
                _pauseTimer = BounceHitPause;
                return;
            }
        }

        Destroy(gameObject);
    }

    // lowest health fraction among plants and friendly insects, excluding the primary target,
    // within the Rhodiola's own attack range from the impact point
    private Entity FindBounceTarget()
    {
        float range = _source != null ? _source.attackRange : float.MaxValue;

        Entity best = null;
        float bestFrac = float.MaxValue;

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == _target) continue;
            if (plant.health >= plant.maxHealth) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > range) continue;
            float frac = plant.health / plant.maxHealth;
            if (frac < bestFrac) { bestFrac = frac; best = plant; }
        }

        foreach (Insect ally in Insect.friendlyInsects)
        {
            if (ally == null || !ally.IsAlive || (Entity)ally == _target) continue;
            if (ally.health >= ally.maxHealth) continue;
            if (Vector3.Distance(transform.position, ally.transform.position) > range) continue;
            float frac = ally.health / ally.maxHealth;
            if (frac < bestFrac) { bestFrac = frac; best = ally; }
        }

        return best;
    }
}
