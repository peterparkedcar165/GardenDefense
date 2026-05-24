using UnityEngine;

public class KnockUpEffect : Airborne
{
    private Insect insect;
    private float knockUpForce;

    public KnockUpEffect(Entity target, float duration, int level, Entity source, float knockUpForce)
        : base(target, duration, level, source)
    {
        this.knockUpForce = knockUpForce;
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#4FC3F7>Knocked Up</color>";
    public override string GetDescription() => "Launched into the air.";

    public override void OnApply()
    {
        insect = target as Insect;
        if (insect == null) return;
        insect.fallDamageSource = source;
        float tenacityScale = Mathf.Sqrt(Mathf.Max(0f, 1f - insect.tenacity));
        float force = -knockUpForce * tenacityScale;
        // If already airborne from a prior knock-up, compound the forces so
        // multiple simultaneous geysers send the insect proportionally higher.
        if (!insect.isOnGround && insect.verticalVelocity < 0f)
            insect.verticalVelocity += force;
        else
            insect.verticalVelocity = force;
    }

    public override void OnTick(float deltaTime)
    {
        if (insect == null) return;
        if (insect.isOnGround && insect.verticalVelocity >= 0f) { duration = 0f; return; }
        if (insect.HasEffect<BubblePrisonEffect>() && insect.verticalVelocity >= 0f) { duration = 0f; return; }
    }
}
