using UnityEngine;
using System.Collections.Generic;

public class PoisonShroom : Shooter
{
    private PoisonShroomData PSData => data as PoisonShroomData;
    public float PoisonBaseDPS => PSData?.basePoisonDPS ?? 0f;
    public float ToxicSporeDuration => ((PSData?.baseToxicSporeDuration ?? 3f) + (PSData?.path1ToxicSporeDurationPerLevel ?? 0.4f) * effectivePath1Level) * (1 + passiveDuration);

    public float activeRadius;
    [SerializeField] private GameObject poisonBlobPrefab;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        activeRadius = data.baseSkillRadius;
        AddDotCanCrit();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        RemoveDotCanCrit();
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
        baseCriticalChance    = data.baseCriticalChance    + (PSData?.baseCritChanceBonus ?? 0.1f)         + (PSData?.path2CritChancePerLevel ?? 0.03f) * effectivePath2Level;
        baseelementalAffinity = data.baseelementalAffinity + (PSData?.baseElementalAffinityBonus ?? 0.15f) + (PSData?.path2ElementalAffinityPerLevel ?? 0.04f) * effectivePath2Level;
        float eecBonus    = IsPath2Maxed ? (PSData?.path2MaxElementalEffectChanceBonus ?? 0.1f) : 0f;
        float dotDurBonus = IsPath2Maxed ? (PSData?.path2MaxDotDurationBonus           ?? 0.5f)  : 0f;
        elementalEffectChanceAdder += eecBonus;
        dotDurationAdder += dotDurBonus;
        base.UpdateStats();
        elementalEffectChanceAdder -= eecBonus;
        dotDurationAdder -= dotDurBonus;
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
        float fieldDPS = PoisonBaseDPS + skillDamageMultiplier * magicPower;
        GameObject obj = Instantiate(poisonBlobPrefab, transform.position, Quaternion.identity);
        PoisonBlob blob = obj.GetComponent<PoisonBlob>();
        if (blob != null)
            blob.Initialize(position, activeRadius, skillDuration, this, fieldDPS);
    }

    public override string GetName() => $"<b><color=purple>{(data != null ? data.displayName : "Poison Shroom")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} fires toxic spores that poison the target over time, and can inflict Critical Damage with its Damage Over Time effects.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl    = PSData?.path1AttackDamagePerLevel ?? 8f;
        float aspl    = PSData?.path1AttackSpeedPerLevel ?? 0.08f;
        float rangepl = PSData?.path1AttackRangePerLevel ?? 0.1f;
        float durpl   = PSData?.path1ToxicSporeDurationPerLevel ?? 0.4f;
        string desc = details
            ? $"Fires <color=purple><b>Toxic Spores</b></color> at the target, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second for <color=green><b>{ToxicSporeDuration:F1}</b></color> seconds."
            : $"Fires <color=purple><b>Toxic Spores</b></color> at the target, dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second for <color=green><b>{ToxicSporeDuration:F1}</b></color> seconds.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Increase <color=purple><b>Toxic Spore</b></color> duration by <color=green><b>{durpl:F1}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Attacks splash onto nearby insects within a <color=green><b>1.5</b></color> radius, applying <color=purple><b>Toxic Spores</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float critpl = PSData?.path2CritChancePerLevel        ?? 0.03f;
        float eapl   = PSData?.path2ElementalAffinityPerLevel ?? 0.04f;
        string desc = details
            ? $"The {GetName()} is able to deal <color=green><b>Critical Damage</b></color> with <color=#9400D3><b>Damage Over Time</b></color> effects.\n\n" +
              $"Increase <color=green><b>Critical Chance</b></color> by <color=green><b>[({(PSData?.baseCritChanceBonus ?? 0.1f) * 100f:F0}%) + ({critpl * 100f:F0}%/Lvl.)]</b></color>, and <color=#FFD700><b>Elemental Affinity</b></color> by <color=green><b>[({(PSData?.baseElementalAffinityBonus ?? 0.15f) * 100f:F0}%) + ({eapl * 100f:F0}%/Lvl.)]</b></color>."
            : $"The {GetName()} is able to deal <color=green><b>Critical Damage</b></color> with <color=#9400D3><b>Damage Over Time</b></color> effects.\n\n" +
              $"Increase <color=green><b>Critical Chance</b></color> by <color=green><b>{criticalChance * 100f:F0}%</b></color>, and <color=#FFD700><b>Elemental Affinity</b></color> by <color=green><b>{elementalAffinity * 100f:F0}%</b></color>.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Critical Chance</b></color> by <color=green><b>{critpl * 100f:F0}%</b></color> per level. [<color=green><b>+{critpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=#FFD700><b>Base Elemental Affinity</b></color> by <color=green><b>{eapl * 100f:F0}%</b></color> per level. [<color=green><b>+{eapl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Increase <color=green><b>Elemental Effect Chance</b></color> by <color=green><b>{(PSData?.path2MaxElementalEffectChanceBonus ?? 0.1f) * 100f:F0}%</b></color>, and <color=#9400D3><b>DoT Duration</b></color> by <color=green><b>{(PSData?.path2MaxDotDurationBonus ?? 0.5f) * 100f:F0}%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl    = PSData?.path3SkillDurationPerLevel ?? 1f;
        float radiuspl = PSData?.path3RadiusPerLevel        ?? 0.2f;
        string desc = details
            ? $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>[({data.baseSkillRadius:F1}) + ({radiuspl:F1}/Lvl.)]</b></color> radius that lasts <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. Insects inside take <color=green><b>{PoisonBaseDPS:F0}</b></color> <color=#FFB6C1>[+{skillDamageMultiplier * 100f:F0}% Magic Power]</color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second, and all debuffs on them are frozen in time."
            : $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>{activeRadius:F1}</b></color> radius that lasts <color=green><b>{skillDuration:F0}</b></color> seconds. Insects inside take <color=green><b>{PoisonBaseDPS:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second, and all debuffs on them are frozen in time.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase field duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase field radius by <color=green><b>{radiuspl:F1}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Each tick of damage from the field inflicts <color=purple><b>Toxic Spore</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
