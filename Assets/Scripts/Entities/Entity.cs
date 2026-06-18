using UnityEngine;
using System.Collections;
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
    MinionAttack,
    PassiveDamage,
    SkillDamage,
    ElementalDebuff,
    Coordinated,
    Counter,
    Weather,
    BypassShield,
    Germinate,
    Brittle,
    SpecialCanCrit
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

public struct EntityEventData
{
    public Entity target;
    public Entity source;
    public Vector3 position;
    public float damage;
    public float amount;
    public DamageType damageType;
    public ElementalType elementalType;
    public DamageTag[] tags;
}

public abstract class Entity : MonoBehaviour
{
    private SpriteRenderer _flashRenderer;
    private Material _originalMaterial;
    private Material _flashMaterial;
    private Coroutine _flashCoroutine;

    [Header("Base Stats")]
    public float baseMaxHealth, baseAttackDamage, baseMagicPower, baseAttackSpeed, baseAttackRange, baseHealingBonus, baseHealingReceived;
    public int baseArmor, baseMagicArmor;
    public int baseArmorPenFlat, baseMagicPenFlat;
    public float baseArmorPenPercent, baseMagicPenPercent;
    public float baseFireResistance, baseWaterResistance, baseNatureResistance, baseWindResistance, basePoisonResistance, baseIceResistance;
    public float basePhysicalDamage, baseMagicDamage, baseFallDamage, baseBonusEffectChance;
    public float baseFireDamage, baseWaterDamage, baseNatureDamage, baseWindDamage, basePoisonDamage, baseIceDamage;
    public float baseCriticalChance, baseCriticalDamage;
    public float baseDotResistance, baseDotDamage, baseFallDamageResistance;
    public float baseelementalAffinity;
    public float basePassiveDamage, baseSkillDamage, baseCoordinatedDamage;
    public float baseSkillDuration;
    public float baseTenacity;
    public float baseLightEmissionRange;
    public float baseLifesteal;
    public float baseCounterDamage;
    public float baseDebuffGivenDuration, baseBuffGivenDuration, baseBuffReceivedDuration, baseDebuffReceivedDuration;
    public float baseDotDuration, baseRegenerationDuration, baseShieldDuration, baseSunGenerationCooldown;
    public float baseEvasion, baseAccuracy;
    public float baseBonusCritChanceReceived, baseBonusCritDamageReceived, baseProjectileSpeed;

    public static event System.Action<Plant, Insect, DamageTag[]> OnPlantAttackHit;
    public static event System.Action<EntityEventData> OnEntityHit;
    public static event System.Action<StatusEffect> OnEffectApplied;
    public static event System.Action<Entity, Entity> OnCriticalHit;
    public static event System.Action<EntityEventData> OnEntityKilled;
    public static event System.Action<EntityEventData> OnEntityDied;
    public static event System.Action<EntityEventData> OnHeal;
    public static event System.Action<EntityEventData> OnShieldAcquire;
    public static event System.Action<EntityEventData> OnShieldExpire;

    protected static void RaiseEntityDied(EntityEventData data) => OnEntityDied?.Invoke(data);

    [Header("Stats")]
    public float maxHealth, health, attackDamage, magicPower, attackSpeed, attackCooldown, attackCooldownTimer, attackRange, healingBonus, healingReceived;
    public float MissingHealth => maxHealth - health;
    public float physicalResistance, magicResistance;
    public int armor, magicArmor;
    public float armorPenFlat, magicPenFlat, armorPenPercent, magicPenPercent;
    public float fireResistance, waterResistance, natureResistance, windResistance, poisonResistance, iceResistance;
    public float physicalDamage, magicDamage, fallDamage, bonusEffectChance;
    public float fireDamage, waterDamage, natureDamage, windDamage, poisonDamage, iceDamage;
    public float criticalChance, criticalDamage;
    public float dotResistance, dotDamage, fallDamageResistance;
    public float elementalAffinity;
    public float passiveDamage, skillDamage, coordinatedDamage;
    public float skillDuration;
    public float lightEmissionRange;
    public float lifesteal;
    public float counterDamage;
    public float tenacity;
    public float debuffGivenDuration, buffGivenDuration, buffReceivedDuration, debuffReceivedDuration;
    public float dotDuration, regenerationDuration, shieldDuration, sunGenerationCooldown;
    public float burnDurationBonus;   // multiplies the duration of Burns this entity causes (e.g. Stargazer)
    public float sunYieldBonus;       // increases sun this entity generates and the sun its kills drop (Aeonium's Blessing)
    public float baseMinionDamage;    // the plant's inherent minion damage bonus
    public float minionDamage;        // multiplier on the damage this plant's minions deal
    public float shieldBonusDamage, shieldToughness;
    public float startingShield;
    public bool debuffsFrozen;
    public bool bypassShields;
    public float evasion, accuracy;
    public float bonusCritChanceReceived, bonusCritDamageReceived, projectileSpeed;

    [Header("Stat Adders")]
    public float maxHealthAdder, attackDamageAdder, magicPowerAdder, attackSpeedAdder, attackRangeAdder, healingBonusAdder, healingReceivedAdder;
    public float armorAdder, magicArmorAdder;
    public float armorPenFlatAdder, magicPenFlatAdder, armorPenPercentAdder, magicPenPercentAdder;
    public float fireResistanceAdder, waterResistanceAdder, natureResistanceAdder, windResistanceAdder, poisonResistanceAdder, iceResistanceAdder;
    public float physicalDamageAdder, magicDamageAdder, fallDamageAdder, bonusEffectChanceAdder;
    public float fireDamageAdder, waterDamageAdder, natureDamageAdder, windDamageAdder, poisonDamageAdder, iceDamageAdder;
    public float criticalChanceAdder, criticalDamageAdder;
    public float dotResistanceAdder, dotDamageAdder, fallDamageResistanceAdder;
    public float elementalAffinityAdder;
    public float passiveDamageAdder, skillDamageAdder, coordinatedDamageAdder;
    public float skillDurationAdder;
    public float tenacityAdder, immobilizeDurationAdder;
    public float shieldBonusDamageAdder, shieldToughnessAdder;
    public float lightEmissionRangeAdder;
    public float lifestealAdder;
    public float minionDamageAdder;
    public float counterDamageAdder;
    public float debuffGivenDurationAdder, buffGivenDurationAdder, buffReceivedDurationAdder, debuffReceivedDurationAdder;
    public float dotDurationAdder, regenerationDurationAdder, shieldDurationAdder, sunGenerationCooldownMultiplier;
    public float evasionAdder, accuracyAdder;
    public float bonusCritChanceReceivedAdder, bonusCritDamageReceivedAdder, projectileSpeedAdder;

    [Header("Stat Multipliers")]
    public float maxHealthMultiplier, attackDamageMultiplier, magicPowerMultiplier, attackSpeedMultiplier, attackRangeMultiplier, healingBonusMultiplier, healingReceivedMultiplier;
    public float armorMultiplier, magicArmorMultiplier;
    public float armorPenFlatMultiplier, magicPenFlatMultiplier, armorPenPercentMultiplier, magicPenPercentMultiplier;
    public float fireResistanceMultiplier, waterResistanceMultiplier, natureResistanceMultiplier, windResistanceMultiplier, poisonResistanceMultiplier, iceResistanceMultiplier;
    public float physicalDamageMultiplier, magicDamageMultiplier, bonusEffectChanceMultiplier;
    public float fireDamageMultiplier, waterDamageMultiplier, natureDamageMultiplier, windDamageMultiplier, poisonDamageMultiplier, iceDamageMultiplier;
    public float criticalChanceMultiplier, criticalDamageMultiplier;
    public float dotResistanceMultiplier, dotDamageMultiplier;
    public float elementalAffinityMultiplier;
    public float passiveDamageMultiplier, coordinatedDamageMultiplier;
    public float skillDurationMultiplier;
    public float tenacityMultiplier, immobilizeDurationMultiplier;
    public float lightEmissionRangeMultiplier;
    public float lifestealMultiplier;
    public float counterDamageMultiplier;
    public float debuffGivenDurationMultiplier, buffGivenDurationMultiplier, buffReceivedDurationMultiplier, debuffReceivedDurationMultiplier;
    public float evasionMultiplier, accuracyMultiplier;
    public float bonusCritChanceReceivedMultiplier, projectileSpeedMultiplier;
    public float maxHealthTotalMultiplier = 1f, attackDamageTotalMultiplier = 1f, magicPowerTotalMultiplier = 1f;
    public float attackSpeedTotalMultiplier = 1f, attackRangeTotalMultiplier = 1f;
    public float criticalChanceTotalMultiplier = 1f, criticalDamageTotalMultiplier = 1f;
    public float armorTotalMultiplier = 1f, magicArmorTotalMultiplier = 1f;
    public float armorPenFlatTotalMultiplier = 1f, magicPenFlatTotalMultiplier = 1f;

    [Header("Internal Cooldowns")]
    public float internalCooldown = 2f, blazeInternalCooldown, wetInternalCooldown, sproutInternalCooldown, coldInternalCooldown, taintedInternalCooldown, freezeInternalCooldown, germinateInternalCooldown;

    [Header("Debug")]
    public float timeAlive, totalDamageDealt;
    public virtual void UpdateStats()
    {
        maxHealth = (baseMaxHealth + maxHealthAdder + (baseMaxHealth * maxHealthMultiplier)) * maxHealthTotalMultiplier;
        attackDamage = (baseAttackDamage + attackDamageAdder + (baseAttackDamage * attackDamageMultiplier)) * attackDamageTotalMultiplier;
        magicPower = (baseMagicPower + magicPowerAdder + (baseMagicPower * magicPowerMultiplier)) * magicPowerTotalMultiplier;
        attackSpeed = (baseAttackSpeed + attackSpeedAdder + (baseAttackSpeed * attackSpeedMultiplier)) * attackSpeedTotalMultiplier;
        attackRange = (baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier)) * attackRangeTotalMultiplier;
        healingBonus = baseHealingBonus + healingBonusAdder + (baseHealingBonus * healingBonusMultiplier);
        healingReceived = baseHealingReceived + healingReceivedAdder + (baseHealingReceived * healingReceivedMultiplier);
        fireResistance = baseFireResistance + fireResistanceAdder + (baseFireResistance * fireResistanceMultiplier);
        waterResistance = baseWaterResistance + waterResistanceAdder + (baseWaterResistance * waterResistanceMultiplier);
        natureResistance = baseNatureResistance + natureResistanceAdder + (baseNatureResistance * natureResistanceMultiplier);
        windResistance = baseWindResistance + windResistanceAdder + (baseWindResistance * windResistanceMultiplier);
        poisonResistance = basePoisonResistance + poisonResistanceAdder + (basePoisonResistance * poisonResistanceMultiplier);
        iceResistance = baseIceResistance + iceResistanceAdder + (baseIceResistance * iceResistanceMultiplier);
        physicalDamage = basePhysicalDamage + physicalDamageAdder + (basePhysicalDamage * physicalDamageMultiplier);
        magicDamage = baseMagicDamage + magicDamageAdder + (baseMagicDamage * magicDamageMultiplier);
        fallDamage = baseFallDamage + fallDamageAdder;
        bonusEffectChance = baseBonusEffectChance + bonusEffectChanceAdder + (baseBonusEffectChance * bonusEffectChanceMultiplier);
        fireDamage = baseFireDamage + fireDamageAdder + (baseFireDamage * fireDamageMultiplier);
        waterDamage = baseWaterDamage + waterDamageAdder + (baseWaterDamage * waterDamageMultiplier);
        natureDamage = baseNatureDamage + natureDamageAdder + (baseNatureDamage * natureDamageMultiplier);
        windDamage = baseWindDamage + windDamageAdder + (baseWindDamage * windDamageMultiplier);
        poisonDamage = basePoisonDamage + poisonDamageAdder + (basePoisonDamage * poisonDamageMultiplier);
        iceDamage = baseIceDamage + iceDamageAdder + (baseIceDamage * iceDamageMultiplier);
        criticalChance = (baseCriticalChance + criticalChanceAdder + (baseCriticalChance * criticalChanceMultiplier)) * criticalChanceTotalMultiplier;
        criticalDamage = (baseCriticalDamage + criticalDamageAdder + (baseCriticalDamage * criticalDamageMultiplier)) * criticalDamageTotalMultiplier;
        bonusCritChanceReceived = baseBonusCritChanceReceived + bonusCritChanceReceivedAdder + (baseBonusCritChanceReceived * bonusCritChanceReceivedMultiplier);
        bonusCritDamageReceived = baseBonusCritDamageReceived + bonusCritDamageReceivedAdder;
        projectileSpeed = baseProjectileSpeed + projectileSpeedAdder + (baseProjectileSpeed * projectileSpeedMultiplier);
        dotResistance = baseDotResistance + dotResistanceAdder + (baseDotResistance * dotResistanceMultiplier);
        fallDamageResistance = baseFallDamageResistance + fallDamageResistanceAdder;
        dotDamage = baseDotDamage + dotDamageAdder + (baseDotDamage * dotDamageMultiplier);
        elementalAffinity = baseelementalAffinity + elementalAffinityAdder + (baseelementalAffinity * elementalAffinityMultiplier);
        passiveDamage = basePassiveDamage + passiveDamageAdder + (basePassiveDamage * passiveDamageMultiplier);
        skillDamage = baseSkillDamage + skillDamageAdder;
        coordinatedDamage = baseCoordinatedDamage + coordinatedDamageAdder + (baseCoordinatedDamage * coordinatedDamageMultiplier);
        skillDuration = baseSkillDuration + skillDurationAdder + (baseSkillDuration * skillDurationMultiplier);
        tenacity = baseTenacity + tenacityAdder + (baseTenacity * tenacityMultiplier);
        shieldBonusDamage = shieldBonusDamageAdder;
        shieldToughness   = shieldToughnessAdder;
        lightEmissionRange = baseLightEmissionRange + lightEmissionRangeAdder + (baseLightEmissionRange * lightEmissionRangeMultiplier);
        lifesteal = baseLifesteal + lifestealAdder + (baseLifesteal * lifestealMultiplier);
        minionDamage = baseMinionDamage + minionDamageAdder;
        counterDamage = baseCounterDamage + counterDamageAdder + (baseCounterDamage * counterDamageMultiplier);
        debuffGivenDuration    = baseDebuffGivenDuration    + debuffGivenDurationAdder    + (baseDebuffGivenDuration    * debuffGivenDurationMultiplier);
        buffGivenDuration      = baseBuffGivenDuration      + buffGivenDurationAdder      + (baseBuffGivenDuration      * buffGivenDurationMultiplier);
        buffReceivedDuration   = baseBuffReceivedDuration   + buffReceivedDurationAdder   + (baseBuffReceivedDuration   * buffReceivedDurationMultiplier);
        debuffReceivedDuration = baseDebuffReceivedDuration + debuffReceivedDurationAdder + (baseDebuffReceivedDuration * debuffReceivedDurationMultiplier);
        // percentage bonuses for effect categories (e.g. 0.2 = 20% longer); base + adder only, no multiplier since base is typically 0
        dotDuration           = baseDotDuration           + dotDurationAdder;
        regenerationDuration  = baseRegenerationDuration  + regenerationDurationAdder;
        shieldDuration        = baseShieldDuration        + shieldDurationAdder;
        sunGenerationCooldown = Mathf.Max(-0.8f, sunGenerationCooldownMultiplier);
        evasion  = baseEvasion  + evasionAdder  + (baseEvasion  * evasionMultiplier);
        accuracy = baseAccuracy + accuracyAdder + (baseAccuracy * accuracyMultiplier);
        armor      = (int)(((baseArmor      + armorAdder)      + (baseArmor      * armorMultiplier))      * armorTotalMultiplier);
        magicArmor = (int)(((baseMagicArmor + magicArmorAdder) + (baseMagicArmor * magicArmorMultiplier)) * magicArmorTotalMultiplier);
        physicalResistance = armor      / (100f + armor);
        magicResistance    = magicArmor / (100f + magicArmor);
        armorPenFlat = (baseArmorPenFlat + armorPenFlatAdder + baseArmorPenFlat * armorPenFlatMultiplier) * armorPenFlatTotalMultiplier;
        magicPenFlat = (baseMagicPenFlat + magicPenFlatAdder + baseMagicPenFlat * magicPenFlatMultiplier) * magicPenFlatTotalMultiplier;
        armorPenPercent = baseArmorPenPercent  + armorPenPercentAdder  + (baseArmorPenPercent * armorPenPercentMultiplier);
        magicPenPercent = baseMagicPenPercent  + magicPenPercentAdder  + (baseMagicPenPercent * magicPenPercentMultiplier);
        UpdateHealthBar();
    }

    public virtual void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, DamageTag[] damageTag)
    {
        float modifiedDamage, elementalMultiplier, finalDamage, dotMultiplier;
        switch (elementalType)
        {
            case ElementalType.Fire:
            elementalMultiplier = Mathf.Max(0f, 1 - fireResistance);
            break;
            case ElementalType.Water:
            elementalMultiplier = Mathf.Max(0f, 1 - waterResistance);
            break;
            case ElementalType.Ice:
            elementalMultiplier = Mathf.Max(0f, 1 - iceResistance);
            break;
            case ElementalType.Wind:
            elementalMultiplier = Mathf.Max(0f, 1 - windResistance);
            break;
            case ElementalType.Nature:
            elementalMultiplier = Mathf.Max(0f, 1 - natureResistance);
            break;
            case ElementalType.Poison:
            elementalMultiplier = Mathf.Max(0f, 1 - poisonResistance);
            break;
            default:
            elementalMultiplier = 1;
            break;
        }

        switch (damageType)
        {
            case DamageType.Physical:
            modifiedDamage = damageDealt * Mathf.Max(0f, 1 - physicalResistance);
            break;
            case DamageType.Magic:
            modifiedDamage = damageDealt * Mathf.Max(0f, 1 - magicResistance);
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
        
        finalDamage = Mathf.Round(modifiedDamage * elementalMultiplier * dotMultiplier);
        bool bypassShield = System.Array.Exists(damageTag, t => t == DamageTag.BypassShield);
        health -= (!bypassShield && HasShield()) ? DrainShields(finalDamage, 0f, null, damageType) : finalDamage;
        health = Mathf.Max(0f, health);
        TriggerHitFlash();
        UpdateHealthBar();
        foreach (StatusEffect e in new System.Collections.Generic.List<StatusEffect>(activeEffects))
            e.OnDamageReceived(elementalType, null, damageTag);

        var evtData = new EntityEventData { target = this, position = transform.position, damage = finalDamage, damageType = damageType, elementalType = elementalType, tags = damageTag };
        OnEntityHit?.Invoke(evtData);
        if (health <= 0)
        {
            OnEntityKilled?.Invoke(evtData);
            Kill();
        }
    }

    public virtual void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, Entity source, bool canCrit, DamageTag[] damageTag) // damage with source
    {
        if (source == null)
        {
            Damage(damageDealt, damageType, elementalType, damageTag);
            return;
        }

        if (source.HasEffect<TauntEffect>() && System.Array.Exists(damageTag, t => t == DamageTag.Attack))
            damageDealt *= 0.5f;

        // melee attacks roll their miss check upstream in ReceiveAttack (so the counter
        // can be gated on the same result); here we only handle non-melee attacks
        if (System.Array.Exists(damageTag, t => t == DamageTag.Attack)
            && !System.Array.Exists(damageTag, t => t == DamageTag.Melee))
        {
            float missChance = Mathf.Clamp01(evasion - source.accuracy);
            if (UnityEngine.Random.value < missChance)
            {
                StatusIndicator.Spawn(GetIndicatorPosition(), "Miss", new Color(0.55f, 0.6f, 0.75f));
                return;
            }
        }

        if (this is Insect insect && source is Plant plant) // if target = insect and source = plant
        {
            insect.RegisterAttacker(plant);
            if (System.Array.Exists(damageTag, t => t == DamageTag.Attack))
                OnPlantAttackHit?.Invoke(plant, insect, damageTag);
        }

        float modifiedDamage, elementalMultiplier, finalDamage, elementalDebuffDuration = 6f, dotMultiplier, passiveDamageMult, skillDamageMult, coordinatedDamageMult, counterDamageMult;
        bool isCrit = false;

        if (this.HasEffect<BrittleEffect>() && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            Damage(GetEffect<BrittleEffect>().bonusDamage, damageType, ElementalType.Nature, source, false, new DamageTag[] { DamageTag.ElementalDebuff });
        }

        if (this.HasEffect<FractureEffect>() && damageType == DamageType.Physical && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            Damage(damageDealt * GetEffect<FractureEffect>().bonusMultiplier, DamageType.Physical, ElementalType.Fire, source, false, new DamageTag[] { DamageTag.ElementalDebuff });
        }

        switch (elementalType)
        {
            case ElementalType.Fire:
            elementalMultiplier = Mathf.Max(0f, 1 - fireResistance + source.fireDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && blazeInternalCooldown <= 0)
                {
                    blazeInternalCooldown = internalCooldown;
                    ApplyEffect(new BlazeEffect(this, elementalDebuffDuration, 1, source));
                }

                if (this.HasEffect<GerminateEffect>())
                RemoveEffect<GerminateEffect>();
            break;

            case ElementalType.Water:
            elementalMultiplier = Mathf.Max(0f, 1 - waterResistance + source.waterDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && wetInternalCooldown <= 0)
                {
                    wetInternalCooldown = internalCooldown;
                    ApplyEffect(new WetEffect(this, elementalDebuffDuration, 1, source));
                }
            break;

            case ElementalType.Ice:
            elementalMultiplier = Mathf.Max(0f, 1 - iceResistance + source.iceDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && coldInternalCooldown <= 0)
                {
                    coldInternalCooldown = internalCooldown;
                    ApplyEffect(new ColdEffect(this, elementalDebuffDuration, 1, source));
                }
            break;

            case ElementalType.Wind:
            elementalMultiplier = Mathf.Max(0f, 1 - windResistance + source.windDamage);
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
            elementalMultiplier = Mathf.Max(0f, 1 - natureResistance + source.natureDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && sproutInternalCooldown <= 0)
                {
                    sproutInternalCooldown = internalCooldown;
                    ApplyEffect(new SproutEffect(this, elementalDebuffDuration, 1, source));
                }
            break;

            case ElementalType.Poison:
            elementalMultiplier = Mathf.Max(0f, 1 - poisonResistance + source.poisonDamage);
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && taintedInternalCooldown <= 0)
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
            {
                float effArmor = Mathf.Max(-99f, armor * (1f - source.armorPenPercent) - source.armorPenFlat);
                modifiedDamage = damageDealt * (1f - effArmor / (100f + effArmor));
                break;
            }
            case DamageType.Magic:
            {
                float effMagicArmor = Mathf.Max(-99f, magicArmor * (1f - source.magicPenPercent) - source.magicPenFlat);
                modifiedDamage = damageDealt * (1f - effMagicArmor / (100f + effMagicArmor));
                break;
            }
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

        finalDamage = modifiedDamage * elementalMultiplier * dotMultiplier * passiveDamageMult * skillDamageMult * coordinatedDamageMult * counterDamageMult;

        if (canCrit || System.Array.Exists(damageTag, t => t == DamageTag.SpecialCanCrit))
        {
            if (Random.value < source.criticalChance + bonusCritChanceReceived)
            {
                finalDamage *= source.criticalDamage * (1f + bonusCritDamageReceived);
                isCrit = true;
                OnCriticalHit?.Invoke(source, this);
            }
        }

        finalDamage = Mathf.Round(finalDamage);
        bool bypassShield = System.Array.Exists(damageTag, t => t == DamageTag.BypassShield) || source.bypassShields;
        health -= (!bypassShield && HasShield()) ? DrainShields(finalDamage, source.shieldBonusDamage, source, damageType) : finalDamage;
        health = Mathf.Max(0f, health);
        TriggerHitFlash();
        source.totalDamageDealt += finalDamage; // FOR DEBUG
        if (this is Insect damagedInsect) damagedInsect.lastSource = source;
        if (source.lifesteal > 0f) source.Heal(finalDamage * source.lifesteal);
        if (this is Plant poisonPlant && poisonPlant.elementalType == ElementalType.Poison
            && System.Array.Exists(damageTag, t => t == DamageTag.Attack))
        {
            source.Damage(damageDealt * 2f, DamageType.Physical, ElementalType.Poison, poisonPlant, false, new DamageTag[] { DamageTag.Counter });
        }

        // damage indicator

        DamageIndicator.Spawn(GetIndicatorPosition(), finalDamage, elementalType, isCrit);
        if (System.Array.Exists(damageTag, t => t == DamageTag.DoT))
            DoTAggregator.AddDamage(this, finalDamage, elementalType);

        UpdateHealthBar();
        foreach (StatusEffect e in new System.Collections.Generic.List<StatusEffect>(activeEffects))
            e.OnDamageReceived(elementalType, source, damageTag);

        var evtData = new EntityEventData { target = this, source = source, position = transform.position, damage = finalDamage, damageType = damageType, elementalType = elementalType, tags = damageTag };
        OnEntityHit?.Invoke(evtData);
        if (health <= 0)
        {
            OnEntityKilled?.Invoke(evtData);
            Kill(source);
        }
    }

// method for healing
    public virtual void Heal(float healingAmount, Entity source = null)
    {
        float bonus = source != null ? source.healingBonus : 0f;
        float actual = Mathf.Min(healingAmount * (1f + healingReceived) * (1f + bonus), maxHealth - health);
        if (actual <= 0f) return;
        health += actual;
        OnHeal?.Invoke(new EntityEventData { target = this, source = source, position = transform.position, damage = actual, amount = actual });
        UpdateHealthBar();
        HealIndicator.Spawn(GetIndicatorPosition(), actual);
    }

// method for death
    public virtual void Kill()
    {
        foreach (StatusEffect e in activeEffects) e.OnTargetDied();
        OnEntityDied?.Invoke(new EntityEventData { target = this, position = transform.position });
        Destroy(gameObject);
    }

    public virtual void Kill(Entity source)
    {
        foreach (StatusEffect e in activeEffects) e.OnTargetDied();
        OnEntityDied?.Invoke(new EntityEventData { target = this, source = source, position = transform.position });
        Destroy(gameObject);
    }

    protected virtual Vector3 GetIndicatorPosition()
    {
        return transform.position + Vector3.up * 0.25f;
    }

// upon spawning, occurs before Start()
    protected virtual void Awake()
    {
        UpdateStats();
        health = maxHealth;
        if (this is Insect) SpawnHealthBar();

        _flashRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_flashRenderer != null)
        {
            _originalMaterial = _flashRenderer.sharedMaterial;
            Shader hitFlashShader = Shader.Find("Custom/SpriteHitFlash");
            if (hitFlashShader != null)
            {
                _flashMaterial = new Material(hitFlashShader);
                _flashMaterial.SetColor("_FlashColor", Color.red);
            }
        }
    }

    protected virtual void Start()
    {
        if (startingShield > 0f)
            ApplyEffect(new StartingShieldEffect(this, this, startingShield));
    }

    //every tick
    protected virtual void Update()
    {
        UpdateStats();
        timeAlive += Time.deltaTime;
        TickEffects();

        // keep the health bar fill proportion in sync when maxHealth changes (buffs, effects, etc.)
        if (maxHealth != _lastMaxHealth)
        {
            _lastMaxHealth = maxHealth;
            UpdateHealthBar();
        }

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
    private Transform shieldFill;
    private float _lastMaxHealth = -1f;

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
        offset.x -= 0.35625f;
        healthBarInstance.transform.localPosition = offset;

        Vector3 hbScale = healthBarInstance.transform.localScale;
        hbScale.x *= 0.75f;
        hbScale.y *= 0.75f;
        healthBarInstance.transform.localScale = hbScale;

        healthBarFill = healthBarInstance.transform.Find("Fill");
        SpriteRenderer fillRenderer = healthBarFill != null ? healthBarFill.GetComponent<SpriteRenderer>() : null;
        RefreshHealthBarColor();

        if (healthBarFill != null)
        {
            GameObject shieldFillObj = new GameObject("ShieldFill");
            shieldFillObj.transform.SetParent(healthBarInstance.transform, false);
            shieldFillObj.transform.localPosition = healthBarFill.localPosition;
            shieldFillObj.transform.localScale    = healthBarFill.localScale;
            shieldFillObj.transform.localRotation = Quaternion.identity;

            SpriteRenderer shieldSR = shieldFillObj.AddComponent<SpriteRenderer>();
            if (fillRenderer != null)
            {
                shieldSR.sprite         = fillRenderer.sprite;
                shieldSR.sortingLayerID = fillRenderer.sortingLayerID;
                shieldSR.sortingOrder   = fillRenderer.sortingOrder;
                fillRenderer.sortingOrder += 1;
            }
            shieldSR.color   = new Color(0.55f, 0.55f, 0.55f, 1f);
            shieldSR.enabled = false;
            shieldFill = shieldFillObj.transform;
        }

        healthBarInstance.SetActive(false);
    }

    // plants and friendly units use a green fill, everything else red. overridable for teams
    protected virtual Color HealthBarColor => this is Plant ? Color.green : Color.red;

    // re-applies the fill color (call when the team changes, e.g. a hypnotized insect)
    protected void RefreshHealthBarColor()
    {
        SpriteRenderer fillRenderer = healthBarFill != null ? healthBarFill.GetComponent<SpriteRenderer>() : null;
        if (fillRenderer != null) fillRenderer.color = HealthBarColor;
    }

    protected void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        float totalShieldAmount = TotalShield;
        float totalDisplay      = maxHealth + totalShieldAmount;

        Vector3 hScale = healthBarFill.localScale;
        hScale.x = Mathf.Clamp01(health / totalDisplay);
        healthBarFill.localScale = hScale;

        if (shieldFill != null)
        {
            SpriteRenderer shieldSR = shieldFill.GetComponent<SpriteRenderer>();
            if (totalShieldAmount > 0f)
            {
                Vector3 sScale = shieldFill.localScale;
                sScale.x = Mathf.Clamp01((health + totalShieldAmount) / totalDisplay);
                shieldFill.localScale = sScale;
                if (shieldSR != null) shieldSR.enabled = true;
            }
            else if (shieldSR != null)
            {
                shieldSR.enabled = false;
            }
        }

        if ((health < maxHealth || totalShieldAmount > 0f) && healthBarInstance != null)
            healthBarInstance.SetActive(true);
    }

    private void TriggerHitFlash()
    {
        if (_flashRenderer == null)
            _flashRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_flashRenderer == null || _flashMaterial == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        _flashMaterial.SetFloat("_FlashAmount", 0.15f);
        _flashRenderer.material = _flashMaterial;

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            _flashMaterial.SetFloat("_FlashAmount", Mathf.Lerp(0.15f, 0f, elapsed / 0.3f));
            yield return null;
        }

        _flashRenderer.material = _originalMaterial;
        _flashCoroutine = null;
    }

    public void ShowHealthBar()
    {
        healthBarInstance?.SetActive(true);
    }

    [ContextMenu("Debug Shield")]
    public void DebugShield()
    {
        Debug.Log($"[Shield] {gameObject.name} | startingShield={startingShield} | TotalShield={TotalShield} | HasShield={HasShield()}");
        Debug.Log($"[Shield] health={health:F0}/{maxHealth:F0} | activeEffects={activeEffects.Count}");
        foreach (StatusEffect e in activeEffects)
            if (e is ShieldEffect s)
                Debug.Log($"[Shield]   {e.GetType().Name}: amount={s.amount:F0} duration={s.duration:F2} infinite={s.IsInfinite}");
        Debug.Log($"[Shield] healthBarInstance={(healthBarInstance != null ? healthBarInstance.activeInHierarchy.ToString() : "null")}");
        Debug.Log($"[Shield] healthBarFill={(healthBarFill != null ? healthBarFill.localScale.ToString() : "null")}");
        if (shieldFill != null)
        {
            SpriteRenderer sr = shieldFill.GetComponent<SpriteRenderer>();
            Debug.Log($"[Shield] shieldFill scale={shieldFill.localScale} | SR enabled={sr?.enabled} | sortingOrder={sr?.sortingOrder}");
        }
        else
        {
            Debug.Log("[Shield] shieldFill is NULL");
        }
        if (healthBarFill != null)
        {
            SpriteRenderer sr = healthBarFill.GetComponent<SpriteRenderer>();
            Debug.Log($"[Shield] healthFill scale={healthBarFill.localScale} | sortingOrder={sr?.sortingOrder}");
        }
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

    // SHIELD

    public float TotalShield
    {
        get
        {
            float total = 0f;
            foreach (StatusEffect e in activeEffects)
                if (e is ShieldEffect s) total += s.amount;
            return total;
        }
    }

    public bool HasShield() => TotalShield > 0f;

    public virtual void OnShieldBreak(ShieldEffect shield)
    {
        OnShieldExpire?.Invoke(new EntityEventData { target = this, source = shield.source, position = transform.position, amount = shield.amount });
    }

    private float DrainShields(float damage, float attackerShieldBonus, Entity source = null, DamageType damageType = DamageType.Physical)
    {
        float multiplier = (1f + attackerShieldBonus) * (1f - shieldToughness);
        float remaining  = damage;

        // Pass 1: finite shields first
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (!(activeEffects[i] is ShieldEffect shield) || shield.IsInfinite) continue;
            remaining = DrainSingleShield(shield, i, remaining, multiplier, source, damageType);
            if (activeEffects.Count <= i || activeEffects[i] != shield) i--;
            if (remaining <= 0f) return 0f;
        }

        // Pass 2: infinite shields last
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (!(activeEffects[i] is ShieldEffect shield) || !shield.IsInfinite) continue;
            remaining = DrainSingleShield(shield, i, remaining, multiplier, source, damageType);
            if (activeEffects.Count <= i || activeEffects[i] != shield) i--;
            if (remaining <= 0f) return 0f;
        }

        return remaining;
    }

    private float DrainSingleShield(ShieldEffect shield, int index, float remaining, float multiplier, Entity source, DamageType damageType)
    {
        float origToDeplete = multiplier > 0f ? shield.amount / multiplier : float.MaxValue;
        if (remaining >= origToDeplete)
        {
            shield.OnAbsorbHit(source, damageType);
            remaining -= origToDeplete;
            OnShieldBreak(shield);
            shield.OnExpire();
            activeEffects.RemoveAt(index);
            return remaining;
        }
        shield.amount -= remaining * multiplier;
        shield.OnAbsorbHit(source, damageType);
        return 0f;
    }

    // STATUS EFFECTS

    public List<StatusEffect> activeEffects = new List<StatusEffect>();

    public virtual void ApplyEffect(StatusEffect effect)
    {
        // Hellebore Protection intercept: negative effects aimed at a shielded entity can be reflected
        if (effect.effectType == StatusEffect.Type.negative)
        {
            foreach (StatusEffect e in new System.Collections.Generic.List<StatusEffect>(activeEffects))
                if (e.TryBlockNegativeEffect(effect)) return;
        }

        // OleandicToxin intercept: positive effects applied to a toxin-afflicted entity are captured and blocked
        if (effect.effectType == StatusEffect.Type.positive)
        {
            OleandicToxinEffect toxin = GetEffect<OleandicToxinEffect>();
            if (toxin != null && toxin.TryCapture(effect))
                return;
        }

        if (effect is ShieldEffect newShield)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].GetType() == effect.GetType() && activeEffects[i] is ShieldEffect existing)
                {
                    if (newShield.amount <= existing.amount) return;
                    existing.OnExpire();
                    activeEffects.RemoveAt(i);
                    break;
                }
            }
        }
        else
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].GetType() == effect.GetType())
                {
                    if (effect.sourceStackable && activeEffects[i].source != effect.source)
                        continue;
                    effect.OnReapply(activeEffects[i]);
                    activeEffects[i].OnExpire();
                    activeEffects.RemoveAt(i);
                    break;
                }
            }
        }
        // scale duration by source's given-duration stat and this entity's received-duration stat
        if (effect.effectType == StatusEffect.Type.positive)
        {
            if (effect.source != null) effect.duration *= Mathf.Max(0f, 1f + effect.source.buffGivenDuration);
            effect.duration *= Mathf.Max(0f, 1f + buffReceivedDuration);
        }
        else if (effect.effectType == StatusEffect.Type.negative)
        {
            if (effect.source != null) effect.duration *= Mathf.Max(0f, 1f + effect.source.debuffGivenDuration);
            effect.duration *= Mathf.Max(0f, 1f + debuffReceivedDuration);
        }

        activeEffects.Add(effect);
        effect.OnApply();
        OnEffectApplied?.Invoke(effect);
        if (effect is ShieldEffect acquiredShield)
            OnShieldAcquire?.Invoke(new EntityEventData { target = this, source = acquiredShield.source, position = transform.position, amount = acquiredShield.amount });
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
        // snapshot so effects that add/remove other effects during OnTick/OnExpire
        // can't corrupt the iteration (indices shifting out of range)
        StatusEffect[] snapshot = activeEffects.ToArray();
        foreach (StatusEffect effect in snapshot)
        {
            if (!activeEffects.Contains(effect)) continue; // already removed this frame

            bool durationFrozen = debuffsFrozen && effect.effectType == StatusEffect.Type.negative;

            effect.OnTick(Time.deltaTime);
            if (!durationFrozen)
                effect.duration -= Time.deltaTime;

            if (effect.IsExpired() && activeEffects.Contains(effect))
            {
                effect.OnExpire();
                activeEffects.Remove(effect);
                if (effect is ShieldEffect expiredShield)
                    OnShieldExpire?.Invoke(new EntityEventData { target = this, source = expiredShield.source, position = transform.position, amount = expiredShield.amount });
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
