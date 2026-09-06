using UnityEngine;

public abstract class FlyingInsect : Insect
{
    [SerializeField] private Sprite spriteFlyRight;
    [SerializeField] private Sprite spriteFlyLeft;

    public float flightSpeed; // flight speed is ALWAYS 2x of movementSpeed
    public float flightHeight;
    private float hoverSpeed;
    private float hoverAmplitude;
    private float hoverPhase;

    protected override void Awake()
    {
        base.Awake();
        flightHeight = Random.Range(1.25f, 1.75f);
        hoverSpeed = Random.Range(2f, 3f);
        hoverAmplitude = Random.Range(0.075f, 0.125f);
        hoverPhase = Random.Range(0f, Mathf.PI * 2f);
        isFlying = true;
        visual = transform.Find("Visual");
        if (visual != null)
        {
            visual.localPosition += new Vector3(0, 0.4f + flightHeight, 0);
        }
    }

    public const float FlightEvasionBonus = 0.10f;

    public override void UpdateStats()
    {
        base.UpdateStats();
        {
            flightSpeed = 2f * movementSpeed;
            if (isFlying) evasion += FlightEvasionBonus;
        }
    }

    protected override float GetMoveSpeed() => isFlying ? flightSpeed : movementSpeed;

    public virtual void SetFlight(bool newState)
    {
        bool wasFlying = isFlying;
        isFlying = newState;

        // while flying, obstacle collision is ignored entirely (see Insect.Move's isFlying
        // bypass), so landing can leave it positioned directly on top of one - push it clear from
        // the obstacle tile's own center so it doesn't end up stuck inside solid geometry
        if (wasFlying && !newState)
            PushOffObstacleIfLanded();
    }

    private void PushOffObstacleIfLanded()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.15f, ObstacleLayer);
        if (hit == null) return;

        Vector3 obstacleCenter = hit.bounds.center;
        Vector2 pushDir = transform.position - obstacleCenter;
        if (pushDir.sqrMagnitude < 0.0001f) pushDir = Vector2.up;
        else pushDir.Normalize();

        float pushDistance = Mathf.Max(hit.bounds.extents.x, hit.bounds.extents.y) + 0.2f;
        transform.position = obstacleCenter + (Vector3)(pushDir * pushDistance);
    }

    protected override void UpdateFacingSprite()
    {
        if (_spriteRenderer == null) return;
        Sprite s = isFlying
            ? (_facingRight ? spriteFlyRight  : spriteFlyLeft)
            : (_facingRight ? spriteRight     : spriteLeft);
        if (s != null) _spriteRenderer.sprite = s;
    }

    public override void ApplyEffect(StatusEffect effect)
    {
        // a Freeze that Insect.ApplyEffect is about to block outright for an ICryotolerant flyer
        // shouldn't still ground it on the way through
        bool blockedByCryotolerance = this is ICryotolerant && effect is FreezeEffect;
        if (effect is HardCrowdControl and not BubblePrisonEffect && !blockedByCryotolerance)
        {
            float groundDuration = effect is KnockUpEffect ? 5f : effect.duration + 4f;
            base.ApplyEffect(new GroundedEffect(this, groundDuration, 1, effect.source));
        }
        base.ApplyEffect(effect);
    }

    protected override void Update()
    {
        base.Update();
        if (!isDying && isFlying && visual != null && !HasEffect<BubblePrisonEffect>())
        {
            hoverPhase += Time.deltaTime * hoverSpeed;
            Vector3 pos = visual.localPosition;
            pos.y = Mathf.MoveTowards(pos.y, 0.4f + flightHeight + Mathf.Sin(hoverPhase) * hoverAmplitude, 3f * Time.deltaTime);
            visual.localPosition = pos;
        }
    }

    // appended by every flying insect's GetDescription(), before the (always-last) AggressivityLine
    protected string FlyingLine() =>
        $"\n\n<b>Flying:</b> Immobilization effects ground it for the effect's duration + 4 seconds " +
        $"(5 seconds if Knocked Up).\n\nIncreases Evasion by <color=green><b>{FlightEvasionBonus * 100f:F0}%</b></color> while flying.";
}

// whenever a flying insect is HARD CC'ed, they get grounded, bringing their visual down to their original position
// Grounded will be an effect that when applied, sets flying to false. and on every tick, drops
// the visual Y position until it reaches the position of its gameobject
// when NOT grounded, insect's visual flies up until it reaches its flightHeight
// upon expire of Grounded, isFlying is set back to true.

// now the issue, if an insect is grounded, it will constantly shove its visual downwards, but if it's knocked up, there might be conflict
// or if it's bubbled by the water lily, what to do?