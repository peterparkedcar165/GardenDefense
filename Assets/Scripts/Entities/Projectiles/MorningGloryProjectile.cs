// single-target wind blade. damage is computed by the plant (attackDamage * projectileSpeed)
// and passed in as projectileDamage
public class MorningGloryProjectile : Projectile
{
    public float speedDamageScale = 2.5f;

    protected override void OnHit(Insect insect)
    {
        // attackDamage + scale * the projectile's OWN speed (so a mid-flight speed change
        // is reflected) rather than the plant's current speed
        insect.Damage(projectileDamage + speedDamageScale * projectileSpeed, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
    }
}
