// fired by OldCarrot's Psionic Bond whenever the bonded Shooter fires. every stat (speed, range,
// damage, element, piercing) is OldCarrot's own - only the target and the timing come from the
// bonded Shooter. homes in and dashes through the same as OldCarrot's own attack (see
// DashHomingProjectile). damage is tagged Coordinated (scales with
// OldCarrot's coordinatedDamage stat, matching its Kinship identity), PassiveDamage, and Projectile.
// at OldCarrot's path1 max, also stacks the same Psionic Mark its own attack does (see
// OldCarrot.ApplyPsionicMark) - both attack types feed and benefit from one shared stack per target
public class PsionicOldCarrotProjectile : DashHomingProjectile
{
    private static readonly DamageTag[] psionicTags = { DamageTag.Coordinated, DamageTag.PassiveDamage, DamageTag.Projectile };

    protected override void OnHit(Insect insect)
    {
        OldCarrot oldCarrot = source as OldCarrot;

        float damage = projectileDamage;
        if (oldCarrot != null && oldCarrot.IsPath1Maxed)
            damage *= oldCarrot.ApplyPsionicMark(insect);

        insect.Damage(damage, damageType, elementalType, source, true, psionicTags);
    }
}
