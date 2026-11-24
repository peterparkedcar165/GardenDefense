public class BogIrisProjectile : Projectile
{
    // made once and shared by every hit: nothing writes into it
    private static readonly DamageTag[] HitTags = { DamageTag.SingleTarget, DamageTag.Attack, DamageTag.Projectile };

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            HitTags);

        (source as BogIris)?.TryReduceSunTimer();
    }
}
