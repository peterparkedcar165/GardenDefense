using UnityEngine;

public class LeafRanger : Shooter
{
    private bool skillActive;
    private float skillTimer;

    private LeafRangerData LRData => data as LeafRangerData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        if (skillActive)
            attackSpeed += baseAttackSpeed * ((LRData?.baseSkillAttackSpeedBonus ?? 3f) + 0.25f * effectivePath3Level + skillDamageMultiplier * magicPower);
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
        baseCriticalChance = data.baseCriticalChance + 0.05f * level;
        baseAttackSpeed    = data.baseAttackSpeed    + 0.05f * level;
    }

    public override void OnPath2Upgrade(int level) { }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + level * 0.5f;
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

    public override string GetPath1Description() =>
        $"Attack:\n\n" +
        $"Shoots slow but precise and fierce arrows at his target, dealing <color=green><b>{attackDamage}</b></color> <color=green><b>Nature</b></color> <color=#A0522D>Physical</color> damage.\n\n" +
        $"Increase Critical Chance by <color=green><b>5%</b></color> per level. [<color=green><b>+{5 * effectivePath1Level}%</b></color>]\n\n" +
        $"Increase Attack Speed by <color=green><b>0.05</b></color> per level. [<color=green><b>+{0.05 * effectivePath1Level}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description() =>
        $"Passive:\n\n" +
        $"Attacks can pierce <color=green><b>{piercing}</b></color> enemy.\n\n" +
        $"Increase Piercing by an additional <b><color=green>1</color></b> per level. [<b><color=green>+{effectivePath2Level}</color></b>]\n\n" +
        $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description() =>
        $"Skill:\n\n" +
        $"Enters a state of rapid focus, increasing his Attack Speed by <color=green><b>{(LRData?.baseSkillAttackSpeedBonus ?? 3f) * 100f + 25f * effectivePath3Level + skillDamageMultiplier * magicPower * 100f:F0}%</b></color> for <color=green><b>{skillDuration}</b></color> seconds.\n\n" +
        $"Scaling: <color=#FFB6C1><b>+{skillDamageMultiplier * 100f:F0}%</b></color> bonus per <color=#FFB6C1>Magic Power</color>. [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower * 100f:F0}%</b></color>]\n\n" +
        $"Increase Attack Speed bonus by <color=green><b>25%</b></color> per level. [<color=green><b>+{25 * effectivePath3Level}%</b></color>]\n\n" +
        $"Increase duration by <color=green><b>0.5</b></color> seconds per level. [<color=green><b>+{0.5 * effectivePath3Level}s</b></color>]\n\n" +
        $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
