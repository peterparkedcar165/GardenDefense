using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Waterlily : Shooter
{
    public float skillAoERadius;
    public float bubbleDamage;
    public float slowProcChance;
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

    public float SlowDuration => WLData?.baseSlowDuration ?? 6f;
    // base stack is 1, each passive level raises the cap; maxSlowStacksAdder is the skill tree's
    // flat bonus on top (see PlantStatApplier.MaxSlowStacksFlat)
    public int maxSlowStacksAdder;
    public int MaxSlowStacks => 1 + (WLData?.path2MaxSlowStacksPerLevel ?? 1) * effectivePath2Level + maxSlowStacksAdder;

    // applies or refreshes the stacking slow on a direct hit.
    // if this waterlily's own cap can't push the stack any higher (e.g. a lower level waterlily
    // hitting a target already stacked up by a stronger one), the level never drops, only the
    // duration refreshes
    public void ApplyStackingSlow(Insect insect)
    {
        if (insect == null || !insect.IsAlive) return;
        float procChance = slowProcChance * (1f + bonusEffectChance);
        bool proc = Random.value < procChance;
        // max level passive: a failed roll gets a second attempt at the same chance
        if (!proc && IsPath2Maxed)
            proc = Random.value < procChance;
        if (!proc) return;
        int currentStacks = insect.GetEffect<SlowEffect>()?.level ?? 0;
        int attemptedStacks = Mathf.Min(currentStacks + 1, MaxSlowStacks);
        int newStacks = Mathf.Max(currentStacks, attemptedStacks);
        insect.ApplyEffect(new SlowEffect(insect, SlowDuration, newStacks, this));
    }

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        skillAoERadius = data.baseSkillRadius;
        slowProcChance = WLData?.slowProcChance ?? 0.5f;
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

    // skill tree node 10.2: any Water damage this Waterlily deals to an insect currently caught
    // in one of her own Bubble Prisons extends that bubble's remaining duration
    private void OnAnyEntityHit(EntityEventData hit)
    {
        if (hit.source != this) return;
        if (hit.elementalType != ElementalType.Water) return;
        if (!SkillTreeManager.HasUnlock(this, ExtendBubbleDurationUnlock)) return;
        if (hit.target is not Insect insect) return;
        BubblePrisonEffect bubble = insect.GetEffect<BubblePrisonEffect>();
        if (bubble != null) bubble.duration += BubbleDurationExtension;
    }

    public override void UpdateStats()
    {
        bool extraTarget = SkillTreeManager.HasUnlock(this, ExtraTargetUnlock);
        attackDamageTotalMultiplier = extraTarget ? ExtraTargetDamageMultiplier : 1f;
        base.UpdateStats();
        if (IsPath1Maxed)
            onHitEffectiveness *= 1f + (WLData?.path1MaxOnHitEffectivenessBonus ?? 0.5f);
        if (extraTarget)
            onHitEffectiveness *= 1f - ExtraTargetOnHitReduction;
        float bubblepl = WLData?.path3BubbleDamagePerLevel ?? 12f;
        bubbleDamage = (WLData?.baseBubblePrisonImpactDamage ?? 0f) + bubblepl * effectivePath3Level + skillDamageMultiplier * magicPower;
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
        baseAttackSpeed = data.baseAttackSpeed + level * (WLData?.path1AttackSpeedPerLevel ?? 0.3f);
    }

    public override void OnPath2Upgrade(int level)
    {
        slowProcChance = (WLData?.slowProcChance ?? 0.5f) + level * (WLData?.path2SlowProcChancePerLevel ?? 0.05f);
        // passive base effect: +1 Piercing always, plus +1 more per level
        piercingAdder = 1 + level;
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
            bubble.Initialize(position, skillAoERadius, skillDuration, bubbleDamage, this);
    }

    public override string GetName() => "<b><color=#3399FF>Waterlily</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} shoots her targets with little bubbles that can slow and pierce through them. She can also imprison her foes with her larger bubble.";

    public override string GetAttackDescription() =>
        $"Blow little bubbles towards her target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}.";

    public override string GetSkillDesription()
    {
        float bubblepl = WLData?.path3BubbleDamagePerLevel ?? 12f;
        return $"Blow a large bubble onto a targetted area, trapping insects within the bubble while dealing <color={PlantData.ElementalColor(elementalType)}><b>{(WLData?.baseBubblePrisonImpactDamage ?? 0f) + bubblepl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} upon impact, and keeping them airborne for <color=green><b>{skillDuration}</b></color> seconds within a <color=green><b>{skillAoERadius:F1}</b></color> radius.";
    }

    public override string GetPassiveDescription()
    {
        return $"Dealing damage with attacks has a <color=green><b>{slowProcChance * 100f:F0}%</b></color> chance to apply a stacking <color=#87CEEB><b>Slow</b></color> for <color=green><b>{SlowDuration:F1}s</b></color>, up to <color=#87CEEB><b>{MaxSlowStacks}</b></color> stacks.\n\n" +
               $"Attacks pierce through <color=green><b>{piercing}</b></color> additional target(s).";
    }

    public override string GetPath1Description(bool details = false)
    {
        float dmgpl   = WLData?.path1AttackDamagePerLevel ?? 5f;
        float rangepl = WLData?.path1AttackRangePerLevel ?? 0.5f;
        float aspl    = WLData?.path1AttackSpeedPerLevel ?? 0.3f;
        string desc = details
            ? $"Blows little bubbles towards her target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F1}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Increase <color=green><b>On-Hit</b></color> effect effectiveness by <color=green><b>{(WLData?.path1MaxOnHitEffectivenessBonus ?? 0.5f) * 100f:F0}%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        int stackspl  = WLData?.path2MaxSlowStacksPerLevel   ?? 1;
        float chancepl = WLData?.path2SlowProcChancePerLevel ?? 0.05f;
        string desc = details
            ? $"Dealing damage with attacks has a <color=green><b>[({(WLData?.slowProcChance ?? 0.5f) * 100f:F0}%) + ({chancepl * 100f:F0}%/Lvl.)]</b></color> chance to apply a stacking <color=#87CEEB><b>Slow</b></color> for <color=green><b>{SlowDuration:F1}s</b></color>, up to <color=#87CEEB><b>{MaxSlowStacks}</b></color> stacks.\n\n" +
              $"Attacks pierce through <color=green><b>1</b></color> additional target, plus <color=green><b>1</b></color> more per level."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase max <color=#87CEEB><b>Slow</b></color> stacks by <color=green><b>{stackspl}</b></color> per level. [<color=green><b>+{stackspl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase <color=#87CEEB><b>Slow</b></color> chance by <color=green><b>{chancepl * 100f:F0}%</b></color> per level. [<color=green><b>+{chancepl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Piercing</b></color> by <color=green><b>1</b></color> per level. [<color=green><b>+{effectivePath2Level}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"A failed <color=#87CEEB><b>Slow</b></color> proc gets a second chance to apply, at the same <color=green><b>{(WLData?.slowProcChance ?? 0.5f) * 100f + chancepl * 100f * effectivePath2Level:F0}%</b></color> chance.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float bubblepl = WLData?.path3BubbleDamagePerLevel  ?? 12f;
        float durpl    = WLData?.path3SkillDurationPerLevel ?? 2f;
        float radiuspl = WLData?.path3RadiusPerLevel        ?? 0.2f;
        string desc = details
            ? $"Blows a large bubble onto a targeted area, trapping insects within the bubble while dealing <color=green><b>[({WLData?.baseBubblePrisonImpactDamage ?? 0f:F0}) + ({bubblepl:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.DamageTypeLabel(damageType)} upon impact, and keeping them airborne for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds within a <color=green><b>[({data.baseSkillRadius:F1}) + ({radiuspl:F2}/Lvl.)]</b></color> radius."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase impact damage by <color=green><b>{bubblepl:F0}</b></color> per level. [<color=green><b>+{bubblepl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase bubble radius by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Imprisoned insects slowly rise during the effect.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
