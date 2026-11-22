using UnityEngine;

// kill participation: the same list exp is shared by (Insect.attackerSet: every plant that hit it,
// plus plants buffing or recently healing them)
public class CompostTeaMulchEffect : MulchEffect
{
    private readonly CompostTeaMulchData typedData;
    private bool listening;
    // health it gave back this level
    private float healed;

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

    public override string GetStatus() => $"Healed: {healed:0}";

    private void HandleDeath(EntityEventData death)
    {
        if (!OwnerAlive || death.target is not Insect insect) return;
        if (death.source != Owner && !insect.attackerSet.Contains(Owner)) return;
        float before = Owner.health;
        Owner.Heal(Owner.maxHealth * V(typedData.healPercentPerKill));
        healed += Mathf.Max(0f, Owner.health - before);
    }
}
