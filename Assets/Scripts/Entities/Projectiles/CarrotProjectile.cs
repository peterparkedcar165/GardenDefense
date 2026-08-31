using UnityEngine;

// hurled chunk of earth: single target, homes in on it (re-acquiring the nearest valid insect
// if it dies) and dashes through on each pass. at path 1 max, each hit also stacks a Psionic
// Mark that increases this Carrot's own subsequent damage on that target (see PsionicMarkEffect)
public class CarrotProjectile : DashHomingProjectile
{
    protected override void OnHit(Insect insect)
    {
        Carrot carrot = source as Carrot;
        PlaySound(hit);

        float damage = projectileDamage;
        if (carrot != null && carrot.IsPath1Maxed)
            damage *= carrot.ApplyPsionicMark(insect);

        insect.Damage(damage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Attack, DamageTag.SingleTarget });
    }
}
