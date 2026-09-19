using UnityEngine;
using System.Collections.Generic;

public class PoisonShroom : Shooter
{
    private PoisonShroomData PSData => data as PoisonShroomData;
    public float PoisonBaseDPS => PSData?.basePoisonDPS ?? 0f;
    // duration scaling now lives on Path2 (passive), not Path1 (attack) - toxicSporeDurationAdder
    // is a flat bonus from the skill tree (see PlantStatApplier.ToxicSporeDurationFlat)
    public float toxicSporeDurationAdder;
    public float ToxicSporeDuration => ((PSData?.baseToxicSporeDuration ?? 3f) + (PSData?.path2ToxicSporeDurationPerLevel ?? 0.4f) * effectivePath2Level + toxicSporeDurationAdder) * (1 + passiveDuration);
    // fraction of the target's current health Toxic Spore deals as bonus damage per second
    public float PercentHealthDPS => (PSData?.basePercentHealthDPS ?? 0.012f) + (PSData?.path2PercentHealthDPSPerLevel ?? 0.004f) * effectivePath2Level;

    public float activeRadius;
    [SerializeField] private GameObject poisonBlobPrefab;

    // tracks whether this instance currently owns the DotCanCrit source granted by a maxed Path2,
    // so UpdateStats (which runs every frame) adds/removes it exactly once on each state change
    private bool _dotCanCritFromPath2;

    // skill tree node unlock ids
    public const string LingeringToxinsUnlock   = "poison_field_lingering";
    public const string DelayedBloomUnlock      = "poison_field_delayed_bloom";
    public const string InstantSkillUnlock      = "poison_instant_skill";
    public const string ToxicCatalystUnlock     = "poison_toxic_catalyst";
    public const string ExecutionersBloomUnlock = "poison_executioners_bloom";

    protected override void Awake()
    {
        base.Awake();
        LoadData();

        // LoadData already applied any skill tree path1LevelAdder/path3LevelAdder ("+1 Effective X
        // Point" nodes) and recomputed effectivePath1/3Level from them, so re-running these hooks
        // here bakes that virtual level straight into attackDamage/skillDuration/etc. - at level 0
        // (the common case with no adder) this reduces to exactly the plain base values these
        // lines used to assign directly. Path2Upgrade is a no-op for PoisonShroom (its Path2
        // scaling reads effectivePath2Level live in UpdateStats instead), so it's skipped here
        OnPath1Upgrade(effectivePath1Level);
        OnPath3Upgrade(effectivePath3Level); // also sets activeRadius from data.baseSkillRadius

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
        if (_dotCanCritFromPath2) RemoveDotCanCrit();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        PoisonShroomProjectile puff = projectile.GetComponent<PoisonShroomProjectile>();
        if (puff != null)
        {
            puff.SetTarget(FindTarget());
            puff.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    // avoids insects already carrying this Poison Shroom's own Toxic Spores, so its seeds
    // spread across the crowd instead of piling onto one target. if everything in range is
    // already infected, it re-applies to whichever has the least time left, refreshing it
    // right as it would otherwise fall off
    protected override GameObject FindTarget()
    {
        List<Insect> fresh = new List<Insect>();
        Insect leastTimeInsect = null;
        float leastTime = float.MaxValue;

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance > attackRange || !IsValidNightTarget(insect, distance)) continue;

            ToxicSporeEffect ownSpore = FindOwnToxicSpore(insect);
            if (ownSpore == null)
            {
                fresh.Add(insect);
            }
            else if (ownSpore.duration < leastTime)
            {
                leastTime = ownSpore.duration;
                leastTimeInsect = insect;
            }
        }

        if (fresh.Count > 0) return PickByTargeting(fresh);
        if (leastTimeInsect != null) return leastTimeInsect.gameObject;
        return base.FindTarget();
    }

    private ToxicSporeEffect FindOwnToxicSpore(Insect insect)
    {
        foreach (StatusEffect e in insect.activeEffects)
            if (e is ToxicSporeEffect spore && spore.source == this) return spore;
        return null;
    }

    private GameObject PickByTargeting(List<Insect> candidates)
    {
        switch (targeting)
        {
            case TARGETING.Nearest:   return FindNearest(candidates);
            case TARGETING.Last:      return FindLast(candidates);
            case TARGETING.Strongest: return FindStrongest(candidates);
            default:                  return FindFirst(candidates);
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * (PSData?.path1AttackDamagePerLevel ?? 8f);
        baseAttackSpeed  = data.baseAttackSpeed  + level * (PSData?.path1AttackSpeedPerLevel  ?? 0.08f);
        baseAttackRange  = data.baseAttackRange  + level * (PSData?.path1AttackRangePerLevel  ?? 0.1f);
    }

    public override void OnPath2Upgrade(int level) { }

    public override void UpdateStats()
    {
        // Executioner's Bloom trades Total Attack Damage for bonus Toxic Spore damage against
        // low-health insects (applied only in ToxicSporeEffect, not the Poison Field skill).
        // attackDamageTotalMultiplier is an input to base.UpdateStats()'s formula, so it's
        // toggled around the call like AcornSprout's Stun Specialist, rather than post-multiplying
        // the already-computed attackDamage
        bool executionersBloom = SkillTreeManager.HasUnlock(this, ExecutionersBloomUnlock);
        float damagePenalty = executionersBloom ? -0.25f : 0f;
        attackDamageTotalMultiplier += damagePenalty;

        base.UpdateStats();

        attackDamageTotalMultiplier -= damagePenalty;

        // Path2 max bonus: can deal Critical Damage with DoT effects, plus bonus Critical Chance.
        // DotCanCrit is a ref-counted toggle (other sources could grant it too), so it's only
        // added/removed on an actual state change rather than every frame
        if (IsPath2Maxed)
        {
            if (!_dotCanCritFromPath2) { AddDotCanCrit(); _dotCanCritFromPath2 = true; }
            criticalChance += 0.15f;
        }
        else if (_dotCanCritFromPath2)
        {
            RemoveDotCanCrit();
            _dotCanCritFromPath2 = false;
        }
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (PSData?.path3SkillDurationPerLevel ?? 1f) * level;
        activeRadius      = data.baseSkillRadius   + (PSData?.path3RadiusPerLevel        ?? 0.2f) * level;
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(activeRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (poisonBlobPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        float fieldDPS      = PoisonBaseDPS + skillDamageMultiplier * magicPower;
        float fieldRadius   = activeRadius;
        float fieldDuration = skillDuration;

        // Delayed Bloom: starts 25% smaller, then blooms outward to 1.5x that starting size
        // over the first 60% of its lifetime (see PoisonField.Update), holding there afterward -
        // lasts 4s longer to make up for the slow start
        if (SkillTreeManager.HasUnlock(this, DelayedBloomUnlock))
        {
            fieldRadius   *= 0.75f;
            fieldDuration += 4f;
        }

        GameObject obj = Instantiate(poisonBlobPrefab, transform.position, Quaternion.identity);
        PoisonBlob blob = obj.GetComponent<PoisonBlob>();
        if (blob != null)
            blob.Initialize(position, fieldRadius, fieldDuration, this, fieldDPS);
    }

    public override string GetName() => $"<b><color=purple>{(data != null ? data.displayName : "Poison Shroom")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} fires toxic spores that poison the target over time.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl    = PSData?.path1AttackDamagePerLevel ?? 8f;
        float aspl    = PSData?.path1AttackSpeedPerLevel ?? 0.08f;
        float rangepl = PSData?.path1AttackRangePerLevel ?? 0.1f;
        string desc = details
            ? $"Fires <color=purple><b>Toxic Spores</b></color> at the target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} per second for <color=green><b>{ToxicSporeDuration:F1}</b></color> seconds."
            : $"Fires <color=purple><b>Toxic Spores</b></color> at the target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} per second for <color=green><b>{ToxicSporeDuration:F1}</b></color> seconds.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Attacks splash onto nearby insects within a <color=green><b>1</b></color> radius, applying <color=purple><b>Toxic Spores</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float durpl  = PSData?.path2ToxicSporeDurationPerLevel ?? 0.4f;
        float pctBase = PSData?.basePercentHealthDPS ?? 0.012f;
        float pctpl   = PSData?.path2PercentHealthDPSPerLevel  ?? 0.004f;
        string desc = details
            ? $"While active, <color=purple><b>Toxic Spore</b></color> deals an additional <color=green><b>[({pctBase * 100f:F1}%) + ({pctpl * 100f:F1}%/Lvl.)]</b></color> of the target's current health per second."
            : $"While active, <color=purple><b>Toxic Spore</b></color> deals an additional <color=green><b>{PercentHealthDPS * 100f:F1}%</b></color> of the target's current health per second.";
        string maxBonus = $"The {GetName()} is able to deal <color=green><b>Critical Damage</b></color> with <color=#9400D3><b>Damage Over Time</b></color> effects. Increase <color=green><b>Critical Chance</b></color> by <color=green><b>15%</b></color>.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=purple><b>Toxic Spore</b></color>'s current health damage by <color=green><b>{pctpl * 100f:F1}%</b></color> per level. [<color=green><b>+{pctpl * effectivePath2Level * 100f:F1}%</b></color>]\n\n" +
               $"Increase <color=purple><b>Toxic Spore</b></color> duration by <color=green><b>{durpl:F1}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path2Level, maxBonus)}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl    = PSData?.path3SkillDurationPerLevel ?? 1f;
        float radiuspl = PSData?.path3RadiusPerLevel        ?? 0.2f;
        string desc = details
            ? $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>[({data.baseSkillRadius:F1}) + ({radiuspl:F1}/Lvl.)]</b></color> radius that lasts <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. Insects inside take <color=green><b>{PoisonBaseDPS:F0}</b></color> <color=#FFB6C1>[+{skillDamageMultiplier * 100f:F0}% Magic Power]</color> {PlantData.DamageTypeLabel(damageType)} per second, and all debuffs on them are frozen in time."
            : $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>{activeRadius:F1}</b></color> radius that lasts <color=green><b>{skillDuration:F0}</b></color> seconds. Insects inside take <color={PlantData.ElementalColor(elementalType)}><b>{PoisonBaseDPS:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} per second, and all debuffs on them are frozen in time.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase field duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase field radius by <color=green><b>{radiuspl:F1}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Each tick of damage from the field inflicts <color=purple><b>Toxic Spore</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
