using UnityEngine;

public class LeafRangerProjectile : Projectile
{
    // True Flight (skill tree): +3% damage per unit of distance already traveled when it hits
    private const float TrueFlightBonusPerUnit = 0.03f;

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override bool DisablePierceFalloff =>
        source is LeafRanger lr && SkillTreeManager.HasUnlock(lr, LeafRanger.TrueFlightUnlock);

    protected override void OnHit(Insect insect)
    {
        float damage = projectileDamage;
        if (DisablePierceFalloff)
        {
            float distanceTraveled = Vector3.Distance(spawnPosition, transform.position);
            damage *= 1f + TrueFlightBonusPerUnit * distanceTraveled;
        }
        insect.Damage(damage, damageType, elementalType, source, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
        PlaySound(hit);
        trackedTarget = null;
    }

}
