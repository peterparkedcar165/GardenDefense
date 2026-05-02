using UnityEngine;
using System.Collections.Generic;

public class Snowdrop : Aura
{
    private float 
    bAD = 9f, // base attack damage
    bAS = 0.33f, // base attack speed
    bAR = 1.75f; // base attack range
    private float tickTimer = 0f;
    public float activeDuration = 5f;
    private const float tickInterval = 0.25f;
    public int chillLevel = 1;
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        activeCooldown = 32f;
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
        insect.ApplyEffect(new ChillEffect(insect, 0.25f, chillLevel, this));

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

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = bAD + (level * 0.5f);
        baseAttackRange = bAR + (level * 0.1f);
    }

    public override void OnPath2Upgrade(int level)
    {
        chillLevel = 1 + (level);
    }

    public override void OnPath3Upgrade(int level)
    {
        activeDuration = 5f + 1*(level - 1);
        activeCooldown = 32f - 2f*(level);
    }

    protected override void Attack()
    {

        base.Attack();
    }

    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=#00FFFF>Snowdrop</color></b>";
    }

    public override string GetDescription()
    {
        return $"Silence falls wherever the {GetName()} blooms. Insects that wander too close find themselves freezing, and wondering what went wrong.";
    }

    public override string GetAttackDescription()
    {
        return $"The {GetName()} radiates a frosty aura continuously dealing <color=cyan>Ice</color> <color=pink>Magic</color> damage to insects around her, and applying a <color=cyan>Chill</color> effect, which slows them down.";
    }

    public override string GetSkillDesription()
    {
        return $"The {GetName()} opens up her petals, increasing her effect radius, and the intensity of <color=cyan>Chill</color> effect.";
    }

    public override string GetPassiveDescription()
    {
        return $"The {GetName()} unleashes cold gusts that <color=cyan>Freezes</color> currently <color=cyan>Chilled</color> targets.";
    }
}
