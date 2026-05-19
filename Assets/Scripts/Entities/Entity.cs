using UnityEngine;
using System.Collections.Generic;

public enum DamageType
{
    Physical,
    Magic,
    True,
    Environmental
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
    PassiveDamage,
    SkillDamage,
    ElementalDebuff,
    Coordinated,
    Counter
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

    [Header("Base Stats")]
    public float baseMaxHealth, baseAttackDamage, baseMagicPower, baseAttackSpeed, baseAttackRange, baseHealingBonus, baseHealingReceived;
    public float basePhysicalResistance, baseMagicResistance;
    public float baseFireResistance, baseWaterResistance, baseNatureResistance, baseWindResistance, basePoisonResistance, baseIceResistance;
    public float basePhysicalShred, baseMagicShred, baseBonusEffectChance;
    public float baseFireDamage, baseWaterDamage, baseNatureDamage, baseWindDamage, basePoisonDamage, baseIceDamage;
    public float baseCriticalChance, baseCriticalDamage;
    public float baseDotResistance, baseDotDamage;
    public float baseElementalPower;
    public float basePassiveDamage, baseSkillDamage, baseCoordinatedDamage;
    public float baseSkillDuration;
    public float baseTenacity;
    public float baseLightEmissionRange;
    public float baseLifesteal;
    public float baseCounterDamage;

    [Header("Stats")]
    public float maxHealth, health, attackDamage, magicPower, attackSpeed, attackCooldown, attackCooldownTimer, attackRange, healingBonus, healingReceived;
    public float physicalResistance, magicResistance;
    public float fireResistance, waterResistance, natureResistance, windResistance, poisonResistance, iceResistance;
    public float physicalShred, magicShred, bonusEffectChance;
    public float fireDamage, waterDamage, natureDamage, windDamage, poisonDamage, iceDamage;
    public float criticalChance, criticalDamage;
    public float dotResistance, dotDamage;
    public float elementalPower;
    public float passiveDamage, skillDamage, coordinatedDamage;
    public float skillDuration;
    public float lightEmissionRange;
    public float lifesteal;
    public float counterDamage;
    public float tenacity;
    public bool debuffsFrozen;

    [Header("Stat Adders")]
    public float maxHealthAdder, attackDamageAdder, magicPowerAdder, attackSpeedAdder, attackRangeAdder, healingBonusAdder, healingReceivedAdder;
    public float physicalResistanceAdder, magicResistanceAdder;
    public float fireResistanceAdder, waterResistanceAdder, natureResistanceAdder, windResistanceAdder, poisonResistanceAdder, iceResistanceAdder;
    public float physicalShredAdder, magicShredAdder, bonusEffectChanceAdder;
    public float fireDamageAdder, waterDamageAdder, natureDamageAdder, windDamageAdder, poisonDamageAdder, iceDamageAdder;
    public float criticalChanceAdder, criticalDamageAdder;
    public float dotResistanceAdder, dotDamageAdder;
    public float elementalPowerAdder;
    public float passiveDamageAdder, skillDamageAdder, coordinatedDamageAdder;
    public float skillDurationAdder;
    public float tenacityAdder, immobilizeDurationAdder;
    public float lightEmissionRangeAdder;
    public float lifestealAdder;
    public float counterDamageAdder;

    [Header("Stat Multipliers")]
    public float maxHealthMultiplier, attackDamageMultiplier, magicPowerMultiplier, attackSpeedMultiplier, attackRangeMultiplier, healingBonusMultiplier, healingReceivedMultiplier;
    public float physicalResistanceMultiplier, magicResistanceMultiplier;
    public float fireResistanceMultiplier, waterResistanceMultiplier, natureResistanceMultiplier, windResistanceMultiplier, poisonResistanceMultiplier, iceResistanceMultiplier;
    public float physicalShredMultiplier, magicShredMultiplier, bonusEffectChanceMultiplier;
    public float fireDamageMultiplier, waterDamageMultiplier, natureDamageMultiplier, windDamageMultiplier, poisonDamageMultiplier, iceDamageMultiplier;
    public float criticalChanceMultiplier, criticalDamageMultiplier;
    public float dotResistanceMultiplier, dotDamageMultiplier;
    public float elementalPowerMultiplier;
    public float passiveDamageMultiplier, coordinatedDamageMultiplier;
    public float skillDurationMultiplier;
    public float tenacityMultiplier, immobilizeDurationMultiplier;
    public float lightEmissionRangeMultiplier;
    public float lifestealMultiplier;
    public float counterDamageMultiplier;

    [Header("Internal Cooldowns")]
    public float internalCooldown = 1f, blazeInternalCooldown, wetInternalCooldown, sproutInternalCooldown, coldInternalCooldown, taintedInternalCooldown, freezeInternalCooldown, germinateInternalCooldown;

    [Header("Debug")]
    public float timeAlive, totalDamageDealt;
    public virtual void UpdateStats()
    {
        maxHealth = baseMaxHealth + maxHealthAdder + (baseMaxHealth * maxHealthMultiplier);
        physicalResistance = basePhysicalResistance + physicalResistanceAdder + (basePhysicalResistance * physicalResistanceMultiplier);
        magicResistance = baseMagicResistance + magicResistanceAdder + (baseMagicResistance * magicResistanceMultiplier);
        attackDamage = baseAttackDamage + attackDamageAdder + (baseAttackDamage * attackDamageMultiplier);
        magicPower = baseMagicPower + magicPowerAdder + (baseMagicPower * magicPowerMultiplier);
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
        dotDamage = baseDotDamage + dotDamageAdder + (baseDotDamage * dotDamageMultiplier);
        elementalPower = baseElementalPower + elementalPowerAdder + (baseElementalPower * elementalPowerMultiplier);
        passiveDamage = basePassiveDamage + passiveDamageAdder + (basePassiveDamage * passiveDamageMultiplier);
        skillDamage = baseSkillDamage + skillDamageAdder;
        coordinatedDamage = baseCoordinatedDamage + coordinatedDamageAdder + (baseCoordinatedDamage * coordinatedDamageMultiplier);
        skillDuration = baseSkillDuration + skillDurationAdder + (baseSkillDuration * skillDurationMultiplier);
        tenacity = baseTenacity + tenacityAdder + (baseTenacity * tenacityMultiplier);
        lightEmissionRange = baseLightEmissionRange + lightEmissionRangeAdder + (baseLightEmissionRange * lightEmissionRangeMultiplier);
        lifesteal = baseLifesteal + lifestealAdder + (baseLifesteal * lifestealMultiplier);
        counterDamage = baseCounterDamage + counterDamageAdder + (baseCounterDamage * counterDamageMultiplier);
    }

    public virtual void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, DamageTag[] damageTag)
    {
        float modifiedDamage, elementalMultiplier, finalDamage, dotMultiplier;
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

        if (System.Array.Exists(damageTag, t => t == DamageTag.DoT))
        {
            dotMultiplier = 1 - dotResistance;
        } else
        {
            dotMultiplier = 1;
        }
        
        finalDamage = (modifiedDamage * elementalMultiplier * dotMultiplier);
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
            insect.RegisterAttacker(plant);
        }

        float modifiedDamage, elementalMultiplier, finalDamage, elementalDebuffDuration = 6f, dotMultiplier, elementalDebuffMultiplier, passiveDamageMult, skillDamageMult, coordinatedDamageMult, counterDamageMult;
        bool isCrit = false;

        if (this.HasEffect<BrittleEffect>() && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            Damage(3, damageType, ElementalType.Ice, source, false, new DamageTag [] {DamageTag.ElementalDebuff});
        }

        if (this.HasEffect<FractureEffect>() && damageType == DamageType.Physical && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            Damage(damageDealt * 0.25f, DamageType.Physical, ElementalType.Fire, source, false, new DamageTag[] { DamageTag.ElementalDebuff });
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

                if (this.HasEffect<GerminateEffect>())
                RemoveEffect<GerminateEffect>();
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
            if (this is Insect windInsect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
                {
                    if (windInsect.HasEffect<BlazeEffect>() || windInsect.HasEffect<ColdEffect>() ||
                        windInsect.HasEffect<WetEffect>() || windInsect.HasEffect<TaintedEffect>() ||
                        windInsect.HasEffect<SproutEffect>())
                    {
                        ApplyEffect(new GustEffect(this, 0.5f, 1, source));
                    }
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
            dotMultiplier = 1 - dotResistance + source.dotDamage;
        } else
        {
            dotMultiplier = 1;
        }

        if (System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            elementalDebuffMultiplier = 1 + source.elementalPower;
        } else
        {
            elementalDebuffMultiplier = 1;
        }

        if (System.Array.Exists(damageTag, t => t == DamageTag.PassiveDamage))
        {
            passiveDamageMult = 1 + source.passiveDamage;
        } else
        {
            passiveDamageMult = 1;
        }

        if (System.Array.Exists(damageTag, t => t == DamageTag.SkillDamage))
        {
            skillDamageMult = 1 + source.skillDamage;
        } else
        {
            skillDamageMult = 1;
        }

        if (System.Array.Exists(damageTag, t => t == DamageTag.Coordinated))
        {
            coordinatedDamageMult = 1 + source.coordinatedDamage;
        } else
        {
            coordinatedDamageMult = 1;
        }

        if (System.Array.Exists(damageTag, t => t == DamageTag.Counter))
        {
            counterDamageMult = 1 + source.counterDamage;
        } else
        {
            counterDamageMult = 1;
        }

        finalDamage = modifiedDamage * elementalMultiplier * dotMultiplier * elementalDebuffMultiplier * passiveDamageMult * skillDamageMult * coordinatedDamageMult * counterDamageMult;

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
        if (this is Insect damagedInsect) damagedInsect.lastSource = source;
        if (source.lifesteal > 0f) source.Heal(finalDamage * source.lifesteal);
        if (this is Plant poisonPlant && poisonPlant.elementalType == ElementalType.Poison
            && System.Array.Exists(damageTag, t => t == DamageTag.Attack))
        {
            source.Damage(damageDealt * 2f, DamageType.Physical, ElementalType.Poison, poisonPlant, false, new DamageTag[] { DamageTag.Counter });
        }

        // damage indicator

        GameObject indicator = Instantiate(damageIndicatorPrefab, GetIndicatorPosition(), Quaternion.identity);
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
        float actual = Mathf.Min(healingAmount * (1f + healingReceived), maxHealth - health);
        if (actual <= 0f) return;
        health += actual;
        UpdateHealthBar();
        GameObject indicator = Instantiate(damageIndicatorPrefab, GetIndicatorPosition(), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>()?.Initialize($"+{actual:F0}", new Color(0.2f, 1f, 0.2f));
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

    protected virtual Vector3 GetIndicatorPosition()
    {
        return transform.position + Vector3.up * 0.25f;
    }

// upon spawning, occurs before Start()
    protected virtual void Awake()
    {
        damageIndicatorPrefab = Resources.Load<GameObject>("DamageIndicator");
        UpdateStats();
        health = maxHealth;
        if (this is Insect) SpawnHealthBar();
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

        if (taintedInternalCooldown > 0)
        {
            taintedInternalCooldown -= Time.deltaTime;
        }

        if (freezeInternalCooldown > 0)
            freezeInternalCooldown -= Time.deltaTime;

        if (germinateInternalCooldown > 0)
            germinateInternalCooldown -= Time.deltaTime;
    }

    // HEALTH BAR

    protected Vector3 healthBarOffset = new Vector3(0, 0.6f, 0); // OFFSET
    protected GameObject healthBarInstance;
    private Transform healthBarFill;

    private static GameObject _healthBarPrefab;

    protected void SpawnHealthBar()
    {
        if (_healthBarPrefab == null)
            _healthBarPrefab = Resources.Load<GameObject>("HealthBar");
        GameObject healthBarPrefab = _healthBarPrefab;
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
        if (healthBarFill == null) return;

        float ratio = Mathf.Clamp01(health/maxHealth);
        Vector3 scale = healthBarFill.localScale;
        scale.x = ratio;
        healthBarFill.localScale = scale;

        if (health < maxHealth && healthBarInstance != null)
            healthBarInstance.SetActive(true);
    }

    public void ShowHealthBar()
    {
        healthBarInstance?.SetActive(true);
    }

    public void RefreshHealthBarVisibility()
    {
        if (healthBarInstance == null) return;
        healthBarInstance.SetActive(health < maxHealth);
    }
    
    void OnMouseEnter()
    {
        if (healthBarInstance != null)
            healthBarInstance.SetActive(true);
        OnHover();
    }

    void OnMouseExit()
    {
        OnHoverExit();
    }

    protected virtual void OnHover() {}
    protected virtual void OnHoverExit()
    {
        if (healthBarInstance != null)
            healthBarInstance.SetActive(false);
    }

    // STATUS EFFECTS

    public List<StatusEffect> activeEffects = new List<StatusEffect>();

    public virtual void ApplyEffect(StatusEffect effect)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].GetType() == effect.GetType())
            {
                activeEffects[i].OnExpire();
                activeEffects.RemoveAt(i);
                break;
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

    public T GetEffect<T>() where T : StatusEffect
    {
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect is T typedEffect)
                return typedEffect;
        }
        return null;
    }

// tick effects of the status effect. starts at end of the list
// checks until head, if duration is under or equal to 0, executes isExpired
// and then removes it from the list
    private void TickEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];
            bool durationFrozen = debuffsFrozen && effect.effectType == StatusEffect.Type.negative;

            effect.OnTick(Time.deltaTime);
            if (!durationFrozen)
                effect.duration -= Time.deltaTime;

            if (effect.IsExpired())
            {
                effect.OnExpire();
                activeEffects.RemoveAt(i);
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
