using UnityEngine;

// Dandelion's Wind Gust: an insect caught by the gust is Displaced and, for the effect's
// duration, continuously swept along at the gust's own velocity (fed into windVelocity every
// tick, the same way the gust's own travel works) - see Insect.Move for how windVelocity
// becomes actual movement, and DisplacedEffect's own doc comment for the walk-back-to-original-
// position behavior that follows once this expires (handled generically by Insect.Move, not here)
public class WindDisplacedEffect : DisplacedEffect
{
    private readonly Vector2 velocity;

    public WindDisplacedEffect(Entity target, float duration, Entity source, Vector2 velocity)
        : base(target, duration, 1, source)
    {
        this.velocity = velocity;
    }

    public override void OnTick(float deltaTime)
    {
        base.OnTick(deltaTime);
        if (target is Insect insect) insect.windVelocity += velocity;
    }
}
