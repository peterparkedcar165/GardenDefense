using UnityEngine;

public class AcornProjectile : Projectile
{

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType);
    }

    protected override void Update()
    {
        base.Update();
    }
    
}
