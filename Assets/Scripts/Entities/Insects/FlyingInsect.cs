using UnityEngine;

public abstract class FlyingInsect : Insect
{

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

    protected override void UpdateStats()
    {
        base.UpdateStats();
        {
            flightSpeed = 2f * movementSpeed;
        }
    }

    protected override float GetMoveSpeed() => isFlying ? flightSpeed : movementSpeed;

    public virtual void SetFlight(bool newState)
    {
        isFlying = newState;
    }

    public override void ApplyEffect(StatusEffect effect)
    {
        if (effect is HardCrowdControl and not BubblePrisonEffect)
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
}

// whenever a flying insect is HARD CC'ed, they get grounded, bringing their visual down to their original position
// Grounded will be an effect that when applied, sets flying to false. and on every tick, drops
// the visual Y position until it reaches the position of its gameobject
// when NOT grounded, insect's visual flies up until it reaches its flightHeight
// upon expire of Grounded, isFlying is set back to true.

// now the issue, if an insect is grounded, it will constantly shove its visual downwards, but if it's knocked up, there might be conflict
// or if it's bubbled by the water lily, what to do?