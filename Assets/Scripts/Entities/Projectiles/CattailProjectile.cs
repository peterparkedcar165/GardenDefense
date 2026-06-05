using UnityEngine;

// the Cattail's high-pressure water dart: single-target physical water hit (Water damage primes Wet
// automatically via the damage flow) that grounds the target if it was flying
public class CattailProjectile : Projectile
{
    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });

        PlaySound(hit);
        trackedTarget = null;
        if (!insect.IsAlive) return;

        // a hit on a flyer knocks it out of the air
        if (insect is FlyingInsect)
        {
            Cattail cattail = source as Cattail;
            insect.ApplyEffect(new GroundedEffect(insect, cattail != null ? cattail.GroundDuration : 6f, 1, source));
        }
    }
}
