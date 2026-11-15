// listens to Entity.OnEntityDied, which fires once per insect death (Insect.Kill guards against
// dying twice), with the killing plant as the source. a kill by a damage over time tick counts
// for the plant that applied it
public class CompostTeaMulchEffect : MulchEffect
{
    private readonly CompostTeaMulchData typedData;

    public CompostTeaMulchEffect(CompostTeaMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => Entity.OnEntityDied += HandleDeath;
    public override void Deactivate() => Entity.OnEntityDied -= HandleDeath;

    private void HandleDeath(EntityEventData death)
    {
        if (death.target is not Insect) return;
        if (death.source is not Plant killer || killer == null || !killer.IsAlive) return;
        killer.Heal(killer.maxHealth * typedData.healPercentPerKill);
    }
}
