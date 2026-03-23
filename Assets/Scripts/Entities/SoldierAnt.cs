using UnityEngine;

public class SoldierAnt : Ant
{

    protected override void Awake() {
        maxHealth = 40f;
        physicalResistance += 0.15f;
        sunDrop = 50;
        base.Awake();
    }

    public override void Damage(float damageDealt, DamageType damageType)
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
      
        base.Damage(reducedDamage, damageType); // calls up to parent for damage reduction
    }
    }
