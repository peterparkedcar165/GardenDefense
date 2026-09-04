using UnityEngine;

// walks the path until a plant enters leap range, then aims for 1 second and leaps using real gravity.
// immune to fall damage during an intentional leap; damage + web applied on landing.
// if CC'd mid-air the immunity is stripped and the spider takes normal fall damage on landing.
// unlike ground insects, it CAN leap onto a highground plant (that's the point of leaping) but
// still never targets a plant sitting on water, since landing there would just kill it. if its
// highground target dies and nothing else is in leap range, it leaps back to solid ground
// instead of being stranded up there.
public class JumpingSpider : Insect
{
    private JumpingSpiderData JSData => data as JumpingSpiderData;

    private float LeapRange      => JSData?.leapRange      ?? 3f;
    private float AimDuration    => JSData?.aimDuration    ?? 0.5f;
    private float JumpUpVelocity => JSData?.jumpUpVelocity ?? 7f;
    private float WebDuration    => JSData?.webDuration    ?? 2f;

    private enum LeapState { Walking, Aiming, Leaping, Attacking }
    private LeapState _leapState = LeapState.Walking;

    private bool        _fallDamageImmune;
    private bool        _hasLeftGround;
    private IAttackable _leapTarget;
    private float       _aimTimer;
    private Vector3 _leapDestination;
    private float   _leapHorizSpeed;

    // solid-ground memory: where to jump back to if stranded on a highground perch
    private Vector3 _leapOrigin;
    private bool    _isPerchedOnHighground;
    private bool    _pendingLeapIsHighground;
    private bool    _isReturnLeap;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity = Aggressivity.High;
    }

    protected override bool FallDamageImmune => _fallDamageImmune;

    // only exposes a target during the attacking phase so the base attack loop fires
    public override IAttackable target
    {
        get
        {
            if (_leapState == LeapState.Attacking && _leapTarget != null && _leapTarget.IsAlive)
                return _leapTarget;
            return null;
        }
    }

    protected override void Move()
    {
        if (isDying) return;

        bool ccActive = HasEffect<HardCrowdControl>();

        // cc while airborne: strip immunity so the fall deals damage on landing
        if (ccActive && _leapState == LeapState.Leaping && _fallDamageImmune)
        {
            _fallDamageImmune = false;
            _leapTarget       = null;  // cancel the landing attack
        }

        if (ccActive)
        {
            if (_leapState == LeapState.Leaping) CheckLanding();
            else if (_leapState == LeapState.Aiming) _leapState = LeapState.Walking;
            return;
        }

        // a taunt from anything other than what it's already attacking breaks off whatever it's
        // doing (walking, aiming at something else, or mid-attack) and leaps at the taunter
        // instead — mid-air is the only state left alone, since a leap can't redirect in flight.
        // taunted by the thing it's already attacking: nothing changes, it just keeps attacking
        if (_leapState != LeapState.Leaping)
        {
            IAttackable taunter = GetEffect<TauntEffect>()?.taunter;
            if ((taunter as UnityEngine.Object) != null && taunter != _leapTarget)
            {
                _leapTarget = taunter;
                _pendingLeapIsHighground = taunter is Plant tauntPlant
                    && tauntPlant.occupiedTile != null && tauntPlant.occupiedTile.isHighground;
                _aimTimer  = AimDuration;
                _leapState = LeapState.Aiming;
            }
        }

        switch (_leapState)
        {
            case LeapState.Walking:
                CheckLeapOpportunity();
                base.Move();
                break;

            case LeapState.Aiming:
                if (_leapTarget == null || !_leapTarget.IsAlive)
                { _leapState = LeapState.Walking; break; }
                _aimTimer -= Time.deltaTime;
                if (_aimTimer <= 0f) BeginLeap();
                break;

            case LeapState.Leaping:
                UpdateLeapHorizontal();
                CheckLanding();
                break;

            case LeapState.Attacking:
                if (_leapTarget == null || !_leapTarget.IsAlive)
                {
                    _leapTarget = null;
                    bool wasHighground = _isPerchedOnHighground;
                    if (!CheckLeapOpportunity() && wasHighground)
                        BeginReturnLeap();
                    else if (_leapState != LeapState.Aiming)
                        _leapState = LeapState.Walking;
                }
                break;
        }
    }

    // returns true and starts aiming if a valid target was found, false otherwise. never
    // targets a plant on water (fatal landing); highground plants ARE valid leap targets
    private bool CheckLeapOpportunity()
    {
        IAttackable nearest     = null;
        float       nearestDist = Mathf.Infinity;
        bool        nearestIsHighground = false;

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (plant.occupiedTile != null && plant.occupiedTile.tileType == TileType.Water) continue;
            float dist = Vector3.Distance(transform.position, plant.GetApproachPoint(transform.position));
            if (dist <= LeapRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = plant;
                nearestIsHighground = plant.occupiedTile != null && plant.occupiedTile.isHighground;
            }
        }

        foreach (Insect friendly in Insect.friendlyInsects)
        {
            if (friendly == null || !friendly.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, friendly.transform.position);
            if (dist <= LeapRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = friendly;
                nearestIsHighground = false;
            }
        }

        if (nearest == null) return false;

        _leapTarget = nearest;
        _pendingLeapIsHighground = nearestIsHighground;
        _aimTimer   = AimDuration;
        _leapState  = LeapState.Aiming;
        return true;
    }

    private void BeginLeap()
    {
        // only refresh the solid-ground memory when leaping from solid ground; leaping from one
        // highground perch to another keeps remembering the last real ground position
        if (!_isPerchedOnHighground) _leapOrigin = transform.position;

        _isReturnLeap     = false;
        _leapDestination  = _leapTarget.GetApproachPoint(transform.position);
        _hasLeftGround    = false;
        _fallDamageImmune = true;
        verticalVelocity  = -JumpUpVelocity;  // negative = upward in ApplyGravity convention

        // calculate horizontal speed so the spider arrives as the arc completes
        float horizDist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(_leapDestination.x,   _leapDestination.y));
        float airTime    = 2f * JumpUpVelocity / gravity;
        _leapHorizSpeed  = airTime > 0f ? horizDist / airTime : movementSpeed;

        _leapState = LeapState.Leaping;
    }

    // leaps to the nearest Path tile (ties broken by whichever is closest to where it originally
    // jumped from), with no target/attack on landing. falls back to the origin itself if somehow
    // no Path tile exists anywhere
    private void BeginReturnLeap()
    {
        _isReturnLeap     = true;
        _leapTarget       = null;
        _leapDestination  = FindNearestPathTile() ?? _leapOrigin;
        _hasLeftGround    = false;
        _fallDamageImmune = true;
        verticalVelocity  = -JumpUpVelocity;

        float horizDist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(_leapDestination.x,   _leapDestination.y));
        float airTime    = 2f * JumpUpVelocity / gravity;
        _leapHorizSpeed  = airTime > 0f ? horizDist / airTime : movementSpeed;

        _leapState = LeapState.Leaping;
    }

    // nearest Path tile to the spider's current (stranded) position; ties broken by whichever
    // candidate is closest to _leapOrigin, the last solid ground it jumped from
    private Vector3? FindNearestPathTile()
    {
        const float tieEpsilon = 0.01f;
        Tile best = null;
        float bestDist = Mathf.Infinity;
        float bestOriginDist = Mathf.Infinity;

        foreach (Tile tile in Tile.allTiles.Values)
        {
            if (tile == null || tile.tileType != TileType.Path) continue;
            float dist = Vector3.Distance(transform.position, tile.transform.position);
            float originDist = Vector3.Distance(_leapOrigin, tile.transform.position);

            if (dist < bestDist - tieEpsilon)
            {
                best = tile;
                bestDist = dist;
                bestOriginDist = originDist;
            }
            else if (dist <= bestDist + tieEpsilon && originDist < bestOriginDist)
            {
                best = tile;
                bestDist = Mathf.Min(bestDist, dist);
                bestOriginDist = originDist;
            }
        }

        return best != null ? best.transform.position : (Vector3?)null;
    }

    private void UpdateLeapHorizontal()
    {
        Vector3 toTarget = _leapDestination - transform.position;
        if (toTarget.magnitude > 0.05f)
            transform.position += toTarget.normalized * _leapHorizSpeed * Time.deltaTime;
    }

    private void CheckLanding()
    {
        if (visual != null && visual.localPosition.y > 0.5f)
            _hasLeftGround = true;

        if (!_hasLeftGround || !isOnGround) return;

        // clear immunity on any landing
        _fallDamageImmune = false;

        if (!_isReturnLeap && _leapTarget != null && _leapTarget.IsAlive)
        {
            float dist = Vector3.Distance(transform.position, _leapTarget.GetApproachPoint(transform.position));
            if (dist <= attackRange * 1.5f)
            {
                bool hit = _leapTarget.ReceiveAttack(attackDamage, this);
                if (hit && _leapTarget.IsAlive && _leapTarget is Entity landedOn)
                    landedOn.ApplyEffect(new WebbedEffect(landedOn, WebDuration, 1, this));
                _isPerchedOnHighground = _pendingLeapIsHighground;
                _leapState = LeapState.Attacking;
                return;
            }
        }

        _leapTarget = null;
        _isPerchedOnHighground = false;   // landed on solid ground either way (return leap or a miss)
        _leapState  = LeapState.Walking;
    }

    public override string GetDescription() =>
        $"Aggressive arachnid. Leaps onto plants and apply Webbed for <b>{WebDuration:F0}</b> seconds, " +
        "restricting any attacks or skill usage." + AggressivityLine();
}
