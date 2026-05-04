using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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
            insect.Damage(attackDamage * tickInterval, damageType, elementalType, this, false, new DamageTag [] {DamageTag.AoE, DamageTag.DoT});
        }
        }

        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
        }

    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = bAD + (level * 1f);
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
        return $"Silence falls wherever the {GetName()} blooms. Insects that wander too close grow cold.";
    }

    public override string GetAttackDescription()
    {
        return $"Radiates a frosty aura continuously dealing trickling <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage to insects around her";
    }

    public override string GetSkillDesription()
    {
        return $"idk yet gang sry";
    }

    public override string GetPassiveDescription()
    {
        return $"The frosty aura applies a <color=#00FFFF>Chill</color> effect, slowing down insects.";
    }
    public override string GetPath1Name()
    {
      return "";  
    }

    public override string GetPath1Description()
    {
        return "Path 1:\n\nIncrease Attack Damage by <b><color=green>0.5</color></b> per level. [<b><color=green>+" + (1f * path1Level) + "</color></b>]\n" +
           "Increase Attack Range by <b><color=green>0.1</color></b> per level. [<b><color=green>+" + (0.1f * path1Level) + "</color></b>]\n\n" +
           "Level: [<color=green><b>" + path1Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath1Level-path1Level) + ")</b></color>";
    }
    public override string GetPath2Name()
    {
      return "";  
    }

    public override string GetPath2Description()
    {
       return "Increase <color=#00FFFF>Chill</color> level by <color=green><b>1</b></color> per level. [<color=green><b>+" + (1*effectivePath2Level) + "</b></color>]\n\n" +
    "<color=#00FFFF>Chill</color>\nSlow: 24% + 6% per level [<color=green><b>" + (24 + 6 * effectivePath2Level) + "%</b></color>]\nCurrent Level: [<color=green><b>" + effectivePath2Level + "</b></color>]\n\n" +
    "Level: [<color=green><b>" + path2Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath2Level-path2Level) + ")</b></color>";
    }
    public override string GetPath3Name()
    {
        return "";
    }

    public override string GetPath3Description()
    {
      return "";  
    }
}
