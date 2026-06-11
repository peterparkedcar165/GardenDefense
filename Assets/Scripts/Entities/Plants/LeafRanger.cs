using UnityEngine;
using System.Collections.Generic;

public class LeafRanger : Shooter
{
    private bool skillActive;
    private float skillTimer;

    public override bool ShowRangeCircle => false;
    private LeafRangerData LRData => data as LeafRangerData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void UpdateStats()
    {
        // passive grants a flat base crit bonus at level 0 plus/Lvl. scaling.
        // set before base.UpdateStats so the derived criticalChance picks it up
        baseCriticalChance = data.baseCriticalChance
            + (LRData?.baseCritChanceBonus ?? 0.1f)
            + (LRData?.path2CritChancePerLevel ?? 0.05f) * effectivePath2Level;

        base.UpdateStats();
        float aspl = LRData?.path3AttackSpeedBonusPerLevel ?? 0.25f;
        if (skillActive)
            attackSpeed += baseAttackSpeed * ((LRData?.baseSkillAttackSpeedBonus ?? 3f) + aspl * effectivePath3Level + skillDamageMultiplier * magicPower);
    }

    protected override void Update()
    {
        base.Update();
        basePiercing = data.basePiercing + effectivePath2Level;

        if (skillActive)
        {
            skillTimer -= Time.deltaTime;
            if (skillTimer <= 0f)
                skillActive = false;
        }
    }

    protected override GameObject FindTarget()
    {
        if (DarknessManager.instance == null || !DarknessManager.instance.isDark)
            return base.FindTarget();

        List<Insect> illuminated = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (DarknessManager.instance.IsIlluminated(insect.transform.position))
                illuminated.Add(insect);
        }

        return targeting switch
        {
            TARGETING.First   => FindFirst(illuminated),
            TARGETING.Nearest => FindNearest(illuminated),
            TARGETING.Last    => FindLast(illuminated),
            _                 => null,
        };
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        LeafRangerProjectile arrow = projectile.GetComponent<LeafRangerProjectile>();
        if (arrow != null)
        {
            arrow.SetTarget(FindTarget());
            arrow.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage   = data.baseAttackDamage   + (LRData?.path1AttackDamagePerLevel  ?? 5f)    * level;
        baseAttackSpeed    = data.baseAttackSpeed    + (LRData?.path1AttackSpeedPerLevel   ?? 0.05f) * level;
    }

    public override void OnPath2Upgrade(int level) { }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (LRData?.path3SkillDurationPerLevel ?? 0.5f) * level;
    }

    public override void ActivateSkill()
    {
        skillActive = true;
        skillTimer = skillDuration;
        skillCooldownTimer = skillCooldown;
    }

    public override string GetDescription() =>
        $"The {GetName()} shoots slow but precise arrow shots from across the garden. His arrows can pierce through his target.";

    public override string GetPath1Name() => "Attack";
    public override string GetPath2Name() => "Passive";
    public override string GetPath3Name() => "Skill";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = LRData?.path1AttackDamagePerLevel ?? 5f;
        float aspl = LRData?.path1AttackSpeedPerLevel  ?? 0.05f;
        string desc = details
            ? $"Shoots slow but precise and fierce arrows at his target, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : $"Shoots slow but precise and fierce arrows at his target, dealing <color=green><b>{attackDamage}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(effectivePath1Level)}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float baseCrit = LRData?.baseCritChanceBonus    ?? 0.1f;
        float critpl   = LRData?.path2CritChancePerLevel ?? 0.05f;
        float totalCrit = baseCrit + critpl * effectivePath2Level;
        string desc = details
            ? $"Gains <color=green><b>[({baseCrit * 100f:F0}%) + ({critpl * 100f:F0}%/Lvl.)]</b></color> <color=green>Base Critical Chance</color>, and <color=green><b>[(0) + (1/Lvl.)]</b></color> <color=green>Piercing</color>."
            : $"Gains <color=green><b>{totalCrit * 100f:F0}%</b></color> <color=green>Base Critical Chance</color>, and <color=green><b>{piercing}</b></color> <color=green>Piercing</color>.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green>Piercing</color> by <color=green><b>1</b></color> per level. [<color=green><b>+{effectivePath2Level}</b></color>]\n\n" +
               $"Increase <color=green>Base Critical Chance</color> by <color=green><b>{critpl * 100f:F0}%</b></color> per level. [<color=green><b>+{critpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(effectivePath2Level)}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float aspl  = LRData?.path3AttackSpeedBonusPerLevel ?? 0.25f;
        float durpl = LRData?.path3SkillDurationPerLevel    ?? 0.5f;
        float baseAS = LRData?.baseSkillAttackSpeedBonus    ?? 3f;
        string desc = details
            ? $"Enters a state of rapid focus, increasing his Attack Speed by <color=green><b>[({baseAS * 100f:F0}%) + ({aspl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> for <color=green><b>[({data.baseSkillDuration:F1}) + ({durpl:F1}/Lvl.)]</b></color> seconds."
            : $"Enters a state of rapid focus, increasing his Attack Speed by <color=green><b>{(baseAS + aspl * effectivePath3Level) * 100f + skillDamageMultiplier * magicPower * 100f:F0}%</b></color> for <color=green><b>{skillDuration}</b></color> seconds.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase Attack Speed bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F1}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{Level5Section(effectivePath3Level)}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
