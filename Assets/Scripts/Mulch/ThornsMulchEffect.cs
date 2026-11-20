// an insect's attack landing on its plant (Entity.OnEntityHit, Attack tagged, not damage over time)
// hurts that insect back with true damage, sourced to the plant
public class ThornsMulchEffect : MulchEffect
{
    private static readonly DamageTag[] ThornTags = { DamageTag.PassiveDamage };

    private readonly ThornsMulchData typedData;
    private bool listening;

    public ThornsMulchEffect(ThornsMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (listening) return;
        listening = true;
        Entity.OnEntityHit += HandleHit;
    }

    public override void Deactivate()
    {
        if (!listening) return;
        listening = false;
        Entity.OnEntityHit -= HandleHit;
    }

    private void HandleHit(EntityEventData hit)
    {
        if (!OwnerAlive || hit.target != Owner || hit.source is not Insect attacker || !attacker.IsAlive) return;
        if (attacker.team == Team.Friendly) return;
        if (!Entity.HasTag(hit.tags, DamageTag.Attack) || Entity.HasTag(hit.tags, DamageTag.DoT)) return;
        attacker.Damage(V(typedData.damage), DamageType.True, ElementalType.Neutral, Owner, false, ThornTags);
    }
}
