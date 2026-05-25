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
        float splashpl = WLData?.path2SplashDamageScalingPerLevel ?? 0.05f;
        float bubblepl = WLData?.path3BubbleDamagePerLevel        ?? 12f;
        splashDamage = data.basePassiveDamage + attackDamage * splashpl * effectivePath2Level + skillDamageMultiplier * magicPower;
        bubbleDamage = (WLData?.baseBubblePrisonImpactDamage ?? 0f) + bubblepl * effectivePath3Level + skillDamageMultiplier * magicPower;
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
        baseAttackRange = data.baseAttackRange + level * (WLData?.path1AttackRangePerLevel ?? 0.5f);
        baseAttackSpeed = data.baseAttackSpeed + level * (WLData?.path1AttackSpeedPerLevel ?? 0.3f);
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAoERange = (WLData?.baseAoERange ?? 0.75f) + level * (WLData?.path2AoERangePerLevel ?? 0.05f);
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
        $"The {GetName()} shoots her targets with little bubbles that hurts surrounding insects. She can also imprison her foes with her larger bubble.";

    public override string GetAttackDescription() =>
        $"Blow little bubbles towards her target, dealing <color=green><b>{attackDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic </color>damage.";

    public override string GetSkillDesription()
    {
        float bubblepl = WLData?.path3BubbleDamagePerLevel ?? 12f;
        return $"Blow a large bubble onto a targetted area, trapping insects within the bubble while dealing <color=green><b>{(WLData?.baseBubblePrisonImpactDamage ?? 0f) + bubblepl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] <color=#3399FF>Water</color> <color=#FFB6C1>Magic</color> damage upon impact, and keeping them airborne for <color=green><b>{skillDuration}</b></color> seconds within a <color=green><b>{skillAoERadius:F1}</b></color> radius.";
    }

    public override string GetPassiveDescription()
    {
        float splashpl = WLData?.path2SplashDamageScalingPerLevel ?? 0.05f;
        return $"Attacks deal <color=green><b>{data.basePassiveDamage + attackDamage * splashpl * effectivePath2Level:F1}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F1}</b></color>] <color=#3399FF>Water</color> damage to surrounding insects within a <color=green><b>{AoERange}</b></color> radius.";
    }

    public override string GetPath1Description()
    {
        float rangepl = WLData?.path1AttackRangePerLevel ?? 0.5f;
        float aspl    = WLData?.path1AttackSpeedPerLevel ?? 0.3f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Speed by <color=green><b>{aspl:F1}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Increase Attack Range by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        float splashpl = WLData?.path2SplashDamageScalingPerLevel ?? 0.05f;
        float aoepl   = WLData?.path2AoERangePerLevel             ?? 0.05f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
               $"Increase splash damage by <color=green><b>{splashpl * 100f:F0}%</b></color> of Attack Damage per level. [<color=green><b>+{attackDamage * splashpl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"Increase splash radius by <color=green><b>{aoepl:F2}</b></color> per level. [<color=green><b>+{aoepl * effectivePath2Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float bubblepl = WLData?.path3BubbleDamagePerLevel     ?? 12f;
        float durpl    = WLData?.path3SkillDurationPerLevel     ?? 2f;
        float radiuspl = WLData?.path3RadiusPerLevel            ?? 0.2f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
               $"Increase impact damage by <color=green><b>{bubblepl:F0}</b></color> per level. [<color=green><b>+{bubblepl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"Increase bubble radius by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
