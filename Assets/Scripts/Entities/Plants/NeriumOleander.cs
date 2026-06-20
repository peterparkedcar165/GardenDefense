using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NeriumOleander : Shooter
{
    private NeriumOleanderData OleanderData => data as NeriumOleanderData;

    [SerializeField] private float skillDelay = 0.5f;

    private float toxinDuration;
    private float rootDuration;
    private float computedSkillDamage;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        piercing           += (OleanderData?.path1BouncePerLevel ?? 1) * effectivePath1Level;
        float durpl         = OleanderData?.path2ToxinDurationPerLevel ?? 2f;
        toxinDuration       = ((OleanderData?.baseToxinDuration ?? 6f) + durpl * effectivePath2Level) * (1 + passiveDuration);
        rootDuration        = (data.baseSkillDuration + (OleanderData?.path3RootDurationPerLevel ?? 0.5f) * effectivePath3Level) * (1 + passiveDuration);
        float dmgpl         = OleanderData?.path3SkillDamagePerLevel ?? 20f;
        computedSkillDamage = (OleanderData?.baseSkillFlatDamage ?? 50f) + dmgpl * effectivePath3Level + skillDamageMultiplier * magicPower;
        skillRadius         = data.baseSkillRadius + (OleanderData?.path3SkillRadiusPerLevel ?? 0.5f) * effectivePath3Level;
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        NeriumOleanderProjectile petal = proj.GetComponent<NeriumOleanderProjectile>();
        if (petal != null)
        {
            petal.SetTarget(FindTarget());
            petal.Initialize(target, attackDamage, projectileSpeed, maxRange, 0, damageType, elementalType, this);
            petal.SetBounceData(1 + piercing, toxinDuration, 1, OleanderData?.bounceSearchRadius ?? 6f, OleanderData?.bounceDamageReduction ?? 0.1f);
        }
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(skillRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        skillCooldownTimer = skillCooldown;
        StartCoroutine(ExecuteSkill(position));
    }

    private IEnumerator ExecuteSkill(Vector3 position)
    {
        yield return new WaitForSeconds(skillDelay);

        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(position, insect.transform.position) > skillRadius) continue;

            OleandicToxinEffect existing = insect.GetEffect<OleandicToxinEffect>();
            if (existing != null)
                existing.RefreshAndStack(this);
            else
                insect.ApplyEffect(new OleandicToxinEffect(insect, toxinDuration, 1, this));

            int lockedCount = IsPath3Maxed ? (insect.GetEffect<OleandicToxinEffect>()?.immuneTypes.Count ?? 0) : 0;
            float skillDmg = computedSkillDamage * (1f + 0.12f * lockedCount);
            insect.Damage(skillDmg, damageType, elementalType, this, false,
                new DamageTag[] { DamageTag.AoE, DamageTag.SkillDamage });

            insect.ApplyEffect(new EntrappedEffect(insect, rootDuration, 1, this));
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (OleanderData?.path1AttackDamagePerLevel ?? 10f) * level;
        baseAttackRange  = data.baseAttackRange  + (OleanderData?.path1AttackRangePerLevel  ?? 0.5f) * level;
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override string GetName() => $"<b><color=purple>{(data != null ? data.displayName : "Nerium Oleander")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} fires toxic petals that bounce between insects, applying <color=#9B59B6>Oleandic Toxin</color> which strips and immunizes them to their own buffs.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = OleanderData?.path1AttackDamagePerLevel  ?? 10f;
        float arpl = OleanderData?.path1AttackRangePerLevel   ?? 0.5f;
        int   bpl  = OleanderData?.path1BouncePerLevel        ?? 1;
        string desc = details
            ? $"Fires a toxic petal at the target dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage. The petal bounces to <color=green><b>[(1) + Piercing]</b></color> additional target(s). The petal deals <color=green><b>{(OleanderData?.bounceDamageReduction ?? 0.1f) * 100f:F0}%</b></color> reduced damage per bounce."
            : $"Fires a toxic petal at the target dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage. The petal bounces to <color=green><b>{1 + piercing}</b></color> additional target(s). The petal deals <color=green><b>{(OleanderData?.bounceDamageReduction ?? 0.1f) * 100f:F0}%</b></color> reduced damage per bounce.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{arpl:F1}</b></color> per level. [<color=green><b>+{arpl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Increase <color=green><b>Piercing</b></color> by <color=green><b>{bpl}</b></color> per level. [<color=green><b>+{bpl * effectivePath1Level}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Deal <color=green><b>22% increased damage</b></color> for each positive effect on the target.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float durpl = OleanderData?.path2ToxinDurationPerLevel ?? 2f;
        string desc = details
            ? $"Each petal hit applies <color=#9B59B6><b>Oleandic Toxin</b></color> for <color=green><b>[({OleanderData?.baseToxinDuration ?? 6f:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds.\n\n" +
              $"<color=#9B59B6><b><u>Oleandic Toxin</u></b></color>\n" +
              $"Cleanses a random buff, and prevents them from receiving that buff while the effect is active."
            : $"Each petal hit applies <color=#9B59B6><b>Oleandic Toxin</b></color> for <color=green><b>{toxinDuration:F1}</b></color> seconds.\n\n" +
              $"<color=#9B59B6><b><u>Oleandic Toxin</u></b></color>\n" +
              $"Cleanses a random buff, and prevents them from receiving that buff while the effect is active.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"For each positive effect locked, the target loses <color=#FF69B4><b>8 Magic Armor</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float dmgpl    = OleanderData?.path3SkillDamagePerLevel  ?? 20f;
        float rootpl   = OleanderData?.path3RootDurationPerLevel ?? 0.5f;
        float radiuspl = OleanderData?.path3SkillRadiusPerLevel  ?? 0.5f;
        string desc = details
            ? $"Target an area. After <color=green><b>{skillDelay:F1}</b></color> seconds, all insects within <color=green><b>[({data.baseSkillRadius:F1}) + ({radiuspl:F1}/Lvl.)]</b></color> radius are dealt <color=green><b>[({OleanderData?.baseSkillFlatDamage ?? 50f:F0}) + ({dmgpl:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, rooted for <color=green><b>[({data.baseSkillDuration:F1}) + ({rootpl:F1}/Lvl.)]</b></color> seconds, and afflicted with <color=#9B59B6>Oleandic Toxin</color>."
            : $"Target an area. After <color=green><b>{skillDelay:F1}</b></color> seconds, all insects within <color=green><b>{skillRadius:F1}</b></color> radius are dealt <color=green><b>{computedSkillDamage:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, rooted for <color=green><b>{rootDuration:F1}</b></color> seconds, and afflicted with <color=#9B59B6>Oleandic Toxin</color>.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase Skill Damage by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase Root Duration by <color=green><b>{rootpl:F1}</b></color> seconds per level. [<color=green><b>+{rootpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase Skill Radius by <color=green><b>{radiuspl:F1}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Applies <color=#9B59B6><b>Oleandic Toxin</b></color>. For each positive effect locked, increase skill damage by <color=green><b>12%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
