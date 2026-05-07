using UnityEngine;

public class SoldierAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 2;
        baseMaxHealth = 320f;
        basePhysicalResistance = 0.33f;
        sunDrop = 18;
        base.Awake();
    }

    public override void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, Entity source, bool canCrit, DamageTag[] damageTag) // sourced
    {
        float reducedDamage;
        // passive of the soldier ant, reduces physical damage taken by a flat 2
        if (damageType == DamageType.Physical)
        {
            reducedDamage = Mathf.Max(0, damageDealt -12f);
        }
        else // if its magic or true, ignores the reduction
        {
            reducedDamage = damageDealt;
        }
      
        base.Damage(reducedDamage, damageType, elementalType, source, canCrit, damageTag); // calls up to parent for damage reduction
    }
    public override void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, DamageTag[] damageTag) // non-sourced
    {
        float reducedDamage;
        // passive of the soldier ant, reduces physical damage taken by a flat 2
        if (damageType == DamageType.Physical)
        {
            reducedDamage = Mathf.Max(0, damageDealt -12f);
        }
        else // if its magic or true, ignores the reduction
        {
            reducedDamage = damageDealt;
        }

        base.Damage(reducedDamage, damageType, elementalType, damageTag); // calls up to parent for damage reduction
    }

    public override string GetName() => "<b><color=#8B4513>Soldier Ant</color></b>";

    public override string GetDescription() => $"The {GetName()} is a frontline brawler with high HP and armor.";

    public override string GetPassiveDescription() => "Reduces all incoming physical damage by a flat 12.";
}
