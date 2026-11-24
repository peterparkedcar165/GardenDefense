public class HelleboreProjectile : Projectile
{
    // made once and shared by every hit: nothing writes into it
    private static readonly DamageTag[] HitTags = { DamageTag.Attack, DamageTag.Projectile, DamageTag.SingleTarget };

    protected override void OnHit(Insect insect)
    {
        Hellebore hellebore = source as Hellebore;
        float dmg = projectileDamage;
        if (hellebore != null && hellebore.IsPath1Maxed)
            dmg += hellebore.armor * 0.28f;
        insect.Damage(dmg, damageType, elementalType, source, true,
            HitTags);
        hellebore?.OnProjectileHit();
        PlaySound(hit);
    }
}
