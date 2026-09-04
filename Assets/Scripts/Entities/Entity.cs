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
    SpecialCanCrit,
    CanHitBurrowed,
    // generic on-hit bonus damage (e.g. Bird of Paradise's per-stack bonus and its missing-health
    // finisher) - deliberately excludes Attack so it doesn't re-trigger Entity.HandleOnHitEffects
    OnHit
    // IgnoresPhysicalResistance,
    // IgnoresMagicResistance,
    // IgnoresIceResistance,
    // IgnoresGrassResistance,
    // IgnoresFireResistance,
    // IgnoresWaterResistance,
    // IgnoresWindResistance
}

public enum ElementalType
{
    Fire, Water, Grass, Poison, Ice, Wind, Neutral, Ground
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
    // overrides source.onHitEffectiveness for this specific event (e.g. a weaker proc from splash/AoE damage)
    public float? effectivenessOverride;
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
    public float baseFireResistance, baseWaterResistance, baseGrassResistance, baseWindResistance, basePoisonResistance, baseIceResistance, baseGroundResistance;
    public float basePhysicalDamage, baseMagicDamage, baseFallDamage, baseBonusEffectChance, baseElementalEffectChance;
    public float baseFireDamage, baseWaterDamage, baseGrassDamage, baseWindDamage, basePoisonDamage, baseIceDamage, baseGroundDamage;
    public float baseCriticalChance, baseCriticalDamage;
    public float baseMinimumDamage = 0.8f, baseMaximumDamage = 1.2f;
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
    public float baseOnHitEffectiveness = 1f;

    public static event System.Action<EntityEventData> OnHit;
    public static event System.Action<EntityEventData> OnEntityHit;
    public static event System.Action<StatusEffect> OnEffectApplied;
    public static event System.Action<Entity, Entity> OnCriticalHit;
    public static event System.Action<EntityEventData> OnEntityKilled;
    public static event System.Action<EntityEventData> OnEntityDied;
    public static event System.Action<EntityEventData> OnHeal;
    public static event System.Action<EntityEventData> OnShieldAcquire;
    public static event System.Action<EntityEventData> OnShieldExpire;

    protected static void RaiseEntityDied(EntityEventData data) => OnEntityDied?.Invoke(data);
    public static void RaiseOnHit(EntityEventData data) => OnHit?.Invoke(data);

    // single centralized dispatcher for all on-hit effects, rather than each effect
    // subscribing to OnHit individually. add a new on-hit effect here as its own check
    static Entity()
    {
        OnHit += HandleOnHitEffects;
    }

    private static void HandleOnHitEffects(EntityEventData data)
    {
        if (data.source == null || data.target is not Insect insect || !insect.IsAlive) return;

        // fast-attacking sources (Snowdrop, Stargazer) deal reduced on-hit effect damage per hit,
        // so their overall on-hit output doesn't scale with attack speed the way base damage does
        float effectiveness = data.effectivenessOverride ?? data.source.onHitEffectiveness;

        // Symbiosis/Wither family passive: attacks have a 50% chance to reduce this plant's own
        // skill cooldown, scaled by on-hit effectiveness same as any other on-hit bonus. this
        // only reaches HandleOnHitEffects at all when DamageTag.Attack is present (see
        // Entity.Damage's OnHit?.Invoke gate), so no separate Attack-tag check is needed here
        if (data.source is Plant attackerPlant && attackerPlant.data != null
            && (attackerPlant.data.family == PlantFamily.Symbiosis || attackerPlant.data.family == PlantFamily.Wither)
            && Random.value < 0.5f)
            attackerPlant.skillCooldownTimer = Mathf.Max(0f, attackerPlant.skillCooldownTimer - 1f * effectiveness);

        // Bird of Paradise: Talon Focus stack gain plus its per-stack on-hit bonus damage (and,
        // at Path1 max with full stacks, the percent-current-health finisher). unlike Floral
        // Glow/Ablaze below, this isn't gated to projectile attacks since Bird of Paradise is melee
        if (data.source is BirdOfParadise bird)
            bird.OnAttackHit(insect, effectiveness);

        // Cactus: every needle attack hit applies 1 flat Punctured stack (the retaliation/counter
        // damage applies its own separate, level-scaling amount directly in OnHitByInsect, since
        // that damage isn't tagged Attack and so never reaches here)
        if (data.source is Cactus cactus)
            insect.ApplyEffect(new PuncturedEffect(insect, cactus.passiveDuration, 1, cactus));

        // Floral Glow and Ablaze proc off projectile OR melee attacks (but not, say, a passive
        // aura tick) - Waterlily's path2-max splash counts too, since it deliberately tags itself
        // Projectile for exactly this, and Bird of Paradise's melee attacks (tagged Melee) now
        // qualify the same way
        if (data.tags == null || !System.Array.Exists(data.tags, t => t == DamageTag.Projectile || t == DamageTag.Melee)) return;

        FloralGlowEffect floralGlow = data.source.GetEffect<FloralGlowEffect>();
        if (floralGlow != null)
            floralGlow.Trigger(insect, effectiveness);

        AblazeEffect ablaze = data.source.GetEffect<AblazeEffect>();
        if (ablaze != null)
            ablaze.Trigger(insect, effectiveness);
    }

    [Header("Stats")]
    public float maxHealth, health, attackDamage, magicPower, attackSpeed, attackCooldown, attackCooldownTimer, attackRange, healingBonus, healingReceived;
    public float MissingHealth => maxHealth - health;
    public float physicalResistance, magicResistance;
    public int armor, magicArmor;
    public float armorPenFlat, magicPenFlat, armorPenPercent, magicPenPercent;
    public float fireResistance, waterResistance, grassResistance, windResistance, poisonResistance, iceResistance, groundResistance;
    public float physicalDamage, magicDamage, fallDamage, bonusEffectChance, elementalEffectChance;
    public float fireDamage, waterDamage, grassDamage, windDamage, poisonDamage, iceDamage, groundDamage;
    public float criticalChance, criticalDamage;
    public float minimumDamage, maximumDamage;
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
    public float baseMinionDamage;    // the plant's inherent minion damage bonus
    public float minionDamage;        // multiplier on the damage this plant's minions deal
    public float shieldBonusDamage, shieldToughness;
    public float startingShield;
    public bool debuffsFrozen;
    public bool bypassShields;

    private int _healCanCritSources;
    public bool HealCanCrit => _healCanCritSources > 0;
    public void AddHealCanCrit()    => _healCanCritSources++;
    public void RemoveHealCanCrit() => _healCanCritSources = Mathf.Max(0, _healCanCritSources - 1);

    private int _dotCanCritSources;
    public bool DotCanCrit => _dotCanCritSources > 0;
    public void AddDotCanCrit()    => _dotCanCritSources++;
    public void RemoveDotCanCrit() => _dotCanCritSources = Mathf.Max(0, _dotCanCritSources - 1);

    private int _elementalReactionCanCritSources;
    public bool ElementalReactionCanCrit => _elementalReactionCanCritSources > 0;
    public void AddElementalReactionCanCrit()    => _elementalReactionCanCritSources++;
    public void RemoveElementalReactionCanCrit() => _elementalReactionCanCritSources = Mathf.Max(0, _elementalReactionCanCritSources - 1);
    public float evasion, accuracy;
    public float bonusCritChanceReceived, bonusCritDamageReceived, projectileSpeed;
    public float onHitEffectiveness;

    [Header("Stat Adders")]
    public float maxHealthAdder, attackDamageAdder, magicPowerAdder, attackSpeedAdder, attackRangeAdder, healingBonusAdder, healingReceivedAdder;
    public float armorAdder, magicArmorAdder;
    public float armorPenFlatAdder, magicPenFlatAdder, armorPenPercentAdder, magicPenPercentAdder;
    public float fireResistanceAdder, waterResistanceAdder, grassResistanceAdder, windResistanceAdder, poisonResistanceAdder, iceResistanceAdder, groundResistanceAdder;
    public float physicalDamageAdder, magicDamageAdder, fallDamageAdder, bonusEffectChanceAdder, elementalEffectChanceAdder;
    public float fireDamageAdder, waterDamageAdder, grassDamageAdder, windDamageAdder, poisonDamageAdder, iceDamageAdder, groundDamageAdder;
    public float criticalChanceAdder, criticalDamageAdder;
    public float minimumDamageAdder, maximumDamageAdder;
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
    public float sunYieldAdder, currencyYieldAdder;
    public float onHitEffectivenessAdder;

    [Header("Stat Multipliers")]
    public float maxHealthMultiplier, attackDamageMultiplier, magicPowerMultiplier, attackSpeedMultiplier, attackRangeMultiplier, healingBonusMultiplier, healingReceivedMultiplier;
    public float armorMultiplier, magicArmorMultiplier;
    public float armorPenFlatMultiplier, magicPenFlatMultiplier, armorPenPercentMultiplier, magicPenPercentMultiplier;
    public float fireResistanceMultiplier, waterResistanceMultiplier, grassResistanceMultiplier, windResistanceMultiplier, poisonResistanceMultiplier, iceResistanceMultiplier, groundResistanceMultiplier;
    public float physicalDamageMultiplier, magicDamageMultiplier, bonusEffectChanceMultiplier, elementalEffectChanceMultiplier;
    public float fireDamageMultiplier, waterDamageMultiplier, grassDamageMultiplier, windDamageMultiplier, poisonDamageMultiplier, iceDamageMultiplier, groundDamageMultiplier;
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
    public float sunYieldMultiplier, currencyYieldMultiplier;
    public float maxHealthTotalMultiplier = 1f, attackDamageTotalMultiplier = 1f, magicPowerTotalMultiplier = 1f;
    public float attackSpeedTotalMultiplier = 1f, attackRangeTotalMultiplier = 1f;
    public float criticalChanceTotalMultiplier = 1f, criticalDamageTotalMultiplier = 1f;
    public float armorTotalMultiplier = 1f, magicArmorTotalMultiplier = 1f;
    public float armorPenFlatTotalMultiplier = 1f, magicPenFlatTotalMultiplier = 1f;

    [Header("Internal Cooldowns")]
    public float internalCooldown = 4f, fireInternalCooldown, waterInternalCooldown, grassInternalCooldown, iceInternalCooldown, poisonInternalCooldown, windInternalCooldown, groundInternalCooldown, freezeInternalCooldown, germinateInternalCooldown;

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
        grassResistance = baseGrassResistance + grassResistanceAdder + (baseGrassResistance * grassResistanceMultiplier);
        windResistance = baseWindResistance + windResistanceAdder + (baseWindResistance * windResistanceMultiplier);
        poisonResistance = basePoisonResistance + poisonResistanceAdder + (basePoisonResistance * poisonResistanceMultiplier);
        iceResistance = baseIceResistance + iceResistanceAdder + (baseIceResistance * iceResistanceMultiplier);
        groundResistance = baseGroundResistance + groundResistanceAdder + (baseGroundResistance * groundResistanceMultiplier);
        physicalDamage = basePhysicalDamage + physicalDamageAdder + (basePhysicalDamage * physicalDamageMultiplier);
        magicDamage = baseMagicDamage + magicDamageAdder + (baseMagicDamage * magicDamageMultiplier);
        fallDamage = baseFallDamage + fallDamageAdder;
        bonusEffectChance = baseBonusEffectChance + bonusEffectChanceAdder + (baseBonusEffectChance * bonusEffectChanceMultiplier);
        elementalEffectChance = baseElementalEffectChance + elementalEffectChanceAdder + (baseElementalEffectChance * elementalEffectChanceMultiplier);
        fireDamage = baseFireDamage + fireDamageAdder + (baseFireDamage * fireDamageMultiplier);
        waterDamage = baseWaterDamage + waterDamageAdder + (baseWaterDamage * waterDamageMultiplier);
        grassDamage = baseGrassDamage + grassDamageAdder + (baseGrassDamage * grassDamageMultiplier);
        windDamage = baseWindDamage + windDamageAdder + (baseWindDamage * windDamageMultiplier);
        poisonDamage = basePoisonDamage + poisonDamageAdder + (basePoisonDamage * poisonDamageMultiplier);
        iceDamage = baseIceDamage + iceDamageAdder + (baseIceDamage * iceDamageMultiplier);
        groundDamage = baseGroundDamage + groundDamageAdder + (baseGroundDamage * groundDamageMultiplier);
        minimumDamage = baseMinimumDamage + minimumDamageAdder;
        maximumDamage = baseMaximumDamage + maximumDamageAdder;
        criticalChance = (baseCriticalChance + criticalChanceAdder + (baseCriticalChance * criticalChanceMultiplier)) * criticalChanceTotalMultiplier;
        criticalDamage = (baseCriticalDamage + criticalDamageAdder + (baseCriticalDamage * criticalDamageMultiplier)) * criticalDamageTotalMultiplier;
        bonusCritChanceReceived = baseBonusCritChanceReceived + bonusCritChanceReceivedAdder + (baseBonusCritChanceReceived * bonusCritChanceReceivedMultiplier);
        bonusCritDamageReceived = baseBonusCritDamageReceived + bonusCritDamageReceivedAdder;
        projectileSpeed = baseProjectileSpeed + projectileSpeedAdder + (baseProjectileSpeed * projectileSpeedMultiplier);
        onHitEffectiveness = Mathf.Max(0f, baseOnHitEffectiveness + onHitEffectivenessAdder);
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
        // burrowed insects can't be hit by anything new that doesn't explicitly carry
        // CanHitBurrowed (e.g. a splash/AoE attack that happens to overlap one incidentally) -
        // this is the hard backstop, independent of whether targeting logic picked it or not.
        // DoT ticks (burn/poison/etc) are exempt: those are already-running effects from before
        // it burrowed, not a new attack reaching it underground, so they keep ticking normally
        if (this is Insect burrowedCheck && burrowedCheck.isBurrowed
            && !System.Array.Exists(damageTag, t => t == DamageTag.CanHitBurrowed)
            && !System.Array.Exists(damageTag, t => t == DamageTag.DoT))
            return;

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
            case ElementalType.Grass:
            elementalMultiplier = Mathf.Max(0f, 1 - grassResistance);
            break;
            case ElementalType.Poison:
            elementalMultiplier = Mathf.Max(0f, 1 - poisonResistance);
            break;
            case ElementalType.Ground:
            elementalMultiplier = Mathf.Max(0f, 1 - groundResistance);
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
            dotMultiplier = Mathf.Max(0f, 1 - dotResistance);
        } else
        {
            dotMultiplier = 1;
        }
        
        finalDamage = Mathf.Round(modifiedDamage * elementalMultiplier * dotMultiplier);
        bool bypassShield = System.Array.Exists(damageTag, t => t == DamageTag.BypassShield);
        health -= (!bypassShield && HasShield()) ? DrainShields(finalDamage, 0f, null, damageType) : finalDamage;
        health = Mathf.Max(0f, health);
        RefreshCombatState();
        TriggerHitFlash();
        UpdateHealthBar();
        foreach (StatusEffect e in new System.Collections.Generic.List<StatusEffect>(activeEffects))
            e.OnDamageReceived(elementalType, null, damageTag);

        var evtData = new EntityEventData { target = this, position = transform.position, damage = finalDamage, damageType = damageType, elementalType = elementalType, tags = damageTag };
        if (System.Array.Exists(damageTag, t => t == DamageTag.Attack))
            OnHit?.Invoke(evtData);
        OnEntityHit?.Invoke(evtData);
        if (health <= 0)
        {
            OnEntityKilled?.Invoke(evtData);
            Kill();
        }
    }

    // onHitEffectivenessOverride: when a DamageTag.OnHit hit's effectiveness should come from a
    // different entity than source (e.g. Floral Glow's bonus hit is sourced from Calendula, but
    // should scale with the blessed ally's own onHitEffectiveness, not Calendula's) - defaults to
    // source.onHitEffectiveness when not provided
    public virtual void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, Entity source, bool canCrit, DamageTag[] damageTag, bool forceCrit = false, float? onHitEffectivenessOverride = null) // damage with source
    {
        if (this is Insect burrowedCheck && burrowedCheck.isBurrowed
            && !System.Array.Exists(damageTag, t => t == DamageTag.CanHitBurrowed)
            && !System.Array.Exists(damageTag, t => t == DamageTag.DoT))
            return;

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

        if (this is Insect insect && source is Plant plant)
            insect.RegisterAttacker(plant);

        float modifiedDamage, elementalMultiplier, finalDamage, dotMultiplier, passiveDamageMult, skillDamageMult, coordinatedDamageMult, counterDamageMult, onHitMult;
        bool isCrit = false;

        // elemental effect procs: dot damage (burn/poison ticks etc) rolls at half chance, and
        // damage already tagged as a reaction/debuff proc (e.g. a burn tick) cannot itself proc a new effect
        bool canProcElementalEffect = this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff);
        bool isDoTDamage = System.Array.Exists(damageTag, t => t == DamageTag.DoT);
        float elementalEffectRoll = source.elementalEffectChance * (1f + source.bonusEffectChance) * (isDoTDamage ? 0.5f : 1f);

        if (this.HasEffect<BrittleEffect>() && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            Damage(GetEffect<BrittleEffect>().bonusDamage, damageType, ElementalType.Grass, source, false, new DamageTag[] { DamageTag.ElementalDebuff });
        }

        if (this.HasEffect<FractureEffect>() && damageType == DamageType.Physical && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            Damage(damageDealt * GetEffect<FractureEffect>().bonusMultiplier, DamageType.Physical, ElementalType.Fire, source, false, new DamageTag[] { DamageTag.ElementalDebuff });
        }

        // Punctured (Cactus passive): each stack deals 1 extra Physical/Grass damage whenever
        // Physical damage is taken, sourced from whoever actually dealt that original damage (not
        // necessarily the Cactus that applied the stacks) - tagged ElementalDebuff like
        // Fracture/Brittle above so this bonus hit can't re-trigger itself
        if (this.HasEffect<PuncturedEffect>() && damageType == DamageType.Physical && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            Damage(GetEffect<PuncturedEffect>().level, DamageType.Physical, ElementalType.Grass, source, false, new DamageTag[] { DamageTag.ElementalDebuff });
        }

        // windshear: any other element's damage consumes it and shreds that element's resistance, scaled
        // by the elemental affinity of whichever plant originally applied the Windshear primer (not the
        // plant landing this detonating hit). one subclass per element, so multiple Windsheared debuffs
        // can be active on the same target at once
        if (this.HasEffect<WindshearEffect>() && elementalType != ElementalType.Wind && elementalType != ElementalType.Neutral
            && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
        {
            Entity windshearSource = GetEffect<WindshearEffect>().source;
            RemoveEffect<WindshearEffect>();
            StatusEffect windsheared = elementalType switch
            {
                ElementalType.Fire   => new WindshearFireEffect(this, 4f, 1, windshearSource),
                ElementalType.Water  => new WindshearWaterEffect(this, 4f, 1, windshearSource),
                ElementalType.Grass  => new WindshearGrassEffect(this, 4f, 1, windshearSource),
                ElementalType.Poison => new WindshearPoisonEffect(this, 4f, 1, windshearSource),
                ElementalType.Ice    => new WindshearIceEffect(this, 4f, 1, windshearSource),
                ElementalType.Ground => new WindshearGroundEffect(this, 4f, 1, windshearSource),
                _ => null
            };
            if (windsheared != null)
                ApplyEffect(windsheared);
        }

        switch (elementalType)
        {
            case ElementalType.Fire:
            elementalMultiplier = Mathf.Max(0f, 1 - fireResistance) * (1 + source.fireDamage);
            // elemental reactions temporarily disabled
            /*
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && fireInternalCooldown <= 0)
                {
                    fireInternalCooldown = internalCooldown;
                    ApplyEffect(new FirePrimer(this, elementalDebuffDuration, 1, source));
                }
            */

                if (this.HasEffect<GerminateEffect>())
                RemoveEffect<GerminateEffect>();

                if (canProcElementalEffect && Random.value < elementalEffectRoll)
                    ApplyEffect(new BurnEffect(this, 6f, 1, source));
            break;

            case ElementalType.Water:
            elementalMultiplier = Mathf.Max(0f, 1 - waterResistance) * (1 + source.waterDamage);
            // elemental reactions temporarily disabled
            /*
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && waterInternalCooldown <= 0)
                {
                    waterInternalCooldown = internalCooldown;
                    ApplyEffect(new WaterPrimer(this, elementalDebuffDuration, 1, source));
                }
            */

                if (canProcElementalEffect && Random.value < elementalEffectRoll)
                    ApplyEffect(new SoakedEffect(this, 8f, 1, source));
            break;

            case ElementalType.Ice:
            elementalMultiplier = Mathf.Max(0f, 1 - iceResistance) * (1 + source.iceDamage);
            // elemental reactions temporarily disabled
            /*
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && iceInternalCooldown <= 0)
                {
                    iceInternalCooldown = internalCooldown;
                    ApplyEffect(new IcePrimer(this, elementalDebuffDuration, 1, source));
                }
            */

                if (canProcElementalEffect && Random.value < elementalEffectRoll)
                    ApplyEffect(new FreezeEffect(this, 2f, 1, source));
            break;

            case ElementalType.Wind:
            elementalMultiplier = Mathf.Max(0f, 1 - windResistance) * (1 + source.windDamage);
            if (this is Insect windInsect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff))
                {
                    // elemental reactions temporarily disabled
                    /*
                    if (windInternalCooldown <= 0 &&
                        (windInsect.HasEffect<FirePrimer>() || windInsect.HasEffect<IcePrimer>() ||
                         windInsect.HasEffect<WaterPrimer>()   || windInsect.HasEffect<PoisonPrimer>() ||
                         windInsect.HasEffect<GrassPrimer>()))
                    {
                        windInternalCooldown = internalCooldown;
                        ApplyEffect(new WindPrimer(this, 0.5f, 1, source));
                    }
                    */
                    if (source is Anemone anemone)
                        anemone.ApplyWindErosion(windInsect);
                }

                if (canProcElementalEffect && Random.value < elementalEffectRoll)
                    ApplyEffect(new WindshearEffect(this, 8f, 1, source));
            break;

            case ElementalType.Grass:
            elementalMultiplier = Mathf.Max(0f, 1 - grassResistance) * (1 + source.grassDamage);
            // elemental reactions temporarily disabled
            /*
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && grassInternalCooldown <= 0)
                {
                    grassInternalCooldown = internalCooldown;
                    ApplyEffect(new GrassPrimer(this, elementalDebuffDuration, 1, source));
                }
            */

                if (canProcElementalEffect && Random.value < elementalEffectRoll)
                    ApplyEffect(new SeededEffect(this, 8f, 1, source));
            break;

            case ElementalType.Poison:
            elementalMultiplier = Mathf.Max(0f, 1 - poisonResistance) * (1 + source.poisonDamage);
            // elemental reactions temporarily disabled
            /*
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && poisonInternalCooldown <= 0)
                {
                    poisonInternalCooldown = internalCooldown;
                    ApplyEffect(new PoisonPrimer(this, elementalDebuffDuration, 1, source));
                }
            */

                if (canProcElementalEffect && Random.value < elementalEffectRoll)
                    ApplyEffect(new PoisonedEffect(this, 6f, 1, source));
            break;

            case ElementalType.Ground:
            elementalMultiplier = Mathf.Max(0f, 1 - groundResistance) * (1 + source.groundDamage);
            // elemental reactions temporarily disabled
            /*
            if (this is Insect && !System.Array.Exists(damageTag, t => t == DamageTag.ElementalDebuff) && groundInternalCooldown <= 0)
                {
                    groundInternalCooldown = internalCooldown;
                    ApplyEffect(new GroundPrimer(this, elementalDebuffDuration, 1, source));
                }
            */

                if (canProcElementalEffect && Random.value < elementalEffectRoll)
                    ApplyEffect(new VulnerableEffect(this, 8f, 1, source));
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
            dotMultiplier = Mathf.Max(0f, 1 - dotResistance) * (1 + source.dotDamage);
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
            // Kindred family passive: dealing Coordinated damage has a 50% chance to reduce this
            // plant's own skill cooldown, regardless of whether this hit also carries
            // DamageTag.Attack (most Coordinated hits, e.g. Floral Glow's bonus poke or Psionic
            // Carrot, don't)
            if (source is Plant kindredPlant && kindredPlant.data != null && kindredPlant.data.family == PlantFamily.Kindred
                && Random.value < 0.5f)
                kindredPlant.skillCooldownTimer = Mathf.Max(0f, kindredPlant.skillCooldownTimer - 0.5f);
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

        // Thorn family passive: +10% damage against insects currently under 50% health
        float thornMult = (source is Plant thornPlant && thornPlant.data != null && thornPlant.data.family == PlantFamily.Thorn
            && this is Insect && health < maxHealth * 0.5f) ? 1.1f : 1f;

        // generic on-hit bonus damage (Floral Glow, Ablaze, Bird of Paradise's passive/finisher):
        // scales with onHitEffectiveness same as base damage would, but sourced from whichever
        // entity's effectiveness the caller specifies (see onHitEffectivenessOverride above),
        // not necessarily the same entity as source
        if (System.Array.Exists(damageTag, t => t == DamageTag.OnHit))
        {
            onHitMult = onHitEffectivenessOverride ?? source.onHitEffectiveness;
        } else
        {
            onHitMult = 1;
        }

        finalDamage = modifiedDamage * elementalMultiplier * dotMultiplier * passiveDamageMult * skillDamageMult * coordinatedDamageMult * counterDamageMult * thornMult * onHitMult;

        // damage variance roll, dot ticks stay flat so aggregated numbers remain stable
        // min always wins, when min surpasses max every roll lands on min
        if (!System.Array.Exists(damageTag, t => t == DamageTag.DoT))
        {
            float minRoll = source.minimumDamage;
            float maxRoll = Mathf.Max(source.maximumDamage, minRoll);
            finalDamage *= Random.Range(minRoll, maxRoll);
        }

        // forceCrit lets a caller pre-roll one shared crit check and guarantee the outcome here
        // instead of letting each Damage call roll its own (e.g. Carrot's eruption: one roll,
        // applied identically to every insect it hits)
        if (canCrit || forceCrit || System.Array.Exists(damageTag, t => t == DamageTag.SpecialCanCrit))
        {
            if (forceCrit || Random.value < source.criticalChance + bonusCritChanceReceived)
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
        RefreshCombatState();
        source.RefreshCombatState();
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
        if (System.Array.Exists(damageTag, t => t == DamageTag.Attack))
            OnHit?.Invoke(evtData);
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
        DecayEffect decay = GetEffect<DecayEffect>();
        if (decay != null)
        {
            float invertDmg = Mathf.Round(healingAmount);
            if (invertDmg > 0f)
            {
                DamageTag[] invertTags = { DamageTag.PassiveDamage };
                if (decay.source != null)
                    Damage(invertDmg, DamageType.Magic, ElementalType.Poison, decay.source, false, invertTags);
                else
                    Damage(invertDmg, DamageType.Magic, ElementalType.Poison, invertTags);
            }
            return;
        }

        float bonus = source != null ? source.healingBonus : 0f;
        float actual = Mathf.Min(healingAmount * (1f + healingReceived) * (1f + bonus), maxHealth - health);
        if (actual <= 0f) return;
        bool isCrit = false;
        if (source != null && source.HealCanCrit && Random.value < source.criticalChance)
        {
            actual *= 1.5f;
            actual = Mathf.Min(actual, maxHealth - health);
            isCrit = true;
        }
        health += actual;
        OnHeal?.Invoke(new EntityEventData { target = this, source = source, position = transform.position, damage = actual, amount = actual });
        UpdateHealthBar();
        HealIndicator.Spawn(GetIndicatorPosition(), actual, isCrit);
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
        TickHealthBarFill();
        TickDamageChunkFill();
        _shieldJustExpired = false;

        // keep current health in sync when maxHealth changes (buffs, effects, etc.): a maxHealth
        // increase carries the same gain into current health (so e.g. a plant that only gains its
        // placement-based maxHealth bonus a frame or two after spawn doesn't stay stuck missing that
        // health forever), while a decrease just clamps health down to avoid an overheal display.
        // _lastMaxHealth starts at -1 as a sentinel so the very first tick only syncs, never heals
        if (maxHealth != _lastMaxHealth)
        {
            if (_lastMaxHealth >= 0f)
            {
                float delta = maxHealth - _lastMaxHealth;
                if (delta > 0f) health += delta;
            }
            _lastMaxHealth = maxHealth;
            health = Mathf.Min(health, maxHealth);
            UpdateHealthBar();
        }

        if (fireInternalCooldown > 0)
        {
            fireInternalCooldown -= Time.deltaTime;
        }

        if (waterInternalCooldown > 0)
        {
            waterInternalCooldown -= Time.deltaTime;
        }

        if (grassInternalCooldown > 0)
        {
            grassInternalCooldown -= Time.deltaTime;
        }

        if (iceInternalCooldown > 0)
        {
            iceInternalCooldown -= Time.deltaTime;
        }

        if (poisonInternalCooldown > 0)
        {
            poisonInternalCooldown -= Time.deltaTime;
        }

        if (windInternalCooldown > 0)
        {
            windInternalCooldown -= Time.deltaTime;
        }

        if (groundInternalCooldown > 0)
        {
            groundInternalCooldown -= Time.deltaTime;
        }

        if (freezeInternalCooldown > 0)
            freezeInternalCooldown -= Time.deltaTime;

        if (germinateInternalCooldown > 0)
            germinateInternalCooldown -= Time.deltaTime;

    }

    // per-effect-type internal cooldown for hard CC effects that opt in (see
    // HardCrowdControl.InternalCooldownAfterExpiry) - e.g. once Freeze lands on this entity,
    // Freeze specifically can't land again until its own cooldown clears, independent of every
    // other hard CC type. stored as an absolute end time rather than a countdown, so no per-frame
    // decay bookkeeping is needed - replaced the old shared-pool diminishing-returns system
    private readonly System.Collections.Generic.Dictionary<System.Type, float> _hardCCCooldownEndTimes = new System.Collections.Generic.Dictionary<System.Type, float>();

    public bool IsHardCCOnInternalCooldown(System.Type effectType) =>
        _hardCCCooldownEndTimes.TryGetValue(effectType, out float endTime) && Time.time < endTime;

    public void SetHardCCInternalCooldown(System.Type effectType, float cooldownDuration) =>
        _hardCCCooldownEndTimes[effectType] = Time.time + cooldownDuration;

    // generic "in combat" flag: refreshed on both the source and target of every Damage() call
    // (see the two Damage overloads), lasting InCombatEffect.Duration. lets any system check
    // "has this entity dealt or received damage recently" (e.g. Bog Iris's regen doubling) without
    // tracking its own timer
    public bool IsInCombat => HasEffect<InCombatEffect>();

    public void RefreshCombatState() => ApplyEffect(new InCombatEffect(this));

    // HEALTH BAR

    protected Vector3 healthBarOffset = new Vector3(0, 0.6f, 0); // OFFSET
    protected GameObject healthBarInstance;
    private Transform healthBarFill;
    private Transform shieldFill;
    private Transform damageChunkFill;
    private float _lastMaxHealth = -1f;

    // fluid health bar: the fill doesn't snap straight to health/shield, it eases toward them
    // every frame (see TickHealthBarFill, called from Update) so damage and healing read as motion
    private float _displayedHealth = -1f;
    private float _displayedShield = -1f;
    private const float HealthBarLerpSpeed = 6f;

    // trailing "damage chunk" bar: sits behind the main+shield fill holding the pre-hit total
    // protection (health + shield, so a shield-only hit still shows a chunk even though health
    // itself never moved), waits DamageChunkDelay before draining down to the real total at the
    // same speed as the main bar, so a hit briefly shows the exact chunk just lost before it fades
    private float _chunkProtection = -1f;
    private float _lastProtectionForChunk = -1f;
    private float _chunkDelayTimer;
    private const float DamageChunkDelay = 0.5f;

    // set for one frame whenever a ShieldEffect is actually removed (duration ran out, or it was
    // fully depleted by damage) - a shield popping should just vanish, not fade/lerp out like a
    // health chip, and shouldn't leave a "damage taken" afterimage on the chunk bar either
    private bool _shieldJustExpired;

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

        _displayedHealth = health;
        _displayedShield = TotalShield;
        _chunkProtection = health + TotalShield;
        _lastProtectionForChunk = _chunkProtection;

        if (healthBarFill != null)
        {
            int baseOrder = fillRenderer != null ? fillRenderer.sortingOrder : 0;

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
                shieldSR.sortingOrder   = baseOrder + 1;
            }
            shieldSR.color   = new Color(0.55f, 0.55f, 0.55f, 1f);
            shieldSR.enabled = false;
            shieldFill = shieldFillObj.transform;

            GameObject chunkFillObj = new GameObject("DamageChunkFill");
            chunkFillObj.transform.SetParent(healthBarInstance.transform, false);
            chunkFillObj.transform.localPosition = healthBarFill.localPosition;
            chunkFillObj.transform.localScale    = healthBarFill.localScale;
            chunkFillObj.transform.localRotation = Quaternion.identity;

            SpriteRenderer chunkSR = chunkFillObj.AddComponent<SpriteRenderer>();
            if (fillRenderer != null)
            {
                chunkSR.sprite         = fillRenderer.sprite;
                chunkSR.sortingLayerID = fillRenderer.sortingLayerID;
                chunkSR.sortingOrder   = baseOrder;
            }
            chunkSR.color   = DamageChunkColor;
            chunkSR.enabled = false;
            damageChunkFill = chunkFillObj.transform;

            // front-to-back: main Fill > Shield > DamageChunk. previously Shield and DamageChunk
            // shared the same sortingOrder, an unresolved tie that could let either randomly win
            if (fillRenderer != null) fillRenderer.sortingOrder = baseOrder + 2;
        }

        healthBarInstance.SetActive(false);
    }

    // plants and friendly units use a green fill, everything else red. overridable for teams
    protected static readonly Color EnemyHealthBarColor = new Color(0.6f, 0.05f, 0.05f);
    protected virtual Color HealthBarColor => this is Plant ? Color.green : EnemyHealthBarColor;

    // same ally/enemy split as HealthBarColor, used to color the damage chunk bar
    protected virtual bool IsHealthBarAlly => this is Plant;
    private static readonly Color EnemyDamageChunkColor = new Color(1f, 0.55f, 0f);
    private Color DamageChunkColor => IsHealthBarAlly ? Color.white : EnemyDamageChunkColor;

    // re-applies the fill and chunk colors (call when the team changes, e.g. a hypnotized insect)
    protected void RefreshHealthBarColor()
    {
        SpriteRenderer fillRenderer = healthBarFill != null ? healthBarFill.GetComponent<SpriteRenderer>() : null;
        if (fillRenderer != null) fillRenderer.color = HealthBarColor;

        SpriteRenderer chunkRenderer = damageChunkFill != null ? damageChunkFill.GetComponent<SpriteRenderer>() : null;
        if (chunkRenderer != null) chunkRenderer.color = DamageChunkColor;
    }

    // activation only - the fill itself is animated every frame by TickHealthBarFill, so a caller
    // reporting a change (damage, heal, shield gained, etc.) just needs the bar to become visible
    protected void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        if ((health < maxHealth || TotalShield > 0f) && healthBarInstance != null)
            healthBarInstance.SetActive(true);
    }

    // eases the displayed fill toward the real health/shield every frame instead of snapping,
    // so damage and healing read as motion on the bar rather than an instant jump
    private void TickHealthBarFill()
    {
        if (healthBarFill == null) return;

        float totalShieldAmount = TotalShield;
        float totalDisplay      = maxHealth + totalShieldAmount;
        if (totalDisplay <= 0f) return;

        float t = 1f - Mathf.Exp(-HealthBarLerpSpeed * Time.unscaledDeltaTime);
        _displayedHealth = Mathf.Lerp(_displayedHealth, health, t);
        _displayedShield = _shieldJustExpired ? totalShieldAmount : Mathf.Lerp(_displayedShield, totalShieldAmount, t);

        Vector3 hScale = healthBarFill.localScale;
        hScale.x = Mathf.Clamp01(_displayedHealth / totalDisplay);
        healthBarFill.localScale = hScale;

        if (shieldFill != null)
        {
            SpriteRenderer shieldSR = shieldFill.GetComponent<SpriteRenderer>();
            if (_displayedShield > 0.001f || totalShieldAmount > 0f)
            {
                Vector3 sScale = shieldFill.localScale;
                sScale.x = Mathf.Clamp01((_displayedHealth + _displayedShield) / totalDisplay);
                shieldFill.localScale = sScale;
                if (shieldSR != null) shieldSR.enabled = true;
            }
            else if (shieldSR != null)
            {
                shieldSR.enabled = false;
            }
        }
    }

    // holds the pre-hit total protection (health + shield) behind the main+shield fill, then -
    // after DamageChunkDelay seconds with no further drop - eases it down to the real total at the
    // same speed as the main bar. a fresh drop (another hit landing mid-drain, to either health or
    // shield) restarts the hold from wherever the chunk currently sits, rather than snapping it
    // back up to the original pre-hit value
    private void TickDamageChunkFill()
    {
        if (damageChunkFill == null) return;

        float currentProtection = health + TotalShield;

        if (_shieldJustExpired)
        {
            // a shield popping (duration ran out, or fully depleted) isn't damage - snap straight
            // to the new total instead of holding/draining an afterimage of it
            _chunkProtection = currentProtection;
            _chunkDelayTimer = 0f;
        }
        else if (currentProtection < _lastProtectionForChunk)
        {
            _chunkDelayTimer = DamageChunkDelay;
        }
        else if (currentProtection > _chunkProtection)
        {
            _chunkProtection = currentProtection;
            _chunkDelayTimer = 0f;
        }
        _lastProtectionForChunk = currentProtection;

        if (_chunkDelayTimer > 0f)
            _chunkDelayTimer -= Time.unscaledDeltaTime;
        else if (_chunkProtection > currentProtection)
            _chunkProtection = Mathf.Lerp(_chunkProtection, currentProtection, 1f - Mathf.Exp(-HealthBarLerpSpeed * Time.unscaledDeltaTime));

        float totalDisplay = maxHealth + TotalShield;
        if (totalDisplay <= 0f) return;

        SpriteRenderer chunkSR = damageChunkFill.GetComponent<SpriteRenderer>();
        bool visible = _chunkProtection > currentProtection + 0.001f;
        if (chunkSR != null) chunkSR.enabled = visible;
        if (visible)
        {
            Vector3 scale = damageChunkFill.localScale;
            scale.x = Mathf.Clamp01(_chunkProtection / totalDisplay);
            damageChunkFill.localScale = scale;
        }
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
            _shieldJustExpired = true;
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
        // per-effect-type hard CC internal cooldown (see HardCrowdControl.InternalCooldownAfterExpiry)
        // - only rejects effect types that actually opt in (currently just Freeze), skill-sourced
        // hard CCs left at 0 are ungated here and rely on their own skill cooldown instead
        if (effect is HardCrowdControl hardCC && hardCC.InternalCooldownAfterExpiry > 0f
            && IsHardCCOnInternalCooldown(effect.GetType()))
            return;

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

        if (effect is ShieldEffect newShield)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].GetType() == effect.GetType() && activeEffects[i] is ShieldEffect existing)
                {
                    // sourceStackable shields from a different source coexist instead of merging;
                    // same-source (or non-stackable) shields still keep only the stronger one
                    if (effect.sourceStackable && existing.source != effect.source)
                        continue;
                    if (newShield.amount <= existing.amount) return;
                    existing.OnExpire();
                    activeEffects.RemoveAt(i);
                    break;
                }
            }
        }
        else if (effect is IElementalAffinityEffect newElemental)
        {
            // unique per target: the instance with the higher elemental affinity wins, but any
            // attempted application refreshes the duration of whichever instance ends up active
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].GetType() == effect.GetType() && activeEffects[i] is IElementalAffinityEffect existingElemental)
                {
                    if (newElemental.AffinityPower >= existingElemental.AffinityPower)
                    {
                        effect.OnReapply(activeEffects[i]);
                        activeEffects[i].OnExpire();
                        activeEffects.RemoveAt(i);
                        break;
                    }
                    else
                    {
                        activeEffects[i].duration = effect.duration;
                        return;
                    }
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

        activeEffects.Add(effect);
        effect.OnApply();
        if (effect is ShieldEffect) UpdateHealthBar();
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

    // for sourceStackable effects where several instances of the same type can coexist (e.g.
    // Carrot's Psionic Mark - each Carrot hitting a target tracks its own stack independently)
    public T GetEffect<T>(Entity bySource) where T : StatusEffect
    {
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect is T typedEffect && effect.source == bySource)
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
                {
                    _shieldJustExpired = true;
                    OnShieldExpire?.Invoke(new EntityEventData { target = this, source = expiredShield.source, position = transform.position, amount = expiredShield.amount });
                }
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

    // for sourceStackable effects where several instances of the same type can coexist (e.g.
    // multiple Carrots' Psionic Bond on the same Shooter): removes only the one from bySource,
    // leaving every other source's instance untouched
    public void RemoveEffect<T>(Entity bySource) where T : StatusEffect
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i] is T && activeEffects[i].source == bySource)
            {
                activeEffects[i].OnExpire();
                activeEffects.RemoveAt(i);
                return;
            }
        }
    }
}
