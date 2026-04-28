using UnityEngine;
using System.Collections.Generic;

public class Snowdrop : Aura
{
    private float 
    bAD = 12f, // base attack damage
    bAS = 0.33f, // base attack speed
    bAR = 1.75f; // base attack range
    private float tickTimer = 0f;
    private const float tickInterval = 0.25f;
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
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
        tickTimer += Time.deltaTime;

        List<Insect> targets = GetInsectsInRange();
        foreach (Insect insect in targets)
        {
        insect.ApplyEffect(new ChillEffect(insect, 0.25f, 1, this));

        if (tickTimer >= tickInterval)
        {
            insect.Damage(attackDamage * tickInterval, DamageType.Magic, ElementalType.Ice, this, false, new DamageTag [] {DamageTag.AoE, DamageTag.DoT});
        }
        }

        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
        }

    }

    public override void LevelUp()
    {
        base.LevelUp();
        int perLevel = (level - 1);
        baseAttackDamage = bAD + (perLevel * 0.8f);
        baseAttackSpeed = bAS + (perLevel * 0.03f);
        baseAttackRange = bAR + (perLevel * 0.2f);
    }

    protected override void Attack()
    {

        base.Attack();
    }
}
