using UnityEngine;
using UnityEngine.InputSystem;

public class Waterlily : Shooter
{
    public float AoERange, baseAoERange, AoERangeMultiplier, AoERangeAdder;
    public float skillAoERadius;
    public float bubbleDamage;
    public float splashDamage;
    public float slowProcChance;
    public float splashOnHitEffectiveness;
    [SerializeField] private GameObject bubbleTrapPrefab;

    private WaterlilyData WLData => data as WaterlilyData;

    public float SlowDuration => WLData?.baseSlowDuration ?? 6f;
    // base stack is 1, each passive level raises the cap
    public int MaxSlowStacks => 1 + (WLData?.path2MaxSlowStacksPerLevel ?? 1) * effectivePath2Level;

    // applies or refreshes the stacking slow, called on both the direct hit and splash damage.
    // if this waterlily's own cap can't push the stack any higher (e.g. a lower level waterlily
    // hitting a target already stacked up by a stronger one), the level never drops, only the
    // duration refreshes
    public void ApplyStackingSlow(Insect insect)
    {
        if (insect == null || !insect.IsAlive) return;
        float procChance = slowProcChance * (1f + bonusEffectChance);
        if (Random.value >= procChance) return;
        int currentStacks = insect.GetEffect<SlowEffect>()?.level ?? 0;
        int attemptedStacks = Mathf.Min(currentStacks + 1, MaxSlowStacks);
        int newStacks = Mathf.Max(currentStacks, attemptedStacks);
        insect.ApplyEffect(new SlowEffect(insect, SlowDuration, newStacks, this));
    }

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        baseAoERange   = WLData?.baseAoERange ?? 0.75f;
        skillAoERadius = data.baseSkillRadius;
        slowProcChance = WLData?.slowProcChance ?? 0.5f;
    }

    protected override void Update()
    {
        base.Update();
        AoERange = baseAoERange + AoERangeAdder + (baseAoERange * AoERangeMultiplier);
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        if (IsPath1Maxed)
            onHitEffectiveness *= 1f + (WLData?.path1MaxOnHitEffectivenessBonus ?? 0.5f);
        float splashpl = WLData?.path2SplashDamageScalingPerLevel ?? 0.05f;
        float bubblepl = WLData?.path3BubbleDamagePerLevel        ?? 12f;
        splashDamage = WLData.baseSplashDamage + attackDamage * splashpl * effectivePath2Level + skillDamageMultiplier * magicPower;
        bubbleDamage = (WLData?.baseBubblePrisonImpactDamage ?? 0f) + bubblepl * effectivePath3Level + skillDamageMultiplier * magicPower;
        // kept as a fraction of onHitEffectiveness, so it mirrors any bonus applied above instead of being a fixed value
        splashOnHitEffectiveness = (WLData?.splashOnHitEffectiveness ?? 0.33f) * onHitEffectiveness;
    }

    protected override void Shoot(Vector3 target)
    {
        FireBubble(FindTarget(), target);
    }

    private void FireBubble(GameObject targetObj, Vector3 targetPos)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        WaterlilyProjectile bubble = projectile.GetComponent<WaterlilyProjectile>();
        if (bubble != null)
        {
            bubble.SetTarget(targetObj);
            bubble.Initialize(targetPos, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * (WLData?.path1AttackDamagePerLevel ?? 5f);
        baseAttackRange = data.baseAttackRange + level * (WLData?.path1AttackRangePerLevel ?? 0.5f);
        baseAttackSpeed = data.baseAttackSpeed + level * (WLData?.path1AttackSpeedPerLevel ?? 0.3f);
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAoERange = (WLData?.baseAoERange ?? 0.75f) + level * (WLData?.path2AoERangePerLevel ?? 0.05f);
        slowProcChance = (WLData?.slowProcChance ?? 0.5f) + level * (WLData?.path2SlowProcChancePerLevel ?? 0.05f);
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
        $"Blow little bubbles towards her target, dealing <color=green><b>{attackDamage}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";

    public override string GetSkillDesription()
    {
        float bubblepl = WLData?.path3BubbleDamagePerLevel ?? 12f;
        return $"Blow a large bubble onto a targetted area, trapping insects within the bubble while dealing <color=green><b>{(WLData?.baseBubblePrisonImpactDamage ?? 0f) + bubblepl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage upon impact, and keeping them airborne for <color=green><b>{skillDuration}</b></color> seconds within a <color=green><b>{skillAoERadius:F1}</b></color> radius.";
    }

    public override string GetPassiveDescription()
    {
        float splashpl = WLData?.path2SplashDamageScalingPerLevel ?? 0.05f;
        return $"Attacks deal <color=green><b>{WLData.baseSplashDamage + attackDamage * splashpl * effectivePath2Level:F1}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F1}</b></color>] {PlantData.ElementalTag(elementalType)} damage to surrounding insects within a <color=green><b>{AoERange}</b></color> radius.\n\n" +
               $"Dealing damage with attacks and the splash damage has a <color=green><b>{slowProcChance * 100f:F0}%</b></color> chance to apply a stacking <color=#87CEEB><b>Slow</b></color> for <color=green><b>{SlowDuration:F1}s</b></color>, up to <color=#87CEEB><b>{MaxSlowStacks}</b></color> stacks.";
    }

    public override string GetPath1Description(bool details = false)
    {
        float dmgpl   = WLData?.path1AttackDamagePerLevel ?? 5f;
        float rangepl = WLData?.path1AttackRangePerLevel ?? 0.5f;
        float aspl    = WLData?.path1AttackSpeedPerLevel ?? 0.3f;
        string desc = details
            ? $"Blows little bubbles towards her target, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F1}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Increase <color=green><b>On-Hit</b></color> effect effectiveness by <color=green><b>{(WLData?.path1MaxOnHitEffectivenessBonus ?? 0.5f) * 100f:F0}%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float splashpl = WLData?.path2SplashDamageScalingPerLevel ?? 0.05f;
        float aoepl   = WLData?.path2AoERangePerLevel             ?? 0.05f;
        int stackspl  = WLData?.path2MaxSlowStacksPerLevel        ?? 1;
        float chancepl = WLData?.path2SlowProcChancePerLevel      ?? 0.05f;
        string desc = details
            ? $"Attacks deal <color=green><b>[({WLData.baseSplashDamage:F1}) + ({splashpl * 100f:F0}% <color=green><b>Attack Damage</b></color>/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} damage to surrounding insects within a <color=green><b>[({WLData?.baseAoERange ?? 0.75f:F2}) + ({aoepl:F2}/Lvl.)]</b></color> radius.\n\n" +
              $"Dealing damage with attacks and the splash damage has a <color=green><b>[({(WLData?.slowProcChance ?? 0.5f) * 100f:F0}%) + ({chancepl * 100f:F0}%/Lvl.)]</b></color> chance to apply a stacking <color=#87CEEB><b>Slow</b></color> for <color=green><b>{SlowDuration:F1}s</b></color>, up to <color=#87CEEB><b>{MaxSlowStacks}</b></color> stacks."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase splash damage by <color=green><b>{splashpl * 100f:F0}%</b></color> <color=green><b>Attack Damage</b></color> per level. [<color=green><b>+{attackDamage * splashpl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"Increase splash radius by <color=green><b>{aoepl:F2}</b></color> per level. [<color=green><b>+{aoepl * effectivePath2Level:F2}</b></color>]\n\n" +
               $"Increase max <color=#87CEEB><b>Slow</b></color> stacks by <color=green><b>{stackspl}</b></color> per level. [<color=green><b>+{stackspl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase <color=#87CEEB><b>Slow</b></color> chance by <color=green><b>{chancepl * 100f:F0}%</b></color> per level. [<color=green><b>+{chancepl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Splash damage now applies <color=green><b>On-Hit</b></color> effects at <color=green><b>{splashOnHitEffectiveness * 100f:F0}%</b></color> effectiveness. Splash damage is now also considered a <color=green><b>Projectile</b></color> attack.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float bubblepl = WLData?.path3BubbleDamagePerLevel  ?? 12f;
        float durpl    = WLData?.path3SkillDurationPerLevel ?? 2f;
        float radiuspl = WLData?.path3RadiusPerLevel        ?? 0.2f;
        string desc = details
            ? $"Blows a large bubble onto a targeted area, trapping insects within the bubble while dealing <color=green><b>[({WLData?.baseBubblePrisonImpactDamage ?? 0f:F0}) + ({bubblepl:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage upon impact, and keeping them airborne for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds within a <color=green><b>[({data.baseSkillRadius:F1}) + ({radiuspl:F2}/Lvl.)]</b></color> radius."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase impact damage by <color=green><b>{bubblepl:F0}</b></color> per level. [<color=green><b>+{bubblepl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase bubble radius by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Imprisoned insects slowly rise during the effect.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
