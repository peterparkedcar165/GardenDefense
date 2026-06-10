using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NeriumOleander : Shooter
{
    private NeriumOleanderData OleanderData => data as NeriumOleanderData;

    [SerializeField] private float skillDelay = 0.5f;

    private int bounceCount;
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
        bounceCount         = (OleanderData?.baseBounceCount ?? 1) + (OleanderData?.path1BouncePerLevel ?? 1) * effectivePath1Level;
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
            petal.SetBounceData(bounceCount, toxinDuration, 1, OleanderData?.bounceSearchRadius ?? 6f, OleanderData?.bounceDamageReduction ?? 0.1f);
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

            insect.Damage(computedSkillDamage, damageType, elementalType, this, false,
                new DamageTag[] { DamageTag.AoE, DamageTag.SkillDamage });

            insect.ApplyEffect(new EntrappedEffect(insect, rootDuration, 1, this));

            OleandicToxinEffect existing = insect.GetEffect<OleandicToxinEffect>();
            if (existing != null)
                existing.RefreshAndStack(this);
            else
                insect.ApplyEffect(new OleandicToxinEffect(insect, toxinDuration, 1, this));
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (OleanderData?.path1AttackDamagePerLevel ?? 10f) * level;
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override string GetName() => $"<b><color=purple>{(data != null ? data.displayName : "Nerium Oleander")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} fires toxic petals that bounce between insects, applying <color=#9B59B6>Oleandic Toxin</color> which strips and immunizes them to their own buffs.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = OleanderData?.path1AttackDamagePerLevel ?? 10f;
        int   bpl  = OleanderData?.path1BouncePerLevel ?? 1;
        string scaling = details
            ? $"<color=green><b>Base Attack Damage</b></color> +{adpl:F0} per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
              $"<color=green><b>Bounce Count</b></color> +{bpl} per level. [<color=green><b>+{bpl * effectivePath1Level}</b></color>]"
            : $"<color=green><b>Base Attack Damage</b></color> +{adpl:F0}.\n\n" +
              $"<color=green><b>Bounce Count</b></color> +{bpl}.";
        return $"Attack:\n\n" +
               $"Fires a toxic petal at the target dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage. The petal bounces to <color=green><b>{bounceCount}</b></color> additional target(s). The petal deals <color=green><b>{(OleanderData?.bounceDamageReduction ?? 0.1f) * 100f:F0}%</b></color> reduced damage per bounce.\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float durpl = OleanderData?.path2ToxinDurationPerLevel ?? 2f;
        string scaling = details
            ? $"Increase duration by <color=green><b>{durpl:F0}s</b></color> per level. [<color=green><b>+{durpl * effectivePath2Level:F0}s</b></color>]"
            : $"Increase duration by <color=green><b>{durpl:F0}s</b></color>.";
        return $"Passive:\n\n" +
               $"Each petal hit applies <color=#9B59B6><b>Oleandic Toxin</b></color> for <color=green><b>{toxinDuration:F1}s</b></color>.\n\n" +
               $"<color=#9B59B6><b><u>Oleandic Toxin</u></b></color>\n" +
               $"Cleanses a random buff, and prevents them from receiving that buff while the effect is active.\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float dmgpl    = OleanderData?.path3SkillDamagePerLevel ?? 20f;
        float rootpl   = OleanderData?.path3RootDurationPerLevel ?? 0.5f;
        float radiuspl = OleanderData?.path3SkillRadiusPerLevel ?? 0.5f;
        string scaling = details
            ? $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
              $"Increase Skill Damage by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath3Level:F0}</b></color>]\n\n" +
              $"Increase Root Duration by <color=green><b>{rootpl:F1}s</b></color> per level. [<color=green><b>+{rootpl * effectivePath3Level:F1}s</b></color>]\n\n" +
              $"Increase Skill Radius by <color=green><b>{radiuspl:F1}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F1}</b></color>]"
            : $"Increase Skill Damage by <color=green><b>{dmgpl:F0}</b></color>.\n\n" +
              $"Increase Root Duration by <color=green><b>{rootpl:F1}s</b></color>.\n\n" +
              $"Increase Skill Radius by <color=green><b>{radiuspl:F1}</b></color>.";
        return $"Skill:\n\n" +
               $"Target an area. After <color=green><b>{skillDelay:F1}s</b></color>, all insects within <color=green><b>{skillRadius:F1}</b></color> radius are dealt <color=green><b>{computedSkillDamage:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, rooted for <color=green><b>{rootDuration:F1}s</b></color>, and afflicted with <color=#9B59B6>Oleandic Toxin</color>.\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
