public class BogIrisProjectile : Projectile
{
    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.SingleTarget, DamageTag.Attack, DamageTag.Projectile });
    }
}
