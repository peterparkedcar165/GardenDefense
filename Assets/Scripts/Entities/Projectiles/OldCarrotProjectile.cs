using UnityEngine;

// hurled chunk of earth: single target, homes in on it (re-acquiring the nearest valid insect
// if it dies) and dashes through on each pass. at path 1 max, each hit also stacks a Psionic
// Mark that increases this OldCarrot's own subsequent damage on that target (see PsionicMarkEffect)
public class OldCarrotProjectile : DashHomingProjectile
{
    protected override void OnHit(Insect insect)
    {
        OldCarrot oldCarrot = source as OldCarrot;
        PlaySound(hit);

        float damage = projectileDamage;
        if (oldCarrot != null && oldCarrot.IsPath1Maxed)
            damage *= oldCarrot.ApplyPsionicMark(insect);

        insect.Damage(damage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Attack, DamageTag.SingleTarget, DamageTag.Projectile });
    }
}
