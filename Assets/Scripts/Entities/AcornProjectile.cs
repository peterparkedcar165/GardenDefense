using System;
using UnityEngine;

public class AcornProjectile : Projectile
{

    public override void Initialize(GameObject target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType);
    }

    protected virtual void Update()
    {
        base.Update();
    }
    
}
