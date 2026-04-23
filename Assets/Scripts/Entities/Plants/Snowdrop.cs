using UnityEngine;
using System.Collections.Generic;

public class Snowdrop : Aura
{
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = 2f;
        baseAttackSpeed = 0.33f;
        baseAttackRange = 2f;
    }


    protected override void Update()
    {
        base.Update();

        if (attackCooldownTimer < attackCooldown)
        {
            attackCooldownTimer += Time.deltaTime;
        }
        else
        {
           // if there is at least one insect within range, attack.
           // ONLY WHEN PASSIVE IS UNLOCKED
           // Attack(GetInsectsInRange());
        }
        
        // passive damage tick
        List<Insect> targets = GetInsectsInRange();
        foreach (Insect insect in targets)
        {
        insect.Damage(attackDamage * Time.deltaTime, DamageType.Magic, ElementalType.Ice, this, false);

        insect.ApplyEffect(new ChillEffect(insect, 0.25f, 1, this));

        }

    }

    public override void LevelUp()
    {
        base.LevelUp();
        int perLevel = (level - 1);
        baseAttackDamage = 2f + (perLevel * 0.2f);
        baseAttackSpeed = 0.33f + (perLevel * 0.03f);
        baseAttackRange = 3f + (perLevel * 0.2f);
    }

    protected override void Attack()
    {

        base.Attack();
    }
}
