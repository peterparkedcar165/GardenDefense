using UnityEngine;

public class SoldierAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 3;
        baseMaxHealth = 40f;
        basePhysicalResistance = 0.15f;
        sunDrop = 35;
        base.Awake();
    }

    public override void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, Entity source) // sourced
    {
        float reducedDamage;
        // passive of the soldier ant, reduces physical damage taken by a flat 2
        if (damageType == DamageType.Physical)
        {
            reducedDamage = Mathf.Max(0, damageDealt -2f);
        }
        else // if its magic or true, ignores the reduction
        {
            reducedDamage = damageDealt;
        }
      
        base.Damage(reducedDamage, damageType, elementalType, source); // calls up to parent for damage reduction
    }
    public override void Damage(float damageDealt, DamageType damageType, ElementalType elementalType) // non-sourced
    {
        float reducedDamage;
        // passive of the soldier ant, reduces physical damage taken by a flat 2
        if (damageType == DamageType.Physical)
        {
            reducedDamage = Mathf.Max(0, damageDealt -2f);
        }
        else // if its magic or true, ignores the reduction
        {
            reducedDamage = damageDealt;
        }
      
        base.Damage(reducedDamage, damageType, elementalType); // calls up to parent for damage reduction
    }
}
