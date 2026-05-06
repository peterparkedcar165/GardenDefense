using UnityEngine;
using System.Collections.Generic;

public enum DamageType
{
    Physical,
    Magic,
    True
}

public enum DamageTag
{
    Projectile,
    SingleTarget,
    // MultiTarget,
    AoE,
    DoT,
    Melee,
    Attack,
    Skill,
    ElementalDebuff
    // Coordinated,
    // IgnoresPhysicalResistance,
    // IgnoresMagicResistance,
    // IgnoresIceResistance,
    // IgnoresNatureResistance,
    // IgnoresFireResistance,
    // IgnoresWaterResistance,
    // IgnoresWindResistance
}

public enum ElementalType
{
    Fire, Water, Nature, Poison, Ice, Wind, Neutral
}

public abstract class Entity : MonoBehaviour
{
    private GameObject damageIndicatorPrefab;

    // STATS A BIT MESSY BUT WILL CLEAN EVENTUALLY
    // bonus
    public float physicalShredAdder, physicalShredMultiplier, 
    magicShredAdder, magicShredMultiplier, 
    bonusEffectChanceAdder, bonusEffectChanceMultiplier,
    fireDamageAdder, fireDamageMultiplier,
    waterDamageAdder, waterDamageMultiplier,
    natureDamageAdder, natureDamageMultiplier,
    windDamageAdder, windDamageMultiplier,
    poisonDamageAdder, poisonDamageMultiplier,
    iceDamageAdder,iceDamageMultiplier,
    criticalChanceAdder, criticalChanceMultiplier,
    criticalDamageAdder, criticalDamageMultiplier;

    public float baseMaxHealth, basePhysicalResistance, baseMagicResistance, baseAttackDamage, baseMagicDamage, baseAttackSpeed, baseAttackRange, baseHealingBonus = 0, baseHealingReceived = 0;
    public float maxHealthAdder, physicalResistanceAdder, magicResistanceAdder, attackDamageAdder, magicDamageAdder, attackSpeedAdder, attackRangeAdder, healingBonusAdder, healingReceivedAdder;
    public float maxHealthMultiplier, physicalResistanceMultiplier, magicResistanceMultiplier, attackDamageMultiplier, magicDamageMultiplier, attackSpeedMultiplier, attackRangeMultiplier, healingBonusMultiplier, healingReceivedMultiplier;
    public float maxHealth, health, physicalResistance, magicResistance, attackDamage, magicDamage, attackSpeed, attackCooldown, attackCooldownTimer, attackRange, healingBonus, healingReceived;

    public float baseFireResistance, fireResistance, fireResistanceAdder, fireResistanceMultiplier,
    baseWaterResistance, waterResistance, waterResistanceAdder, waterResistanceMultiplier,
    baseNatureResistance, natureResistance, natureResistanceAdder, natureResistanceMultiplier,
    baseWindResistance, windResistance, windResistanceAdder, windResistanceMultiplier,
    basePoisonResistance, poisonResistance, poisonResistanceAdder, poisonResistanceMultiplier,
    baseIceResistance, iceResistance, iceResistanceAdder, iceResistanceMultiplier;    
    
    public float basePhysicalShred, physicalShred, 
    baseMagicShred, magicShred, baseBonusEffectChance, bonusEffectChance,baseFireDamage, fireDamage,baseWaterDamage, waterDamage, baseNatureDamage, natureDamage,baseWindDamage, windDamage,basePoisonDamage, poisonDamage,baseIceDamage, iceDamage, baseCriticalChance, criticalChance, baseCriticalDamage, criticalDamage,
    baseDotResistance, dotResistance, dotResistanceAdder, dotResistanceMultiplier;    
    
    public float timeAlive, totalDamageDealt; // leaving it public jsut so i can debug, but shgould be private

    public float internalCooldown = 2f, blazeInternalCooldown, wetInternalCooldown, sproutInternalCooldown, coldInternalCooldown, gustInternalCooldown, taintedInternalCooldown, freezeInternalCooldown, germinateInternalCooldown;
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
        fireResistance = baseFireResistance + fireResistanceAdder + (baseFireResistance * fireResistanceMultiplier);
        waterResistance = baseWaterResistance + waterResistanceAdder + (baseWaterResistance * waterResistanceMultiplier);
        natureResistance = baseNatureResistance + natureResistanceAdder + (baseNatureResistance * natureResistanceMultiplier);
        windResistance = baseWindResistance + windResistanceAdder + (baseWindResistance * windResistanceMultiplier);
        poisonResistance = basePoisonResistance + poisonResistanceAdder + (basePoisonResistance * poisonResistanceMultiplier);
        iceResistance = baseIceResistance + iceResistanceAdder + (baseIceResistance * iceResistanceMultiplier);
        physicalShred = basePhysicalShred + physicalShredAdder + (basePhysicalShred * physicalShredMultiplier);
        magicShred = baseMagicShred + magicShredAdder + (baseMagicShred * magicShredMultiplier);
        bonusEffectChance = baseBonusEffectChance + bonusEffectChanceAdder + (baseBonusEffectChance * bonusEffectChanceMultiplier);
        fireDamage = baseFireDamage + fireDamageAdder + (baseFireDamage * fireDamageMultiplier);
        waterDamage = baseWaterDamage + waterDamageAdder + (baseWaterDamage * waterDamageMultiplier);
        natureDamage = baseNatureDamage + natureDamageAdder + (baseNatureDamage * natureDamageMultiplier);
        windDamage = baseWindDamage + windDamageAdder + (baseWindDamage * windDamageMultiplier);
        poisonDamage = basePoisonDamage + poisonDamageAdder + (basePoisonDamage * poisonDamageMultiplier);
        iceDamage = baseIceDamage + iceDamageAdder + (baseIceDamage * iceDamageMultiplier);
        criticalChance = baseCriticalChance + criticalChanceAdder + (baseCriticalChance * criticalChanceMultiplier);
        criticalDamage = baseCriticalDamage + criticalDamageAdder + (baseCriticalDamage * criticalDamageMultiplier);
        dotResistance = baseDotResistance + dotResistanceAdder + (baseDotResistance * dotResistanceMultiplier);
    }

    public virtual void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, DamageTag[] damageTag)
    {
        float modifiedDamage, elementalMultiplier, finalDamage;
        switch (elementalType)
        {
            case ElementalType.Fire:
            elementalMultiplier = (1 - this.fireResistance);
            break;
            case ElementalType.Water:
            elementalMultiplier = (1 - this.waterResistance);
            break;
            case ElementalType.Ice:
            elementalMultiplier = (1 - this.iceResistance);
            break;
            case ElementalType.Wind:
            elementalMultiplier = (1 - this.windResistance);
            break;
            case ElementalType.Nature:
            elementalMultiplier = (1 - this.natureResistance);
            break;
            case ElementalType.Poison:
            elementalMultiplier = (1 - this.poisonResistance);
            break;
            default:
            elementalMultiplier = 1;
            break;
        }

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
        
        finalDamage = (modifiedDamage * elementalMultiplier);
        health -= finalDamage;

        UpdateHealthBar();

        if (health <= 0)
        {
            Kill();
        }
    }

    public virtual void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, Entity source, bool canCrit, DamageTag[] damageTag) // damage with source
    {
        if (this is Insect insect && source is Plant plant) // if target = insect and source = plant
        {
            insect.RegisterAttacker(plant); // register plant into insect's hashset of attackers for exp distribution
        }

        float modifiedDamage, elementalMultiplier, finalDamage, elementalDebuffDuration = 6f, dotMultiplier;
        bool isCrit = false;

        if (this.HasEffect<BrittleEffect>() && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            Damage(2, damageType, ElementalType.Ice, source, false, new DamageTag [] {DamageTag.ElementalDebuff});
        }

        switch (elementalType)
        {
            case ElementalType.Fire:
            elementalMultiplier = (1 - this.fireResistance + source.fireDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && this.blazeInternalCooldown <= 0)
                {
                    blazeInternalCooldown = internalCooldown;
                    ApplyEffect(new BlazeEffect(this, elementalDebuffDuration, 1, source));
                }
            break;

            case ElementalType.Water:
            elementalMultiplier = (1 - this.waterResistance + source.waterDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && this.wetInternalCooldown <= 0)
                {
                    wetInternalCooldown = internalCooldown;
                    ApplyEffect(new WetEffect(this, elementalDebuffDuration, 1, source));
                }
            break;

            case ElementalType.Ice:
            elementalMultiplier = (1 - this.iceResistance + source.iceDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && this.coldInternalCooldown <= 0)
                {
                    coldInternalCooldown = internalCooldown;
                    ApplyEffect(new ColdEffect(this, elementalDebuffDuration, 1, source));
                }
            break;

            case ElementalType.Wind:
            elementalMultiplier = (1 - this.windResistance + source.windDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && this.gustInternalCooldown <= 0)
                {
                    gustInternalCooldown = internalCooldown;
                    ApplyEffect(new GustEffect(this, elementalDebuffDuration, 1, source));
                }
            break;

            case ElementalType.Nature:
            elementalMultiplier = (1 - this.natureResistance + source.natureDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && this.sproutInternalCooldown <= 0)
                {
                    sproutInternalCooldown = internalCooldown;
                    ApplyEffect(new SproutEffect(this, elementalDebuffDuration, 1, source));
                }
            break;

            case ElementalType.Poison:
            elementalMultiplier = (1 - this.poisonResistance + source.poisonDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && this.taintedInternalCooldown <= 0)
                {
                    taintedInternalCooldown = internalCooldown;
                    ApplyEffect(new TaintedEffect(this, elementalDebuffDuration, 1, source));
                }
            break;

            default:
            elementalMultiplier = 1;
            break;
        }

        switch (damageType)
        {
            case DamageType.Physical:
            modifiedDamage = damageDealt * (1 - physicalResistance + source.physicalShred);
            break;
            case DamageType.Magic:
            modifiedDamage = damageDealt * (1 - magicResistance + source.magicShred);
            break;
            default:
            modifiedDamage = damageDealt;
            break;
        }

        if (System.Array.Exists(damageTag, t => t == DamageTag.DoT))
        {
            dotMultiplier = 1 - dotResistance;
        } else
        {
            dotMultiplier = 1;
        }

        finalDamage = (modifiedDamage * elementalMultiplier * dotMultiplier);

        // if damage source can crit, then calculate if it crits or not
        if (canCrit)
        {
            if (Random.value < source.criticalChance)
            {
                finalDamage *= source.criticalDamage;
                isCrit = true; // important for the damage indicator
                // Debug.Log("CRITICAL HIT");
            }
        }
        
        health -= finalDamage;
        source.totalDamageDealt += finalDamage; // FOR DEBUG

        // damage indicator

        GameObject indicator = Instantiate(damageIndicatorPrefab, this.transform.position + Vector3.up * 0.25f, Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize(finalDamage, elementalType, isCrit);


        UpdateHealthBar();

        if (health <= 0)
        {
            Kill(source);
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

    public virtual void Kill(Entity source) // death including source
    {
        Destroy(gameObject);
    }

// upon spawning, occurs before Start()
    protected virtual void Awake()
    {
        damageIndicatorPrefab = Resources.Load<GameObject>("DamageIndicator");
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

        if (blazeInternalCooldown > 0)
        {
            blazeInternalCooldown -= Time.deltaTime;
        }

        if (wetInternalCooldown > 0)
        {
            wetInternalCooldown -= Time.deltaTime;
        }

        if (sproutInternalCooldown > 0)
        {
            sproutInternalCooldown -= Time.deltaTime;
        }

        if (coldInternalCooldown > 0)
        {
            coldInternalCooldown -= Time.deltaTime;
        }

        if (gustInternalCooldown > 0)
        {
            gustInternalCooldown -= Time.deltaTime;
        }

        if (taintedInternalCooldown > 0)
        {
            taintedInternalCooldown -= Time.deltaTime;
        }

        if (freezeInternalCooldown > 0)
        {
            freezeInternalCooldown -= Time.deltaTime;
        }

        if (germinateInternalCooldown > 0)
        {
            germinateInternalCooldown -= Time.deltaTime;
        }
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
            healthBarInstance.SetActive(true);
        OnHover();
    }

    void OnMouseExit()
    {
        if (healthBarInstance != null)
            healthBarInstance.SetActive(false);
        OnHoverExit();
    }

    protected virtual void OnHover() {}
    protected virtual void OnHoverExit() {}

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
                    existing.OnExpire();
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
