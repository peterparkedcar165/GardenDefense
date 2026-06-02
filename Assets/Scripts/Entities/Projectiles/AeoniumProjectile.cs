// Aeonium's energy ball — Nature-magic orb that damages the first insect it hits
public class AeoniumProjectile : Projectile
{
    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
    }
}
