// the Ghost Fungus' attack: a little bolt of ice that deals physical damage to the first insect hit
public class GhostBoltProjectile : Projectile
{
    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
    }
}
