using UnityEngine;
using System.Collections.Generic;

public enum DamageType
{
    Physical,
    Magic,
    True
}

public abstract class Entity : MonoBehaviour
{

    public float baseMaxHealth, basePhysicalResistance, baseMagicResistance, baseAttackDamage, baseMagicDamage, baseAttackSpeed, baseAttackRange, baseHealingBonus = 0, baseHealingReceived = 0;
    protected float maxHealthAdder, physicalResistanceAdder, magicResistanceAdder, attackDamageAdder, magicDamageAdder, attackSpeedAdder, attackRangeAdder, healingBonusAdder, healingReceivedAdder;
    protected float maxHealthMultiplier, physicalResistanceMultiplier, magicResistanceMultiplier, attackDamageMultiplier, magicDamageMultiplier, attackSpeedMultiplier, attackRangeMultiplier, healingBonusMultiplier, healingReceivedMultiplier;
    public float maxHealth, health, physicalResistance, magicResistance, attackDamage, magicDamage, attackSpeed, attackCooldown, attackCooldownTimer, attackRange, healingBonus, healingReceived;

    public float timeAlive; // leaving it public jsut so i can debug, but shgould be private

    protected virtual void UpdateStats()
    {
        maxHealth = baseMaxHealth + maxHealthAdder + (baseMaxHealth * maxHealthMultiplier);
        physicalResistance = basePhysicalResistance + physicalResistanceAdder + (basePhysicalResistance * physicalResistanceMultiplier);
        magicResistance = baseMagicResistance + magicResistanceAdder + (baseMagicResistance * magicResistanceMultiplier);
        attackDamage = baseAttackDamage + attackDamageAdder + (baseAttackDamage * attackDamageMultiplier);
        magicDamage = baseMagicDamage + magicDamageAdder + (baseMagicDamage * magicDamageMultiplier);
        attackSpeed = baseAttackSpeed + attackSpeedAdder + (baseAttackSpeed * attackSpeedMultiplier);
        attackRange = baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier);
        healingBonus = baseHealingBonus + healingBonusAdder + (baseHealingBonus * healingBonusMultiplier);
        healingReceived = baseHealingReceived + healingReceivedAdder + (baseHealingReceived * healingReceivedMultiplier);
    }

    public virtual void Damage(float damageDealt, DamageType damageType)
    {
        float modifiedDamage;

        switch (damageType)
        {
            case DamageType.Physical:
            modifiedDamage = damageDealt * (1 - physicalResistance);
            break;
            case DamageType.Magic:
            modifiedDamage = damageDealt * (1 - magicResistance);
            break;
            default:
            modifiedDamage = damageDealt;
            break;
        }

        health -= modifiedDamage;
        if (health <= 0)
        {
            Kill();
        }
    }

// method for healing
    public virtual void Heal(float healingAmount)
    {
        health += healingAmount * healingReceived;
        if (health >= maxHealth)
        {
            health = maxHealth;
        }
    }

// method for death
    public virtual void Kill()
    {
        Destroy(gameObject);
    }

// upon spawning, occurs before Start()
    protected virtual void Awake()
    {
        UpdateStats();
        health = maxHealth;
    }

    //every tick
    protected virtual void Update()
    {
        UpdateStats();
        timeAlive += Time.deltaTime;
        TickEffects();
    }

    // STATUS EFFECTS

    public List<StatusEffect> activeEffects = new List<StatusEffect>();

    public void ApplyEffect(StatusEffect effect)
    {
        activeEffects.Add(effect);
        effect.OnApply();
    }

    public bool HasEffect<T>() where T : StatusEffect
    {
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect is T)
            {
                return true;
            }
        }
        return false;
    }

    public int GetEffectLevel<T>() where T : StatusEffect
    {
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect is T typedEffect)
            {
                return typedEffect.level;
            }
        }
        return 0; // no effect found
    }

// tick effects of the status effect. starts at end of the list
// checks until head, if duration is under or equal to 0, executes isExpired
// and then removes it from the list
    private void TickEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].Tick(Time.deltaTime);
            if (activeEffects[i].IsExpired())
            {
                activeEffects[i].OnExpire();
                activeEffects.Remove(activeEffects[i]);
            }
        }
    }
}
