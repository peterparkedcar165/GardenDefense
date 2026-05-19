using UnityEngine;
using UnityEngine.InputSystem;

public class Waterlily : Shooter
{
    public float AoERange, baseAoERange, AoERangeMultiplier, AoERangeAdder;
    public float skillAoERadius;
    public float bubbleDamage;
    public float splashDamage;
    [SerializeField] private GameObject bubbleTrapPrefab;

    private WaterlilyData WLData => data as WaterlilyData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        baseAoERange   = WLData?.baseAoERange ?? 0.75f;
        skillAoERadius = data.baseSkillRadius;
    }

    protected override void Update()
    {
        base.Update();
        AoERange = baseAoERange + AoERangeAdder + (baseAoERange * AoERangeMultiplier);
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        splashDamage = data.basePassiveDamage + attackDamage * 0.05f * effectivePath2Level + skillDamageMultiplier * magicPower;
        bubbleDamage = data.baseSkillDamage + 12f * effectivePath3Level + skillDamageMultiplier * magicPower;
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        WaterlilyProjectile bubble = projectile.GetComponent<WaterlilyProjectile>();
        if (bubble != null)
        {
            bubble.SetTarget(FindTarget());
            bubble.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackRange = data.baseAttackRange + level * 0.5f;
        baseAttackSpeed = data.baseAttackSpeed + level * 0.3f;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAoERange = (WLData?.baseAoERange ?? 0.75f) + level * 0.05f;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + 1f * level;
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
        $"The {GetName()} shoots her targets with little bubbles that hurts surrounding insects. She can also imprison her foes with her larger bubble.";

    public override string GetAttackDescription() =>
        $"Blow little bubbles towards her target, dealing <color=green><b>{attackDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic </color>damage.";

    public override string GetSkillDesription() =>
        $"Blow a large bubble onto a targetted area, trapping insects within the bubble while dealing <color=green><b>{data.baseSkillDamage + 12f * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] <color=#3399FF>Water</color> <color=#FFB6C1>Magic</color> damage upon impact, and keeping them airborne for <color=green><b>{skillDuration}</b></color> seconds.";

    public override string GetPassiveDescription() =>
        $"Attacks deal <color=green><b>{data.basePassiveDamage + attackDamage * 0.05f * effectivePath2Level:F1}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F1}</b></color>] <color=#3399FF>Water</color> damage to surrounding insects within a <color=green><b>{AoERange}</b></color> radius.";

    public override string GetPath1Description() =>
        $"Attack:\n\n{GetAttackDescription()}\n\n" +
        $"Increase Attack Speed by <color=green><b>0.3</b></color> per level. [<color=green><b>+{0.3 * effectivePath1Level}</b></color>]\n\n" +
        $"Increase Attack Range by <color=green><b>0.5</b></color> per level. [<color=green><b>+{0.5 * effectivePath1Level}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description() =>
        $"Passive:\n\n{GetPassiveDescription()}\n\n" +
        $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
        $"Increase splash damage by <color=green><b>5%</b></color> of Attack Damage per level. [<color=green><b>+{attackDamage * 0.05f * effectivePath2Level:F1}</b></color>]\n\n" +
        $"Increase splash radius by <color=green><b>0.05</b></color> per level. [<color=green><b>+{0.05 * effectivePath2Level}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description() =>
        $"Skill:\n\n{GetSkillDesription()}\n\n" +
        $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
        $"Increase impact damage by <color=green><b>12</b></color> per level. [<color=green><b>+{12 * effectivePath3Level}</b></color>]\n\n" +
        $"Increase duration by <color=green><b>1</b></color> second per level. [<color=green><b>+{1 * effectivePath3Level}s</b></color>]\n\n" +
        $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
