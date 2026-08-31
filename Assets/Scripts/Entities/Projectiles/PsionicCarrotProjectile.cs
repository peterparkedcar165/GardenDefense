// fired by Carrot's Psionic Bond whenever the bonded Shooter fires. every stat (speed, range,
// damage, element, piercing) is Carrot's own - only the target and the timing come from the
// bonded Shooter. homes in and dashes through the same as Carrot's own attack (see
// DashHomingProjectile). damage is tagged Coordinated (scales with
// Carrot's coordinatedDamage stat, matching its Kinship identity), PassiveDamage, and Projectile.
// at Carrot's path1 max, also stacks the same Psionic Mark its own attack does (see
// Carrot.ApplyPsionicMark) - both attack types feed and benefit from one shared stack per target
public class PsionicCarrotProjectile : DashHomingProjectile
{
    private static readonly DamageTag[] psionicTags = { DamageTag.Coordinated, DamageTag.PassiveDamage, DamageTag.Projectile };

    protected override void OnHit(Insect insect)
    {
        Carrot carrot = source as Carrot;

        float damage = projectileDamage;
        if (carrot != null && carrot.IsPath1Maxed)
            damage *= carrot.ApplyPsionicMark(insect);

        insect.Damage(damage, damageType, elementalType, source, true, psionicTags);
    }
}
