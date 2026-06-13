using UnityEngine;
using System.Collections.Generic;

public class LeafRanger : Shooter
{
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
        if (IsPath1Maxed)
            armorPenFlat += 30f;
    }

    protected override void Update()
    {
        base.Update();
        basePiercing = data.basePiercing + effectivePath2Level;
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

    protected override void OnShoot()
    {
        if (!IsPath2Maxed) return;
        VerdantFervorEffect existing = GetEffect<VerdantFervorEffect>();
        if (existing != null)
            existing.AddStack();
        else
            ApplyEffect(new VerdantFervorEffect(this, 8f, 1, this));
    }

    protected override void Shoot(Vector3 target)
    {
        if (HasEffect<RapidFocusEffect>() && IsPath3Maxed)
        {
            FireVolley(target);
            return;
        }
        FireArrow(FindTarget(), target);
    }

    private void FireVolley(Vector3 target)
    {
        Vector3 baseDir = (target - transform.position).normalized;
        int arrowCount = 5;
        int centerIndex = arrowCount / 2;
        float totalSpread = 30f;
        float spacing = totalSpread / (arrowCount - 1);
        float startAngle = -totalSpread / 2f;
        GameObject primaryTarget = FindTarget();

        for (int i = 0; i < arrowCount; i++)
        {
            float angle = startAngle + spacing * i;
            Vector3 rotatedDir = Quaternion.Euler(0f, 0f, angle) * baseDir;
            Vector3 arrowTarget = transform.position + rotatedDir * maxRange;
            FireArrow(i == centerIndex ? primaryTarget : null, arrowTarget);
        }
    }

    private void FireArrow(GameObject targetObj, Vector3 targetPos)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        LeafRangerProjectile arrow = projectile.GetComponent<LeafRangerProjectile>();
        if (arrow != null)
        {
            arrow.SetTarget(targetObj);
            arrow.Initialize(targetPos, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
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
        skillCooldownTimer = skillCooldown;
        float aspl = LRData?.path3AttackSpeedBonusPerLevel ?? 0.25f;
        float bonus = (LRData?.baseSkillAttackSpeedBonus ?? 3f) + aspl * effectivePath3Level + skillDamageMultiplier * magicPower;
        ApplyEffect(new RapidFocusEffect(this, skillDuration, 1, this, bonus));
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
               $"{Level5Section(path1Level, "Increase <color=green><b>Armor Penetration</b></color> by <color=green><b>30</b></color>.")}\n\n" +
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
               $"{Level5Section(path2Level, "Upon shooting, grant a level of <color=green><b>Verdant Fervor</b></color>, increasing <b>Total Attack Speed</b> by <color=green><b>4%</b></color> per level for <color=green><b>8</b></color> seconds.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float aspl  = LRData?.path3AttackSpeedBonusPerLevel ?? 0.25f;
        float durpl = LRData?.path3SkillDurationPerLevel    ?? 0.5f;
        float baseAS = LRData?.baseSkillAttackSpeedBonus    ?? 3f;
        string desc = details
            ? $"Enter a state of <color=green><b>Rapid Focus</b></color>, increasing <color=green>Attack Speed</color> by <color=green><b>[({baseAS * 100f:F0}%) + ({aspl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color>, for <color=green><b>[({data.baseSkillDuration:F1}) + ({durpl:F1}/Lvl.)]</b></color> seconds."
            : $"Enter a state of <color=green><b>Rapid Focus</b></color>, increasing <color=green>Attack Speed</color> by <color=green><b>{(baseAS + aspl * effectivePath3Level) * 100f + skillDamageMultiplier * magicPower * 100f:F0}%</b></color>, for <color=green><b>{skillDuration}</b></color> seconds.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase Attack Speed bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F1}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path3Level, "While the skill is active, shots instead fire a volley of <color=green><b>5</b></color> arrows in a <color=green><b>30°</b></color> cone.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
