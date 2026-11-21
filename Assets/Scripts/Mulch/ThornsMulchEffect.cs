// an insect's physical melee attack landing on its plant (Entity.OnEntityHit, Attack and Melee tagged,
// physical, not damage over time) hurts that insect back with true neutral damage, sourced to the plant
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
        // only physical melee attacks (Tommy, 2026-11-21): a magic bite or a ranged hit doesn't
        // prick the attacker
        if (!Entity.HasTag(hit.tags, DamageTag.Attack) || !Entity.HasTag(hit.tags, DamageTag.Melee)
            || Entity.HasTag(hit.tags, DamageTag.DoT) || hit.damageType != DamageType.Physical) return;
        attacker.Damage(V(typedData.damage), DamageType.True, ElementalType.Neutral, Owner, false, ThornTags);
    }
}
