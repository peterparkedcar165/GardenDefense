using UnityEngine;

// digs underground on a cycle: burrowed, it's undetectable and unhittable (see Insect.isBurrowed,
// Entity.Damage's CanHitBurrowed gate, and Plant.IsValidNightTarget's DetectsBurrowed check) but
// keeps walking its path the whole time. once it resurfaces - either the timer runs out, or a
// hard CC lands (which can only happen via a plant that can actually detect it while burrowed) -
// it leaves behind an UndergroundTunnel spanning from where it started burrowing to where it
// ended up, letting other ground insects shortcut through afterward
public class Earthworm : Insect
{
    private EarthwormData EWData => data as EarthwormData;

    private float BurrowDuration      => EWData?.burrowDuration      ?? 4f;
    private float BurrowCooldown      => EWData?.burrowCooldown      ?? 6f;
    private float TunnelOpenDuration  => EWData?.tunnelOpenDuration  ?? 20f;

    private bool _burrowing;
    private float _burrowTimer;
    private float _cooldownTimer;
    private Vector3 _burrowStartPosition;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();

        if (isDying) return;

        if (_burrowing)
        {
            _burrowTimer -= Time.deltaTime;
            if (_burrowTimer <= 0f)
                EndBurrow();
        }
        else if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }
        else
        {
            BeginBurrow();
        }
    }

    private void BeginBurrow()
    {
        _burrowing = true;
        _burrowTimer = BurrowDuration;
        _burrowStartPosition = transform.position;
        SetBurrowed(true);
    }

    private void EndBurrow()
    {
        _burrowing = false;
        _cooldownTimer = BurrowCooldown;
        SetBurrowed(false);

        float markerSize = _spriteRenderer != null ? Mathf.Max(_spriteRenderer.bounds.size.x, _spriteRenderer.bounds.size.y) : 0.5f;
        UndergroundTunnel.Create(_burrowStartPosition, transform.position, TunnelOpenDuration, markerSize);
    }

    // any hard CC landing mid-burrow surfaces it early, right where it currently is, forming
    // the tunnel there instead of waiting out the full duration. this can only actually happen
    // via a plant with DetectsBurrowed, since that's the only way one could target it at all
    // while it's underground
    public override void ApplyEffect(StatusEffect effect)
    {
        if (_burrowing && effect is HardCrowdControl)
            EndBurrow();
        base.ApplyEffect(effect);
    }
}
