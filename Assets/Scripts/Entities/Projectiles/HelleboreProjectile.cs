public class HelleboreProjectile : Projectile
{
    protected override void OnHit(Insect insect)
    {
        Hellebore hellebore = source as Hellebore;
        float dmg = projectileDamage;
        if (hellebore != null && hellebore.IsPath1Maxed)
            dmg += hellebore.armor * 0.28f;
        insect.Damage(dmg, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Attack, DamageTag.Projectile, DamageTag.SingleTarget });
        hellebore?.OnProjectileHit();
        PlaySound(hit);
    }
}
