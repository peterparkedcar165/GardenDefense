// applied to a Medium-aggressivity insect that starts attacking an Acorn Sprout above 25% health.
// extends TauntEffect so it's picked up by the same taunter-checking logic in Insect.target,
// but self-removes once the Acorn Sprout drops to or below that health threshold
public class DeliciousAcornEffect : TauntEffect
{
    public const float HealthThreshold = 0.25f;

    private readonly AcornSprout acorn;

    public DeliciousAcornEffect(Entity target, Entity source, AcornSprout acorn)
        : base(target, float.MaxValue, 1, source, acorn)
    {
        this.acorn = acorn;
    }

    public override string GetName() => "<color=green>Delicious Acorn</color>";
    public override string GetDescription() =>
        $"Cannot stop attacking the <color=green><b>Acorn Sprout</b></color> while it remains above <color=green><b>{HealthThreshold * 100f:F0}%</b></color> Health.";

    public override void OnTick(float deltaTime)
    {
        if (acorn == null || !acorn.IsAlive || acorn.health <= acorn.maxHealth * HealthThreshold)
            target.RemoveEffect<DeliciousAcornEffect>();
    }
}
