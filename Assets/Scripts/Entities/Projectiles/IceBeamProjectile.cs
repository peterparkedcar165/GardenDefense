using UnityEngine;

// Ice Beam (Snowdrop's skill): same hit logic as SnowdropProjectile, but piercing no longer halves
// damage on subsequent targets - it keeps hitting at full damage up to (piercing + 1) insects,
// then stops, rather than landing once and vanishing like a normal projectile
public class IceBeamProjectile : Projectile
{
    // this projectile only ever exists while the skill is active (see Snowdrop.Shoot), so the
    // SkillDamage tag is unconditional here rather than a live IceBeamActive check
    private static readonly DamageTag[] attackTags = { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget, DamageTag.SkillDamage };

    protected override bool DisablePierceFalloff => true;

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true, attackTags);

        if (!insect.IsAlive) return;
        (source as Snowdrop)?.OnIceAttackHit(insect);
    }
}
