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

    // height (Tommy, 2026-11-25): the root is the seed's base, on the ground line it travels along; its
    // Visual (and the fly particles, moved under it) is drawn launchHeight above, starting at Rhodiola's
    // staff gem and coming down evenly to the base as it closes in, so it lands on its target. the Shadow
    // stays on the base. a bounce starts on the ground, at no height
    private Transform _visual;
    private Vector3 _visualRestLocal;
    private float _launchHeight;
    private float _launchDistance;

    // the fly particles (Tommy, 2026-11-26) drift straight back from the way the seed is moving, not out
    // from its middle. their prefab's Start Speed is that drift's speed in tiles a second: it's moved onto
    // Velocity over Lifetime, which is turned every frame to point back along the drawn seed's last move
    // (so its drop from the gem counts too). they're in Local space, so the drift is relative to the seed
    private const string FlyParticlesName = "RhodiolaProjectileFlyParticles";
    private ParticleSystem _flyParticles;
    private float _flyBackSpeed;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        _visual = transform.Find("Visual");
        if (_visual != null)
        {
            _visualRestLocal = _visual.localPosition;
            // the fly particles ride with the drawn seed, not its base
            foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
                if (ps.transform.parent == transform) ps.transform.SetParent(_visual, true);
        }
        SetUpFlyParticles();
    }

    private void SetUpFlyParticles()
    {
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
            if (ps.name == FlyParticlesName) { _flyParticles = ps; break; }
        if (_flyParticles == null) return;

        ParticleSystem.MainModule main = _flyParticles.main;
        _flyBackSpeed = main.startSpeed.constant;
        main.startSpeed = 0f;

        ParticleSystem.VelocityOverLifetimeModule velocity = _flyParticles.velocityOverLifetime;
        velocity.enabled = true;
        // in the system's own space, which its Local scaling keeps at tile size (world space could convert
        // through the seed's 0.3 scale); the seed never turns, so its axes are the world's
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0f;
        velocity.z = 0f;
        velocity.radial = 0f;
        velocity.orbitalX = 0f;
        velocity.orbitalY = 0f;
        velocity.orbitalZ = 0f;
        velocity.speedModifier = 1f;
    }

    // points the fly particles' drift back against moving (a world direction, any length); no move
    // (paused, or held after a hit) keeps the last direction
    private void PointFlyParticlesBack(Vector3 moving)
    {
        if (_flyParticles == null || moving.sqrMagnitude < 1e-10f) return;
        Vector3 back = -moving.normalized * _flyBackSpeed;
        ParticleSystem.VelocityOverLifetimeModule velocity = _flyParticles.velocityOverLifetime;
        velocity.x = back.x;
        velocity.y = back.y;
    }

    private Vector3 SeedPosition() => _visual != null ? _visual.position : transform.position;

    // the drawn seed starts at this world height (the gem's), its base staying where it was spawned
    public void SetLaunchHeight(float worldY)
    {
        if (_visual == null) return;
        _launchHeight = worldY - transform.TransformPoint(_visualRestLocal).y;
        _launchDistance = Vector3.Distance(transform.position, _lastKnownTargetPosition);
        PlaceVisual();
        // the drawn seed comes down in a straight line to its target's base
        PointFlyParticlesBack(_lastKnownTargetPosition - _visual.position);
    }

    private void PlaceVisual()
    {
        if (_visual == null || _launchHeight == 0f) return;
        float share = _launchDistance > 0.0001f
            ? Mathf.Clamp01(Vector3.Distance(transform.position, _lastKnownTargetPosition) / _launchDistance) : 0f;
        _visual.position = transform.TransformPoint(_visualRestLocal) + Vector3.up * (_launchHeight * share);
    }

    public void Initialize(Rhodiola source, Entity target, float speed, float healAmount, bool canBounce)
    {
        _source     = source;
        _target     = target;
        _speed      = speed;
        _healAmount = healAmount;
        _canBounce  = canBounce;
        _lastKnownTargetPosition = target != null ? target.transform.position : transform.position;
        PointFlyParticlesBack(_lastKnownTargetPosition - transform.position);
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
                DetachParticles();
                Destroy(gameObject);
            }
            return;
        }

        if (_target != null && _target.health > 0)
            _lastKnownTargetPosition = _target.transform.position;

        Vector3 targetPos = _lastKnownTargetPosition;
        Vector3 seedBefore = SeedPosition();
        transform.position = Vector3.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);
        PlaceVisual();
        PointFlyParticlesBack(SeedPosition() - seedBefore);

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

        DetachParticles();
        Destroy(gameObject);
    }

    // detaches every child particle system (the fly/trail particles) right before this
    // projectile's GameObject is destroyed, so each one survives and finishes fading out
    // naturally instead of popping out of existence along with the parent - same treatment
    // Projectile.OnBeforeDestroy gives every other projectile, duplicated here since this class
    // deliberately isn't built on that base (see the class comment)
    private void DetachParticles()
    {
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>())
        {
            Transform t = ps.transform;
            Vector3 worldPos = t.position;
            Quaternion worldRot = t.rotation;

            t.SetParent(null, false);
            t.position = worldPos;
            t.rotation = worldRot;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(ps.gameObject, ps.main.startLifetime.constantMax);
        }
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
