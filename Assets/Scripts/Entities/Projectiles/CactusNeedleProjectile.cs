public class CactusNeedleProjectile : Projectile
{
    public override void Initialize(UnityEngine.Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void OnHit(Insect insect)
    {
        if (source is Cactus cactus)
            cactus.OnNeedleHit(insect, projectileDamage);
        else
            insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
    }
}
