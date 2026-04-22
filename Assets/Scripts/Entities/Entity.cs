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

        UpdateHealthBar();

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
        UpdateHealthBar();
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
        SpawnHealthBar();
    }

    //every tick
    protected virtual void Update()
    {
        UpdateStats();
        timeAlive += Time.deltaTime;
        TickEffects();
    }

    // HEALTH BAR

    private Vector3 healthBarOffset = new Vector3(0, 0.6f, 0); // OFFSET
    private GameObject healthBarInstance;
    private Transform healthBarFill;

    private void SpawnHealthBar()
    {
        GameObject healthBarPrefab = Resources.Load<GameObject>("HealthBar");
        if (healthBarPrefab == null)
        {
            Debug.LogWarning("HealthBar prefab not found in Resources folder");
            return;
        }

        healthBarInstance = Instantiate(healthBarPrefab, transform);

        Vector3 offset = healthBarOffset;
        offset.x -= 0.475f;
        healthBarInstance.transform.localPosition = offset;

        healthBarFill = healthBarInstance.transform.Find("Fill");
        healthBarInstance.SetActive(false);
    }
    
    private void UpdateHealthBar()
    {
        if (healthBarFill == null) {
            Debug.Log("healthBarFill is null, bailing");
            return;
        }

        float ratio = Mathf.Clamp01(health/maxHealth);
        Vector3 scale = healthBarFill.localScale;
        scale.x = ratio;
        healthBarFill.localScale = scale;
    }
    
    void OnMouseEnter()
    {
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(true);
        }
    }

    void OnMouseExit()
    {
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(false);
        }
    }

    // STATUS EFFECTS

    public List<StatusEffect> activeEffects = new List<StatusEffect>();

    public void ApplyEffect(StatusEffect effect)
    {
        foreach (StatusEffect existing in activeEffects)
        {
            if (existing.GetType() == effect.GetType())
            {
                if (effect.level > existing.level)
                {
                    existing.level = effect.level;
                    existing.duration = effect.duration;
                    existing.OnApply();
                } else if (effect.duration > existing.duration) 
                {
                    existing.duration = effect.duration;
                }
                return;
            }
        }
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

    // removing all debuffs
    public void RemoveAllDebuffs()
    {
        // start at end of the list then go leftwards
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].effectType == StatusEffect.Type.negative)
            {
                activeEffects[i].OnExpire();
                activeEffects.RemoveAt(i);
            }
        }
    }

    // remove all buffs
    public void RemoveAllBuffs()
    {
        // start at end of the list then go leftwards
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].effectType == StatusEffect.Type.positive)
            {
                activeEffects[i].OnExpire();
                activeEffects.RemoveAt(i);
            }
        }
    }

    // remove one specific effect

    public void RemoveEffect<T>() where T : StatusEffect
    {
        for (int i = activeEffects.Count -1; i>=0 ; i--)
        {
            if (activeEffects[i] is T)
            {
                activeEffects[i].OnExpire();
                activeEffects.RemoveAt(i);
                return;
            }
        }
    }
}
