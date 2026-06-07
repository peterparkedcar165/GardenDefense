using UnityEngine;

// walks the path until a plant enters leap range, then aims for 1 second and leaps using real gravity.
// immune to fall damage during an intentional leap; damage + web applied on landing.
// if CC'd mid-air the immunity is stripped and the spider takes normal fall damage on landing.
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
                { _leapTarget = null; _leapState = LeapState.Walking; }
                break;
        }
    }

    private void CheckLeapOpportunity()
    {
        IAttackable nearest     = null;
        float       nearestDist = Mathf.Infinity;

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (plant.occupiedTile != null && plant.occupiedTile.isHighground) continue;
            if (plant.occupiedTile != null && plant.occupiedTile.tileType == TileType.Water) continue;
            float dist = Vector3.Distance(transform.position, plant.GetApproachPoint(transform.position));
            if (dist <= LeapRange && dist < nearestDist) { nearestDist = dist; nearest = plant; }
        }

        foreach (Insect friendly in Insect.friendlyInsects)
        {
            if (friendly == null || !friendly.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, friendly.transform.position);
            if (dist <= LeapRange && dist < nearestDist) { nearestDist = dist; nearest = friendly; }
        }

        if (nearest != null)
        {
            _leapTarget = nearest;
            _aimTimer   = AimDuration;
            _leapState  = LeapState.Aiming;
        }
    }

    private void BeginLeap()
    {
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

        if (_leapTarget != null && _leapTarget.IsAlive)
        {
            float dist = Vector3.Distance(transform.position, _leapTarget.GetApproachPoint(transform.position));
            if (dist <= attackRange * 1.5f)
            {
                bool hit = _leapTarget.ReceiveAttack(attackDamage, this);
                if (hit && _leapTarget.IsAlive && _leapTarget is Entity landedOn)
                    landedOn.ApplyEffect(new WebbedEffect(landedOn, WebDuration, 1, this));
                _leapState = LeapState.Attacking;
                return;
            }
        }

        _leapTarget = null;
        _leapState  = LeapState.Walking;
    }
}
