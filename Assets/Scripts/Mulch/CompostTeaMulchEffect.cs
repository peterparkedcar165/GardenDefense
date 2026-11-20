// kill participation: the same list exp is shared by (Insect.attackerSet: every plant that hit it,
// plus plants buffing or recently healing them)
public class CompostTeaMulchEffect : MulchEffect
{
    private readonly CompostTeaMulchData typedData;
    private bool listening;

    public CompostTeaMulchEffect(CompostTeaMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (listening) return;
        listening = true;
        Entity.OnEntityDied += HandleDeath;
    }

    public override void Deactivate()
    {
        if (!listening) return;
        listening = false;
        Entity.OnEntityDied -= HandleDeath;
    }

    private void HandleDeath(EntityEventData death)
    {
        if (!OwnerAlive || death.target is not Insect insect) return;
        if (death.source != Owner && !insect.attackerSet.Contains(Owner)) return;
        Owner.Heal(Owner.maxHealth * V(typedData.healPercentPerKill));
    }
}
