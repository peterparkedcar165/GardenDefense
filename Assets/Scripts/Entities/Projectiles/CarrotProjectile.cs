using UnityEngine;
using System.Collections.Generic;

// hurled chunk of earth: full damage to the first insect hit, reduced damage in a small
// radius around the impact, knocks down flying insects. at path 1 max the impact also
// knocks insects back
public class CarrotProjectile : Projectile
{
    // the base projectile fires OnHit once per insect overlapped in the same frame,
    // and destruction only happens at end of frame. this chunk detonates exactly once
    private bool _hasImpacted;

    protected override void OnHit(Insect insect)
    {
        if (_hasImpacted) return;
        _hasImpacted = true;

        Carrot thorn = source as Carrot;
        PlaySound(hit);

        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Attack, DamageTag.SingleTarget });
        ApplyImpactEffects(insect, thorn);

        float splashRadius = thorn != null ? thorn.SplashRadius : 1f;
        float splashMultiplier = thorn != null ? thorn.SplashMultiplier : 0.75f;
        Vector3 impact = insect.transform.position;

        foreach (Insect other in new List<Insect>(Insect.allInsects))
        {
            if (other == null || other == insect || !other.IsAlive || other.team == Team.Friendly) continue;
            if (Vector3.Distance(impact, other.transform.position) > splashRadius) continue;

            other.Damage(projectileDamage * splashMultiplier, damageType, elementalType, source, true,
                new DamageTag[] { DamageTag.Attack, DamageTag.AoE });
            ApplyImpactEffects(other, thorn);
        }
    }

    private void ApplyImpactEffects(Insect insect, Carrot thorn)
    {
        if (thorn == null) return;

        if (insect.IsAlive && insect.isFlying)
            insect.ApplyEffect(new GroundedEffect(insect, thorn.GroundedDuration, 1, thorn));

        thorn.ApplyTileBonus(insect);

        // passive max, attacks also shred a portion of the target's armor
        if (thorn.IsPath2Maxed && insect.IsAlive)
            insect.ApplyEffect(new SunderedEffect(insect, thorn.SunderDuration, 1, thorn, thorn.SunderPercent));

        if (thorn.IsPath1Maxed && insect.IsAlive)
            thorn.PushInsect(insect, direction, thorn.KnockbackDistance);
    }
}
