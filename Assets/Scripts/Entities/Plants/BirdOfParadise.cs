using UnityEngine;
using System.Collections.Generic;

// Thorn tank-buster: a melee single-target attacker that ramps up as it keeps hitting the same
// window (Talon Focus stacks decay after 2s without a hit). every landed hit (primary, the
// Path3-max permanent extra target, or either cleave target while Three Talon Strike is active)
// is routed through OnAttackHit via the
// centralized on-hit dispatcher (see Entity.HandleOnHitEffects), which applies the passive's
// per-stack bonus damage and (Path1 max, full stacks) the percent-missing-health finisher before
// finally growing this hit's own Talon Focus stack (see ApplyOnHitPackage for the exact order) -
// both bonus hits scale with on-hit effectiveness, same as Floral Glow/Ablaze. at Path2 max,
// every Nth hit against the same target reapplies the whole package again after a short delay
public class BirdOfParadise : Aura
{
    private BirdOfParadiseData BOPData => data as BirdOfParadiseData;

    public override bool UsesTargeting => true;

    public float TalonFocusASPerStack     => (BOPData?.baseTalonFocusASPerStack ?? 0.04f) + (BOPData?.path1TalonFocusASPerStackPerLevel ?? 0.02f) * effectivePath1Level;
    public float ArmorShredPerStack       => BOPData?.maxLevelArmorShredPerStack ?? 0.02f;
    public float MaxStacksAttackSpeedBonus => BOPData?.maxStacksAttackSpeedBonus ?? 0.15f;
    // flat cap at every level - reachable through sustained hits regardless of Path1 level, but
    // the max-stacks bonuses (missing-health finisher, +Total AS) still require Path1 max
    public int   TalonFocusCap            => BOPData?.baseTalonFocusStackCap ?? 10;

    private float PassiveDamagePerStack => (BOPData?.basePassiveDamagePerStack ?? 3f) + (BOPData?.path2PassiveDamagePerStackPerLevel ?? 1f) * effectivePath2Level;
    private float SkillAttackSpeedBonus => (BOPData?.baseSkillAttackSpeedBonus ?? 0.08f) + (BOPData?.path3AttackSpeedBonusPerLevel ?? 0.04f) * effectivePath3Level;

    private int   ExtraProcEveryNHits => BOPData?.maxLevelExtraProcEveryNHits ?? 3;
    private float ExtraProcDelay      => BOPData?.maxLevelExtraProcDelay ?? 0.15f;

    // Path3 max: the permanent extra target must be within this radius of the main target
    // specifically (not just within the Bird's own attack range)
    private const float PermanentExtraTargetRadius = 1f;

    private static readonly DamageTag[] attackTags        = { DamageTag.Attack, DamageTag.Melee, DamageTag.SingleTarget };
    private static readonly DamageTag[] cleaveTags        = { DamageTag.Attack, DamageTag.Melee, DamageTag.AoE };
    // the passive's flat per-stack bonus keeps PassiveDamage (it's a passive-sourced bonus, so it
    // should still scale with a passiveDamage stat); the missing-health finisher is Attack-tree
    // sourced (Path1), so it stays OnHit-only
    private static readonly DamageTag[] passiveBonusTags  = { DamageTag.OnHit, DamageTag.PassiveDamage };
    private static readonly DamageTag[] onHitBonusTags    = { DamageTag.OnHit };

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();
    }

    protected override void Attack()
    {
        base.Attack();

        GameObject primary = FindTarget();
        Insect primaryInsect = primary != null ? primary.GetComponent<Insect>() : null;
        if (primaryInsect == null || !primaryInsect.IsAlive) return;

        primaryInsect.Damage(attackDamage, damageType, elementalType, this, true, attackTags);

        HashSet<Insect> hit = new HashSet<Insect> { primaryInsect };

        // Path3 max: one permanent extra target, but only within a small radius of the main
        // target specifically - not just anything in the Bird's own attack range
        if (IsPath3Maxed)
        {
            Insect nearPrimary = FindNearestWithinRadius(primaryInsect.transform.position, PermanentExtraTargetRadius, hit);
            if (nearPrimary != null)
            {
                nearPrimary.Damage(attackDamage, damageType, elementalType, this, true, cleaveTags);
                hit.Add(nearPrimary);
            }
        }

        // Three Talon Strike: two additional nearest targets to the Bird itself, within its own
        // attack range
        if (HasEffect<ThreeTalonStrikeEffect>())
        {
            foreach (Insect insect in FindNearestExtra(hit, 2))
            {
                insect.Damage(attackDamage, damageType, elementalType, this, true, cleaveTags);
                hit.Add(insect);
            }
        }
    }

    private Insect FindNearestWithinRadius(Vector3 origin, float radius, HashSet<Insect> exclude)
    {
        Insect nearest = null;
        float nearestDist = float.MaxValue;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || exclude.Contains(insect)) continue;
            float dist = Vector3.Distance(origin, insect.transform.position);
            if (dist > radius || dist >= nearestDist) continue;
            nearestDist = dist;
            nearest = insect;
        }
        return nearest;
    }

    private List<Insect> FindNearestExtra(HashSet<Insect> exclude, int count)
    {
        List<Insect> candidates = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || exclude.Contains(insect) || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) > attackRange) continue;
            candidates.Add(insect);
        }
        candidates.Sort((a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
                .CompareTo(Vector3.Distance(transform.position, b.transform.position)));
        return candidates.Count <= count ? candidates : candidates.GetRange(0, count);
    }

    private GameObject FindTarget()
    {
        switch (targeting)
        {
            case TARGETING.Nearest:   return FindNearest(Insect.allInsects);
            case TARGETING.First:     return FindFirst(Insect.allInsects);
            case TARGETING.Last:      return FindLast(Insect.allInsects);
            case TARGETING.Strongest: return FindStrongest(Insect.allInsects);
            default:                  return null;
        }
    }

    // called by Entity.HandleOnHitEffects for every landed hit (primary or cleave). applies the
    // on-hit package (see ApplyOnHitPackage), then tracks hits against this specific target for
    // Path2 max's every-Nth-hit extra proc
    public void OnAttackHit(Insect insect, float effectiveness)
    {
        ApplyOnHitPackage(insect, effectiveness);

        TalonHitCounterEffect counter = insect.GetEffect<TalonHitCounterEffect>(this);
        if (counter == null)
        {
            counter = new TalonHitCounterEffect(insect, this);
            insect.ApplyEffect(counter);
        }
        counter.RegisterHit();

        if (IsPath2Maxed && ExtraProcEveryNHits > 0 && counter.hitCount % ExtraProcEveryNHits == 0)
            StartCoroutine(DelayedExtraProc(insect, effectiveness));
    }

    private System.Collections.IEnumerator DelayedExtraProc(Insect insect, float effectiveness)
    {
        yield return new WaitForSeconds(ExtraProcDelay);
        if (insect == null || !insect.IsAlive) yield break;
        ApplyOnHitPackage(insect, effectiveness);
    }

    // order matters here, per design: the passive's per-stack bonus damage lands first, then the
    // missing-health finisher (Path1 max), and only then does this hit's own Talon Focus stack
    // get added - so a stack gained on THIS hit doesn't buff THIS hit's own bonus damage, only
    // the next one (first hit does nothing, second hit benefits from 1 stack, etc.)
    private void ApplyOnHitPackage(Insect insect, float effectiveness)
    {
        TalonFocusEffect focus = GetEffect<TalonFocusEffect>();
        int preHitStacks = focus?.stacks ?? 0;

        float bonusDamage = PassiveDamagePerStack * preHitStacks;
        if (bonusDamage > 0f)
            insect.Damage(bonusDamage, damageType, elementalType, this, false, passiveBonusTags, false, effectiveness);

        if (IsPath1Maxed && preHitStacks >= TalonFocusCap)
        {
            float missingHealthDamage = insect.MissingHealth * (BOPData?.maxStacksHealthPercentDamage ?? 0.05f);
            if (missingHealthDamage > 0f)
                insect.Damage(missingHealthDamage, damageType, elementalType, this, false, onHitBonusTags, false, effectiveness);
        }

        if (focus == null)
            ApplyEffect(new TalonFocusEffect(this, this));
        else
            focus.AddStack();
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        ApplyEffect(new ThreeTalonStrikeEffect(this, skillDuration, 1, this, SkillAttackSpeedBonus));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (BOPData?.path1AttackDamagePerLevel ?? 4f) * level;
        baseAttackRange  = data.baseAttackRange  + (BOPData?.path1AttackRangePerLevel ?? 0.2f) * level;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (BOPData?.path3SkillDurationPerLevel ?? 2f) * level;
    }

    public override string GetName() => "<b><color=#FF8C00>Bird of Paradise</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} focuses its talons on a single foe, striking faster and harder with every hit until even the mightiest insects begin to crumble.";

    public override string GetAttackDescription() =>
        $"Attacks a single target dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}, gaining a stack of <color=#B2EBF2><b>Talon Focus</b></color>, increasing <color=green><b>Attack Speed</b></color> by <color=green><b>{TalonFocusASPerStack * 100f:F0}%</b></color> per stack, up to <color=green><b>{TalonFocusCap}</b></color> stacks.";

    public override string GetPassiveDescription() =>
        $"Each stack of <color=#B2EBF2><b>Talon Focus</b></color> deals an additional <color=green><b>{PassiveDamagePerStack:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} on hit.";

    public override string GetSkillDesription() =>
        $"Grants self <color=#B2EBF2><b>Three Talon Strike</b></color> for <color=green><b>{skillDuration:F0}s</b></color>, increasing Total Attack Speed by <color=green><b>{SkillAttackSpeedBonus * 100f:F0}%</b></color> and allowing attacks to hit <color=green><b>2</b></color> additional nearest enemies for the exact same effect.";

    public override string GetPath1Name() => "Talons";
    public override string GetPath2Name() => "Ferocity";
    public override string GetPath3Name() => "Frenzy";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = BOPData?.path1AttackDamagePerLevel ?? 4f;
        float rpl  = BOPData?.path1AttackRangePerLevel ?? 0.2f;
        float aspl = BOPData?.path1TalonFocusASPerStackPerLevel ?? 0.02f;
        string desc = details
            ? $"Attacks a single target dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}, gaining a stack of <color=#B2EBF2><b>Talon Focus</b></color>, increasing <color=green><b>Attack Speed</b></color> by <color=green><b>[({(BOPData?.baseTalonFocusASPerStack ?? 0.04f) * 100f:F0}%) + ({aspl * 100f:F0}%/Lvl.)]</b></color> per stack, up to <color=green><b>{TalonFocusCap}</b></color> stacks."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rpl:F2}</b></color> per level. [<color=green><b>+{rpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Talon Focus</b></color> Attack Speed bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"At <color=green><b>{TalonFocusCap}</b></color> stacks of <color=#B2EBF2><b>Talon Focus</b></color>, attacks also deal <color=green><b>{(BOPData?.maxStacksHealthPercentDamage ?? 0.05f) * 100f:F0}%</b></color> of the target's missing health on hit, and grant <color=green><b>{MaxStacksAttackSpeedBonus * 100f:F0}%</b></color> Total Attack Speed while at full stacks.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float dmgpl = BOPData?.path2PassiveDamagePerStackPerLevel ?? 1f;
        string desc = details
            ? $"Each stack of <color=#B2EBF2><b>Talon Focus</b></color> deals an additional <color=green><b>[({BOPData?.basePassiveDamagePerStack ?? 3f:F0}) + ({dmgpl:F0}/Lvl.)]</b></color> {PlantData.DamageTypeLabel(damageType)} on hit."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase bonus damage per stack by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"<color=#B2EBF2><b>Talon Focus</b></color> also grants <color=green><b>{ArmorShredPerStack * 100f:F0}%</b></color> Armor Shred per stack. Every <color=green><b>{ExtraProcEveryNHits}</b></color> attack hits against the same target, apply the on-hit effect an additional time.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float aspl  = BOPData?.path3AttackSpeedBonusPerLevel ?? 0.04f;
        float durpl = BOPData?.path3SkillDurationPerLevel ?? 2f;
        string desc = details
            ? $"Grants self <color=#B2EBF2><b>Three Talon Strike</b></color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds, increasing Total Attack Speed by <color=green><b>[({(BOPData?.baseSkillAttackSpeedBonus ?? 0.08f) * 100f:F0}%) + ({aspl * 100f:F0}%/Lvl.)]</b></color> and allowing attacks to hit <color=green><b>2</b></color> additional nearest enemies for the exact same effect."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase Total Attack Speed bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Permanently attacks an additional nearest enemy within a <color=green><b>{PermanentExtraTargetRadius:F0}</b></color> radius of the main target, even outside Three Talon Strike.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
