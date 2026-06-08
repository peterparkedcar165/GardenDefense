public class HelleboreProjectile : Projectile
{
    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Attack, DamageTag.Projectile, DamageTag.SingleTarget });
        (source as Hellebore)?.OnProjectileHit();
        PlaySound(hit);
    }
}
