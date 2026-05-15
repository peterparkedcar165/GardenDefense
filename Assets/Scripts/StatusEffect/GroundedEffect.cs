using UnityEngine;

public class GroundedEffect : StatusEffect
{
    private FlyingInsect flyingInsect;
    private Transform visual;
    private float velocity = 0f;
    public GroundedEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#8B4513>Grounded</color>";
    public override string GetDescription() => $"Prevents flight.";

    public override void OnApply()
    {
        flyingInsect = target as FlyingInsect;
        if (flyingInsect == null)
        {
            return;
        }

        velocity = 0f;
        visual = flyingInsect.transform.Find("Visual");
        flyingInsect.SetFlight(false);
    }

    public override void OnTick(float deltaTime)
    {
        if (visual == null)
        {
            return;
        }

        velocity += Insect.gravity * deltaTime;
        Vector3 pos = visual.localPosition;
        pos.y -= velocity * deltaTime;
        if (pos.y < 0.25f)
        {
            pos.y = 0.25f;
        }
        visual.localPosition = pos;
    }

    public override void OnExpire()
    {
        if (flyingInsect == null)
        {
            return;
        }
        flyingInsect.SetFlight(true);
    }
}
