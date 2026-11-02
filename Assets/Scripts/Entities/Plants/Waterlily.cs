using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Waterlily : Shooter
{
    public float AoERange, baseAoERange, AoERangeMultiplier, AoERangeAdder;
    public float skillAoERadius;
    public float bubbleDamage;
    public float splashDamage;
    [SerializeField] private GameObject bubbleTrapPrefab;

    private WaterlilyData WLData => data as WaterlilyData;

    // skill tree node unlock ids
    public const string InstantSkillUnlock = "waterlily_instant_skill";
    public const string BubbleBacktrackUnlock = "waterlily_bubble_backtrack";
    public const string BubblePopDamageUnlock = "waterlily_bubble_pop_damage";
    public const string ExtraTargetUnlock = "waterlily_extra_target";
    public const string ExtendBubbleDurationUnlock = "waterlily_extend_bubble_duration";

    private const float ExtraTargetDamageMultiplier = 0.5f;
    private const float ExtraTargetOnHitReduction = 0.35f;
    private const float BubbleDurationExtension = 0.2f;

    private float SplashDamageRatio => WLData?.splashDamageRatio ?? 0.5f;

    // soaked duration, split into base and magic power parts for the description brackets
    private float SoakDurationBase => (WLData?.baseSoakDuration ?? 4f) + (WLData?.path2SoakDurationPerLevel ?? 0.5f) * effectivePath2Level;
    private float SoakDurationMP   => (WLData?.soakDurationMP ?? 1f) * magicPower / 100f;
    public float SoakDuration      => SoakDurationBase + SoakDurationMP;

    // bubble prison duration and the extra damage imprisoned insects take, same base/MP split
    private float BubbleDurationMP   => (WLData?.bubbleDurationMP ?? 2f) * magicPower / 100f;
    private float BubbleDuration     => skillDuration + BubbleDurationMP;
    private float DamageTakenBase    => (WLData?.baseBubbleDamageTaken ?? 0.08f) + (WLData?.path3DamageTakenPerLevel ?? 0.04f) * effectivePath3Level;
    private float DamageTakenMP      => (WLData?.bubbleDamageTakenMP ?? 0.05f) * magicPower / 100f;
    private float BubbleDamageTaken  => DamageTakenBase + DamageTakenMP;

    // flat cap with no per level scaling; maxSoakStacksAdder is the skill tree's flat bonus on top
    // (see PlantStatApplier.MaxSlowStacksFlat)
    public int maxSoakStacksAdder;
    public int MaxSoakStacks => (WLData?.maxSoakStacks ?? 4) + maxSoakStacksAdder;

    // how many nearest insects around the main target the splash hits, +1 per attack path level
    public int SplashTargets => (WLData?.splashMaxExtraTargets ?? 2) + (WLData?.path1SplashTargetsPerLevel ?? 1) * effectivePath1Level;

    private float MaxSoakStunDuration => WLData?.maxSoakStunDuration ?? 0.5f;

    // applies or refreshes the stacking Soaked, called on both the direct hit and splash damage.
    // if this waterlily's own cap can't push the stack any higher (e.g. a lower level waterlily
    // hitting a target already stacked up by a stronger one), the level never drops, only the
    // duration refreshes
    public void ApplyStackingSoak(Insect insect)
    {
        if (insect == null || !insect.IsAlive) return;
        int currentStacks = insect.GetEffect<SoakedEffect>()?.level ?? 0;
        // Path 2 max level: inflicts two stacks per hit instead of one, still clamped to
        // MaxSoakStacks below, so it can never push a target past its actual stack cap
        int stacksPerHit = IsPath2Maxed ? 2 : 1;
        int attemptedStacks = Mathf.Min(currentStacks + stacksPerHit, MaxSoakStacks);
        int newStacks = Mathf.Max(currentStacks, attemptedStacks);
        insect.ApplyEffect(new SoakedEffect(insect, SoakDuration, newStacks, this));
    }

    // skill max bonus: Bubble Prison popping leaves the insect at this waterlily's full stack cap
    public void ApplyFullSoak(Insect insect)
    {
        if (insect == null || !insect.IsAlive) return;
        int currentStacks = insect.GetEffect<SoakedEffect>()?.level ?? 0;
        insect.ApplyEffect(new SoakedEffect(insect, SoakDuration, Mathf.Max(currentStacks, MaxSoakStacks), this));
    }

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        baseAoERange   = WLData?.baseAoERange ?? 0.75f;
        skillAoERadius = data.baseSkillRadius;
        Entity.OnEntityHit += OnAnyEntityHit;

        // LoadData already applied any skill tree path1LevelAdder/path2LevelAdder/
        // path3LevelAdder ("+1 Effective X Point" nodes) and recomputed
        // effectivePath1/2/3Level from them, so re-running these three hooks here bakes that
        // virtual level straight into the relevant stats
        OnPath1Upgrade(effectivePath1Level);
        OnPath2Upgrade(effectivePath2Level);
        OnPath3Upgrade(effectivePath3Level);

        // free skill readiness on placement - deliberately bypasses UnlockPath3() (which spends
        // sun and adds to totalSunSpent) so this can't be abused for an inflated uproot refund
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Entity.OnEntityHit -= OnAnyEntityHit;
    }

    protected override void Update()
    {
        base.Update();
        AoERange = baseAoERange + AoERangeAdder + (baseAoERange * AoERangeMultiplier);
    }

    private void OnAnyEntityHit(EntityEventData hit)
    {
        if (hit.source != this) return;
        if (hit.target is not Insect insect) return;

        // attack path max bonus: any damage she deals to an insect already at her max Soaked
        // stacks stuns it. this event fires inside Damage, before the hit's own soak is applied,
        // so the hit that first reaches the cap doesn't stun, the next one does
        if (IsPath1Maxed && hit.damage > 0f && insect.IsAlive
            && (insect.GetEffect<SoakedEffect>()?.level ?? 0) >= MaxSoakStacks)
            insect.ApplyEffect(new StunEffect(insect, MaxSoakStunDuration, 1, this));

        // skill tree node 10.2: any Water damage this Waterlily deals to an insect currently caught
        // in one of her own Bubble Prisons extends that bubble's remaining duration
        if (hit.elementalType == ElementalType.Water && SkillTreeManager.HasUnlock(this, ExtendBubbleDurationUnlock))
        {
            BubblePrisonEffect bubble = insect.GetEffect<BubblePrisonEffect>();
            if (bubble != null) bubble.duration += BubbleDurationExtension;
        }
    }

    public override void UpdateStats()
    {
        bool extraTarget = SkillTreeManager.HasUnlock(this, ExtraTargetUnlock);
        attackDamageTotalMultiplier = extraTarget ? ExtraTargetDamageMultiplier : 1f;
        base.UpdateStats();
        if (extraTarget)
            onHitEffectiveness *= 1f - ExtraTargetOnHitReduction;
        // splash is a fixed fraction of the bubble's own damage, no per level or magic power scaling
        splashDamage = attackDamage * SplashDamageRatio;
        // impact damage no longer scales, magic power goes into the bubble's duration and damage taken
        bubbleDamage = WLData?.baseBubblePrisonImpactDamage ?? 0f;
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject primary = FindTarget();
        FireBubble(primary, target);

        // skill tree node 10.1: an extra bubble at the next-most-valid target under the same
        // targeting rule, excluding whichever insect the primary shot already picked
        if (SkillTreeManager.HasUnlock(this, ExtraTargetUnlock))
        {
            GameObject secondary = FindSecondTarget(primary);
            if (secondary != null)
                FireBubble(secondary, PredictTargetPosition(secondary));
        }
    }

    private GameObject FindSecondTarget(GameObject exclude)
    {
        List<Insect> pool = new List<Insect>(Insect.allInsects);
        if (exclude != null)
        {
            Insect excludeInsect = exclude.GetComponent<Insect>();
            if (excludeInsect != null) pool.Remove(excludeInsect);
        }
        switch (targeting)
        {
            case TARGETING.First:     return FindFirst(pool);
            case TARGETING.Nearest:   return FindNearest(pool);
            case TARGETING.Last:      return FindLast(pool);
            case TARGETING.Strongest: return FindStrongest(pool);
            default:                  return null;
        }
    }

    private void FireBubble(GameObject targetObj, Vector3 targetPos)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        WaterlilyProjectile bubble = projectile.GetComponent<WaterlilyProjectile>();
        if (bubble != null)
        {
            bubble.SetTarget(targetObj);
            bubble.Initialize(targetPos, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * (WLData?.path1AttackDamagePerLevel ?? 5f);
        baseAttackRange = data.baseAttackRange + level * (WLData?.path1AttackRangePerLevel ?? 0.5f);
        // attack speed no longer scales per level, the attack path adds splash targets instead (SplashTargets)
        baseAttackSpeed = data.baseAttackSpeed;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAoERange = (WLData?.baseAoERange ?? 0.75f) + level * (WLData?.path2AoERangePerLevel ?? 0.05f);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (WLData?.path3SkillDurationPerLevel ?? 2f) * level;
        skillAoERadius    = data.baseSkillRadius   + (WLData?.path3RadiusPerLevel        ?? 0.2f) * level;
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(skillAoERadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (bubbleTrapPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        GameObject obj = Instantiate(bubbleTrapPrefab, transform.position, Quaternion.identity);
        BubblePrison bubble = obj.GetComponent<BubblePrison>();
        if (bubble != null)
            bubble.Initialize(position, skillAoERadius, BubbleDuration, bubbleDamage, BubbleDamageTaken, this);
    }

    public override string GetName() => "<b><color=#3399FF>Waterlily</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} shoots her targets with little bubbles that soak surrounding insects. She can also imprison her foes with her larger bubble, leaving them exposed to her allies.";

    public override string GetAttackDescription() =>
        $"Blow little bubbles towards her target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}.";

    public override string GetSkillDesription() =>
        $"Blow a large bubble onto a targetted area, trapping insects within the bubble while dealing <color={PlantData.ElementalColor(elementalType)}><b>{bubbleDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} upon impact, and keeping them airborne for <color=green><b>{skillDuration:F1}</b></color> [<color=#FFB6C1><b>+{BubbleDurationMP:F1}</b></color>] seconds within a <color=green><b>{skillAoERadius:F1}</b></color> radius.\n\n" +
        $"Imprisoned insects take <color=green><b>{DamageTakenBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{DamageTakenMP * 100f:F0}%</b></color>] increased damage from all sources.";

    private string SoakLine() =>
        $"Dealing damage with attacks and splash damage inflicts a stacking <color=#4FC3F7><b>Soaked</b></color> for <color=green><b>{SoakDurationBase:F1}s</b></color> [<color=#FFB6C1><b>+{SoakDurationMP:F1}s</b></color>], slowing by <color=green><b>{SoakedEffect.baseSoakSlowness * 100f:F0}%</b></color> + <color=green><b>{SoakedEffect.soakSlownessPerLevel * 100f:F0}%</b></color> per stack, up to <color=#4FC3F7><b>{MaxSoakStacks}</b></color> stacks. <color=#4FC3F7><b>Soaked</b></color> insects are grounded.";

    private string SplashLine() =>
        $"Attacks deal <color={PlantData.ElementalColor(elementalType)}><b>{splashDamage:F1}</b></color> {PlantData.DamageTypeLabel(damageType)} to the <color=green><b>{SplashTargets}</b></color> nearest insects around the target within a <color=green><b>{AoERange:F2}</b></color> radius.";

    public override string GetPassiveDescription() =>
        $"{SplashLine()}\n\n{SoakLine()}";

    public override string GetPath1Description(bool details = false)
    {
        float dmgpl     = WLData?.path1AttackDamagePerLevel ?? 5f;
        float rangepl   = WLData?.path1AttackRangePerLevel ?? 0.5f;
        int   targetspl = WLData?.path1SplashTargetsPerLevel ?? 1;
        string desc = details
            ? $"Blows little bubbles towards her target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}. Splash hits the <color=green><b>[({WLData?.splashMaxExtraTargets ?? 2}) + ({targetspl}/Lvl.)]</b></color> nearest insects around the target."
            : $"{GetAttackDescription()} Splash hits the <color=green><b>{SplashTargets}</b></color> nearest insects around the target.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase splash targets by <color=green><b>{targetspl}</b></color> per level. [<color=green><b>+{targetspl * effectivePath1Level}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Dealing damage to an insect at max <color=#4FC3F7><b>Soaked</b></color> stacks <color=#FFD700><b>Stuns</b></color> it for <color=green><b>{MaxSoakStunDuration:F1}s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float soakpl = WLData?.path2SoakDurationPerLevel ?? 0.5f;
        float aoepl  = WLData?.path2AoERangePerLevel     ?? 0.05f;
        string desc = details
            ? $"Attacks deal <color={PlantData.ElementalColor(elementalType)}><b>[{SplashDamageRatio * 100f:F0}% <color=green><b>Attack Damage</b></color>]</b></color> {PlantData.DamageTypeLabel(damageType)} to the <color=green><b>{SplashTargets}</b></color> nearest insects around the target within a <color=green><b>[({WLData?.baseAoERange ?? 0.75f:F2}) + ({aoepl:F2}/Lvl.)]</b></color> radius.\n\n" +
              $"Dealing damage with attacks and splash damage inflicts a stacking <color=#4FC3F7><b>Soaked</b></color> for <color=green><b>[({WLData?.baseSoakDuration ?? 4f:F1}) + ({soakpl:F1}/Lvl.)]</b></color> [<color=#FFB6C1><b>+{(WLData?.soakDurationMP ?? 1f):F1}s per 100 Magic Power</b></color>] seconds, slowing by <color=green><b>{SoakedEffect.baseSoakSlowness * 100f:F0}%</b></color> + <color=green><b>{SoakedEffect.soakSlownessPerLevel * 100f:F0}%</b></color> per stack, up to <color=#4FC3F7><b>{MaxSoakStacks}</b></color> stacks. <color=#4FC3F7><b>Soaked</b></color> insects are grounded."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=#4FC3F7><b>Soaked</b></color> duration by <color=green><b>{soakpl:F1}s</b></color> per level. [<color=green><b>+{soakpl * effectivePath2Level:F1}s</b></color>]\n\n" +
               $"Increase splash radius by <color=green><b>{aoepl:F2}</b></color> per level. [<color=green><b>+{aoepl * effectivePath2Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Inflicts two stacks of <color=#4FC3F7><b>Soaked</b></color> instead of one.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float takenpl  = WLData?.path3DamageTakenPerLevel   ?? 0.04f;
        float durpl    = WLData?.path3SkillDurationPerLevel ?? 2f;
        float radiuspl = WLData?.path3RadiusPerLevel        ?? 0.2f;
        string desc = details
            ? $"Blows a large bubble onto a targeted area, trapping insects within the bubble while dealing <color=green><b>{bubbleDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} upon impact, and keeping them airborne for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> [<color=#FFB6C1><b>+{(WLData?.bubbleDurationMP ?? 2f):F1}s per 100 Magic Power</b></color>] seconds within a <color=green><b>[({data.baseSkillRadius:F1}) + ({radiuspl:F2}/Lvl.)]</b></color> radius.\n\n" +
              $"Imprisoned insects take <color=green><b>[({(WLData?.baseBubbleDamageTaken ?? 0.08f) * 100f:F0}%) + ({takenpl * 100f:F0}%/Lvl.)]</b></color> [<color=#FFB6C1><b>+{(WLData?.bubbleDamageTakenMP ?? 0.05f) * 100f:F0}% per 100 Magic Power</b></color>] increased damage from all sources."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase damage taken by imprisoned insects by <color=green><b>{takenpl * 100f:F0}%</b></color> per level. [<color=green><b>+{takenpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase bubble radius by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Imprisoned insects slowly rise during the effect, taking fall damage when it ends. When the bubble pops, insects inside are left at full <color=#4FC3F7><b>Soaked</b></color> stacks.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
