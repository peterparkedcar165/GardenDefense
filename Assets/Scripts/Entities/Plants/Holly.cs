using UnityEngine;
using System.Collections.Generic;

public class Holly : Aura
{
    private HollyData HData => data as HollyData;

    // continuous-presence refresh for the passive aura, same shape as the old taunt aura this
    // kit replaced: every AuraTickInterval, every plant currently in range gets its Snow Ward
    // (re)confirmed for SnowWardLingerDuration - leaving range simply lets it tick down and
    // expire naturally after that many seconds instead of vanishing the instant you step out
    private const float AuraTickInterval = 0.25f;
    private float _auraTickTimer;

    [SerializeField] private GameObject blizzardPrefab;
    private ParticleSystem _auraParticle;

    private const string CannotStackNote = "\n\n<color=#888888><b>Cannot stack with other Hollies.</b></color>";

    // skill tree unlock ids
    public const string ResilientFrostUnlock = "holly_resilient_frost";
    public const string InstantSkillUnlock   = "holly_instant_skill";

    // both the ramp rate and the cap scale with Holly's own Heals & Shield Bonus, same as any
    // other outgoing shield in the game - plus a flat Magic Power bonus each, inside the
    // Heals & Shield Bonus multiplier same as every other source
    private float ShieldRampPerSecond => ((HData?.baseShieldRampPerSecond ?? 4f) + (HData?.path2ShieldRampPerLevel ?? 2f) * effectivePath2Level + magicPower * (HData?.shieldRampMagicPowerScaling ?? 0.1f)) * (1f + healingBonus);
    // Shield cap scales 100% with Holly's own Magic Armor - 100 Magic Armor grants +100 Shield cap
    private float ShieldMaxCap        => ((HData?.baseShieldMax ?? 40f)          + (HData?.path2ShieldMaxPerLevel ?? 20f) * effectivePath2Level + magicArmor + magicPower * (HData?.shieldMaxMagicPowerScaling ?? 0.5f)) * (1f + healingBonus);
    private float CoolingPerSecond    => (HData?.baseCoolingPerSecond ?? 1f)    + (HData?.path2CoolingPerLevel ?? 0.2f) * effectivePath2Level;
    private float ShieldTickInterval  => HData?.shieldTickInterval ?? 0.5f;
    // how long Snow Ward lingers once it stops being refreshed (leaves the wind/blizzard) -
    // Resilient Frost (skill tree) doubles it
    private float SnowWardLingerDuration =>
        (HData?.snowWardLingerDuration ?? 1f) * (SkillTreeManager.HasUnlock(this, ResilientFrostUnlock) ? 2f : 1f);
    private string SnowWardLingerText => $"{SnowWardLingerDuration:F0} second{(SnowWardLingerDuration == 1f ? "" : "s")}";
    // Path2 max: Snow Ward also grants this much flat Magic Resistance to whatever it's holding
    private float SnowWardMagicResistanceBonus => IsPath2Maxed ? (HData?.path2MaxMagicResistanceBonus ?? 0.15f) : 0f;

    // skillDamageAdder is generic (fed by the Reinforced Blizzard skill tree node) - Blizzard's
    // damage is otherwise a fully bespoke computed value, so this is the only thing that makes
    // that stat type do anything for her
    private float BlizzardDamagePerSecond   => (HData?.baseBlizzardDamagePerSecond ?? 20f) + (HData?.path3BlizzardDamagePerLevel ?? 4f) * effectivePath3Level + magicPower * (HData?.blizzardDamageMagicPowerScaling ?? 0.5f) + skillDamageAdder;
    // Path3 max: Blizzard's tick interval is halved, so it deals its damage twice as often
    private float BlizzardTickInterval      => (HData?.blizzardTickInterval ?? 0.5f) * (IsPath3Maxed ? 0.5f : 1f);
    private float BlizzardPowerMultiplier   => (HData?.baseBlizzardPowerMultiplier ?? 1.5f) + (HData?.path3BlizzardPowerPerLevel ?? 0.2f) * effectivePath3Level;
    private float BlizzardWidth             => 2f + (HData?.path3BlizzardWidthPerLevel ?? 0.1f) * effectivePath3Level;

    // display-only breakdowns: how much of the final ramp/cap/damage number above came from
    // Magic Power specifically, for the "[+X]" annotations in her tooltips
    private float ShieldRampMPBonus    => magicPower * (HData?.shieldRampMagicPowerScaling ?? 0.1f) * (1f + healingBonus);
    private float ShieldMaxMPBonus     => magicPower * (HData?.shieldMaxMagicPowerScaling ?? 0.5f) * (1f + healingBonus);
    private float BlizzardDamageMPBonus => magicPower * (HData?.blizzardDamageMagicPowerScaling ?? 0.5f);

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        _auraParticle = GetComponentInChildren<ParticleSystem>();

        // Frostguard Reflex (skill tree): free skill readiness on placement - bypasses
        // UnlockPath3() (which spends sun and adds to totalSunSpent) so this can't be abused for
        // an inflated uproot refund, same pattern Snowdrop/AloeVera use
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }
    }

    protected override void Update()
    {
        base.Update();
        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();

        // the aura particle's shape radius always matches her current Attack Range, so the
        // visual stays truthful across Path1 levels/buffs rather than being baked in at a fixed size
        if (_auraParticle != null)
        {
            ParticleSystem.ShapeModule shape = _auraParticle.shape;
            shape.radius = attackRange;
        }

        _auraTickTimer += Time.deltaTime;
        if (_auraTickTimer >= AuraTickInterval)
        {
            _auraTickTimer -= AuraTickInterval;
            foreach (Plant plant in GetPlantsInRange())
                ApplySnowWard(plant, this, SnowWardLingerDuration, ShieldRampPerSecond, ShieldMaxCap, CoolingPerSecond, ShieldTickInterval, SnowWardMagicResistanceBonus);
        }
    }

    private List<Plant> GetPlantsInRange()
    {
        List<Plant> result = new List<Plant>();
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) <= attackRange)
                result.Add(plant);
        }
        return result;
    }

    // Holly's only job here is applying/refreshing the Snow Ward status effect on nearby plants -
    // everything the ward actually does (shield ramp, cooling, Sandstorm immunity) lives entirely
    // in SnowWardEffect itself, not here. shared by this Holly's own passive tick and by her
    // Blizzard skill. exclusivity ("largest shield wins, no stacking, only the owning holly can
    // update it") is resolved here by cap (Power), never by Entity.ApplyEffect's generic same-type
    // shield comparison - see SnowWardEffect's own header comment for why that generic check is
    // the wrong tool for a shield that ramps up instead of landing at full strength immediately
    public static void ApplySnowWard(Plant plant, Holly source, float duration, float rampPerSecond, float shieldMax, float coolingPerSecond, float tickInterval, float magicResistanceBonus)
    {
        if (plant == null || !plant.IsAlive || source == null) return;

        SnowWardEffect existing = plant.GetEffect<SnowWardEffect>();
        if (existing != null)
        {
            if (existing.source == source)
            {
                existing.RefreshPresence(duration, rampPerSecond, shieldMax, coolingPerSecond, tickInterval, magicResistanceBonus);
                return;
            }
            if (existing.Power >= shieldMax) return;
            plant.RemoveEffect<SnowWardEffect>();
        }

        plant.ApplyEffect(new SnowWardEffect(plant, duration, 1, source, rampPerSecond, shieldMax, coolingPerSecond, tickInterval, magicResistanceBonus));
    }

    protected override void Attack()
    {
        base.Attack();
        float magicArmorBonus = IsPath1Maxed ? magicArmor * 0.15f : 0f;
        foreach (Insect insect in GetInsectsInRange())
            insect.Damage(attackDamage + magicArmorBonus, damageType, elementalType, this, false, new DamageTag[] { DamageTag.AoE, DamageTag.Attack });
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        // preview matches the real Blizzard exactly: same sprite (pulled straight off the
        // prefab's own visual, a plain white square) and a constant length equal to skillRadius -
        // Blizzard always travels that full distance regardless of where within range you click,
        // so the length should never vary with the live aim point either
        Sprite blizzardSprite = blizzardPrefab != null ? blizzardPrefab.GetComponentInChildren<SpriteRenderer>()?.sprite : null;
        SkillTargetingManager.instance.BeginTargeting(skillRadius, OnTargetConfirmed, transform.position, skillRadius,
            asLine: true, lineWidth: BlizzardWidth, lineSprite: blizzardSprite, fixedLineLength: skillRadius);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (blizzardPrefab == null) return;
        skillCooldownTimer = skillCooldown;

        Vector2 dir = (Vector2)position - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        GameObject obj = Instantiate(blizzardPrefab, transform.position, Quaternion.identity);
        Blizzard blizzard = obj.GetComponent<Blizzard>();
        // the power multiplier only boosts how fast Snow Ward's shield regenerates, not its cap -
        // Blizzard grants the same shield ceiling as the passive, just filled in faster
        blizzard?.Initialize(transform.position, dir, BlizzardWidth, skillDuration, skillRadius,
            BlizzardDamagePerSecond, BlizzardTickInterval, this,
            ShieldRampPerSecond * BlizzardPowerMultiplier, ShieldMaxCap,
            CoolingPerSecond * BlizzardPowerMultiplier, ShieldTickInterval, SnowWardMagicResistanceBonus, SnowWardLingerDuration);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (HData?.path1AttackDamagePerLevel ?? 3f) * level;
        baseMagicArmor   = data.baseMagicArmor   + (HData?.path1MagicArmorPerLevel   ?? 4) * level;
        baseAttackRange  = data.baseAttackRange  + (HData?.path1AttackRangePerLevel  ?? 0.2f) * level;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (HData?.path3SkillDurationPerLevel ?? 1f) * level;
        baseSkillRadius    = data.baseSkillRadius    + (HData?.path3BlizzardLengthPerLevel ?? 0.5f) * level;
    }

    public override string GetName() => "<b><color=#00FFFF>Holly</color></b>";
    public override string GetDescription() =>
        $"The {GetName()} is a resilient ice bulwark who shields the plants around her from harm and the elements.";
    public override string GetAttackDescription() =>
        $"Conjures a spiraling cold wind around herself, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects within range.";

    public override string GetPassiveDescription() =>
        $"Plants within the spiraling wind are blessed with <color=#00FFFF><b>Snow Ward</b></color>, granting a rampant <color=#00CED1><b>Shield</b></color> of <color=green><b>{(int)ShieldRampPerSecond}</b></color>/s [<color=#FFB6C1><b>+{ShieldRampMPBonus:F0}</b></color>], up to <color=#00CED1><b>{ShieldMaxCap:F0}</b></color> [<color=#FFB6C1><b>+{ShieldMaxMPBonus:F0}</b></color>], " +
        $"cooling them from the hot weather by <color=green><b>{CoolingPerSecond:F1}</b></color>/s, and protecting them against the negative effects of <color=#E8D9A8><b>Sandstorm</b></color>.\n\n" +
        $"The effect lingers for <color=green><b>{SnowWardLingerText}</b></color>. <color=#00CED1><b>Shield</b></color> ramp and cap scale with {GetName()}'s <color=#FF6B81><b>Heals & Shield Bonus</b></color>." +
        CannotStackNote;
    public override string GetSkillDesription() =>
        $"Conjures a blizzard blowing toward a targeted location for <color=green><b>{skillDuration:F0}s</b></color>, dealing <color=green><b>{BlizzardDamagePerSecond:F0}</b></color> [<color=#FFB6C1><b>+{BlizzardDamageMPBonus:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} per second to insects within it, " +
        $"and afflicting plants within it with <color=#00FFFF><b>Snow Ward</b></color>, granting <color=#00CED1><b>Shield</b></color> of <color=green><b>{(int)(ShieldRampPerSecond * BlizzardPowerMultiplier)}</b></color>/s and cooling of <color=green><b>{(CoolingPerSecond * BlizzardPowerMultiplier):F1}</b></color>/s.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = HData?.path1AttackDamagePerLevel ?? 3f;
        float arpl = HData?.path1MagicArmorPerLevel ?? 4;
        float rgpl = HData?.path1AttackRangePerLevel ?? 0.2f;
        string desc = details
            ? $"Conjures a spiraling cold wind around herself, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects within range."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=#FF69B4><b>Base Magic Armor</b></color> by <color=green><b>{arpl:F0}</b></color> per level. [<color=green><b>+{arpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rgpl:F1}</b></color> per level. [<color=green><b>+{rgpl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, details ? $"Attacks deal increased damage equal to <color=green><b>15%</b></color> of {GetName()}'s <color=#FF69B4><b>Magic Armor</b></color>." : $"Attacks deal <color=#FF69B4><b>+{magicArmor * 0.15f:F0}</b></color> increased damage.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float ramppl = HData?.path2ShieldRampPerLevel ?? 2f;
        float maxpl  = HData?.path2ShieldMaxPerLevel ?? 20f;
        float coolpl = HData?.path2CoolingPerLevel ?? 0.2f;
        string desc = details
            ? $"Plants within the spiraling wind are blessed with <color=#00FFFF><b>Snow Ward</b></color>, granting a rampant <color=#00CED1><b>Shield</b></color> of <color=green><b>[({HData?.baseShieldRampPerSecond ?? 4f:F0}) + ({ramppl:F0}/Lvl.) + <color=#FFB6C1>{(HData?.shieldRampMagicPowerScaling ?? 0.1f) * 100f:F0}% Magic Power</color>]</b></color>/s, up to <color=#00CED1><b>[({HData?.baseShieldMax ?? 40f:F0}) + ({maxpl:F0}/Lvl.) + <color=#FFB6C1>{(HData?.shieldMaxMagicPowerScaling ?? 0.5f) * 100f:F0}% Magic Power</color>]</b></color>, " +
              $"cooling them from the hot weather by <color=green><b>[({HData?.baseCoolingPerSecond ?? 1f:F1}) + ({coolpl:F1}/Lvl.)]</b></color>/s, and protecting them against the negative effects of <color=#E8D9A8><b>Sandstorm</b></color>.\n\n" +
              $"The effect lingers for <color=green><b>{SnowWardLingerText}</b></color>. <color=#00CED1><b>Shield</b></color> ramp and cap scale with {GetName()}'s <color=#FF6B81><b>Heals & Shield Bonus</b></color>." +
              CannotStackNote
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=#00CED1><b>Shield</b></color> ramp by <color=green><b>{ramppl:F1}</b></color>/s per level. [<color=green><b>+{ramppl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"Increase <color=#00CED1><b>Shield</b></color> cap by <color=green><b>{maxpl:F0}</b></color> per level. [<color=green><b>+{maxpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase cooling by <color=green><b>{coolpl:F1}</b></color>/s per level. [<color=green><b>+{coolpl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"<color=#00FFFF><b>Snow Ward</b></color> also grants <color=#FF69B4><b>{(HData?.path2MaxMagicResistanceBonus ?? 0.15f) * 100f:F0}% Magic Resistance</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float dmgpl = HData?.path3BlizzardDamagePerLevel ?? 4f;
        float powpl = HData?.path3BlizzardPowerPerLevel ?? 0.2f;
        float durpl = HData?.path3SkillDurationPerLevel ?? 1f;
        float lenpl = HData?.path3BlizzardLengthPerLevel ?? 0.5f;
        float widpl = HData?.path3BlizzardWidthPerLevel ?? 0.1f;
        string desc = details
            ? $"Conjures a blizzard blowing toward a targeted location for <color=green><b>{skillDuration:F0}s</b></color>, dealing <color=green><b>[({HData?.baseBlizzardDamagePerSecond ?? 20f:F0}) + ({dmgpl:F0}/Lvl.) + <color=#FFB6C1>{(HData?.blizzardDamageMagicPowerScaling ?? 0.5f) * 100f:F0}% Magic Power</color>]</b></color> {PlantData.DamageTypeLabel(damageType)} per second to insects within it, " +
              $"and afflicting plants within it with the same <color=#00FFFF><b>Snow Ward</b></color> as her Passive, with its <color=#00CED1><b>Shield</b></color> regenerating at <color=green><b>[({HData?.baseBlizzardPowerMultiplier ?? 1.5f:F1}x) + ({powpl:F1}/Lvl.)]</b></color> the rate (its cap is unaffected)."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase damage by <color=green><b>{dmgpl:F0}</b></color> per second per level. [<color=green><b>+{dmgpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase power multiplier by <color=green><b>{powpl:F1}x</b></color> per level. [<color=green><b>+{powpl * effectivePath3Level:F1}x</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase length by <color=green><b>{lenpl:F1}</b></color> per level. [<color=green><b>+{lenpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase width by <color=green><b>{widpl:F1}</b></color> per level. [<color=green><b>+{widpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path3Level, "Blizzard's tick interval is halved, dealing its damage twice as often.")}\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
