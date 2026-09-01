// Shelter family passive: while a Shelter-family plant remains above HealthThreshold, any
// insect that targets it (High or Medium aggressivity) is forced to keep attacking it instead
// of retargeting elsewhere. extends TauntEffect so it's picked up by the same taunter-checking
// logic in Insect.target, but self-removes once the plant drops to or below the threshold,
// letting the insect target normally again. generalizes what used to be Acorn Sprout's own
// DeliciousAcornEffect (now removed, redundant since Acorn Sprout is itself Shelter family)
public class ShelterAggroEffect : TauntEffect
{
    public const float HealthThreshold = 0.25f;

    private readonly Plant plant;

    public ShelterAggroEffect(Entity target, Entity source, Plant plant)
        : base(target, float.MaxValue, 1, source, plant)
    {
        this.plant = plant;
    }

    public override string GetName() => "<color=#A9A9A9>Retained</color>";
    public override string GetDescription() =>
        $"Cannot stop attacking this plant while it remains above <color=green><b>{HealthThreshold * 100f:F0}%</b></color> Health.";

    public override void OnTick(float deltaTime)
    {
        if (plant == null || !plant.IsAlive || plant.health <= plant.maxHealth * HealthThreshold)
            target.RemoveEffect<ShelterAggroEffect>();
    }
}
