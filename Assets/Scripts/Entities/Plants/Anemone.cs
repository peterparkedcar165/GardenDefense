using UnityEngine;

public class Anemone : Shooter
{
    private AnemoneData AData => data as AnemoneData;

    public float SplashRadius  { get; private set; }
    public float VortexRadius  { get; private set; }

    private int   InitialErosionStacks    => (AData?.baseInitialErosionStacks ?? 3) + Mathf.RoundToInt((AData?.path2InitialStacksPerLevel ?? 1) * effectivePath2Level);
    private float ErosionReductionPerStack => AData?.baseReductionPerStack ?? 0.5f;
    private float VortexDamageFlat      => AData?.vortexDamagePerTick ?? 10f;
    private float VortexDamageMPBonus   => skillDamageMultiplier * magicPower;
    private float VortexDamagePerTick   => VortexDamageFlat + VortexDamageMPBonus;
    private float VortexDetonationFlat  => AData?.vortexDetonationDamage ?? 150f;
    private float VortexDetonationDamage => VortexDetonationFlat + skillDamageMultiplier * magicPower;
    private float VortexDragSpeed       => (AData?.vortexDragSpeed ?? 0.8f) + (AData?.path3DragSpeedPerLevel ?? 0.1f) * effectivePath3Level;

    [SerializeField] private GameObject vortexPrefab;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        SplashRadius = AData?.splashRadius ?? 1.2f;
        VortexRadius = AData?.baseVortexRadius ?? 1.5f;
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject primaryTarget = FindTarget();
        if (primaryTarget == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        AnemoneProjectile projectile = proj.GetComponent<AnemoneProjectile>();
        if (projectile == null) return;
        projectile.SetTarget(primaryTarget);
        projectile.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
    }

    protected override void OnShoot()
    {
        if (IsPath1Maxed)
            skillCooldownTimer += 2f;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (AData?.path1AttackDamagePerLevel ?? 5f) * level;
        baseAttackRange  = data.baseAttackRange  + (AData?.path1AttackRangePerLevel  ?? 0.2f) * level;
    }

    public override void OnPath3Upgrade(int level)
    {
        VortexRadius = (AData?.baseVortexRadius ?? 1.5f) + (AData?.path3RadiusPerLevel ?? 0.2f) * level;
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginTargeting(VortexRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (vortexPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        GameObject obj = Instantiate(vortexPrefab, position, Quaternion.identity);
        AnemoneVortex vortex = obj.GetComponent<AnemoneVortex>();
        if (vortex == null) return;
        vortex.Initialize(
            VortexRadius,
            skillDuration,
            VortexDamagePerTick,
            AData?.vortexTickInterval    ?? 0.5f,
            VortexDragSpeed,
            VortexDetonationDamage,
            VortexRadius,
            AData?.vortexKnockbackForce  ?? 8f,
            IsPath3Maxed,
            IsPath3Maxed,
            this);
    }

    public void ApplyWindErosion(Insect insect)
    {
        if (insect == null || !insect.IsAlive) return;
        WindErosionEffect existing = insect.GetEffect<WindErosionEffect>();
        int newStacks;
        if (existing == null)
            newStacks = InitialErosionStacks;
        else if (existing.level < InitialErosionStacks)
            newStacks = InitialErosionStacks;
        else
            newStacks = existing.level + (IsPath2Maxed ? 2 : 1);
        insect.ApplyEffect(new WindErosionEffect(insect, passiveDuration, newStacks, this, IsPath2Maxed, ErosionReductionPerStack));
    }

    public override string GetName() => "<b><color=#D0E8FF>Anemone</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} commands the winds to erode and consume her foes, then pulls them into a devastating vortex.";

    public override string GetAttackDescription() =>
        $"Launches a wind ball at the target, dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, and <color=green><b>{attackDamage * 0.5f:F0}</b></color> to surrounding insects within a <color=green><b>{SplashRadius:F1}</b></color> radius.";

    public override string GetPassiveDescription() =>
        $"Dealing Wind Damage applies <color=green><b>{InitialErosionStacks}</b></color> stacks of <color=#E0E0E0><b>Wind Erosion</b></color> for <color=green><b>{passiveDuration:F0}s</b></color>, reducing <color=#00CED1><b>Armor</b></color> and <color=#9370DB><b>Magic Resist</b></color> by <color=red><b>{(int)ErosionReductionPerStack}</b></color> per stack. If the target is already afflicted with <color=#E0E0E0><b>Wind Erosion</b></color>, adds <color=green><b>1</b></color> stack instead.";

    public override string GetSkillDesription() =>
        $"Summon a vortex at target location, dragging insects toward its center while dealing <color=green><b>{VortexDamageFlat:F0}</b></color> [<color=#FFB6C1><b>+{VortexDamageMPBonus:F0}</b></color>] {PlantData.ElementalTag(elementalType)} damage every <color=green><b>{AData?.vortexTickInterval ?? 0.5f:F1}s</b></color> within a <color=green><b>{VortexRadius:F1}</b></color> radius, for <color=green><b>{skillDuration:F0}s</b></color>.";

    public override string GetPath1Name() => "Gale";
    public override string GetPath2Name() => "Erosion";
    public override string GetPath3Name() => "Vortex";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = AData?.path1AttackDamagePerLevel ?? 5f;
        float rpl  = AData?.path1AttackRangePerLevel  ?? 0.2f;
        string desc = details
            ? $"Launches a wind ball at the target, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, and half that to surrounding insects within a <color=green><b>{SplashRadius:F1}</b></color> radius."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rpl:F2}</b></color> per level. [<color=green><b>+{rpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Attacks reduce the <color=green><b>Vortex</b></color> cooldown by <color=green><b>2 seconds</b></color> per hit.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        int   spl        = AData?.path2InitialStacksPerLevel ?? 1;
        int   baseStacks = AData?.baseInitialErosionStacks   ?? 3;
        float baseRed    = AData?.baseReductionPerStack      ?? 0.5f;
        string desc = details
            ? $"Dealing Wind Damage applies <color=green><b>[({baseStacks}) + ({spl}/Lvl.)]</b></color> stacks of <color=#E0E0E0><b>Wind Erosion</b></color> for <color=green><b>{passiveDuration:F0}s</b></color>, reducing <color=#00CED1><b>Armor</b></color> and <color=#9370DB><b>Magic Resist</b></color> by <color=red><b>{(int)baseRed}</b></color> per stack. If the target is already afflicted with <color=#E0E0E0><b>Wind Erosion</b></color>, adds <color=green><b>1</b></color> stack instead."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase initial <color=#E0E0E0>Wind Erosion</color> application by <color=green><b>{spl}</b></color> per level. [<color=green><b>+{spl * effectivePath2Level}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Applies <color=green><b>2</b></color> stacks of <color=#E0E0E0>Wind Erosion</color> instead of <color=green><b>1</b></color> when refreshing.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float rpl          = AData?.path3RadiusPerLevel    ?? 0.2f;
        float dpl          = AData?.path3DragSpeedPerLevel ?? 0.1f;
        float tickInterval = AData?.vortexTickInterval    ?? 0.5f;
        float baseRadius   = AData?.baseVortexRadius      ?? 1.5f;
        float baseDrag     = AData?.vortexDragSpeed       ?? 0.8f;
        string desc = details
            ? $"Summon a vortex at target location, dragging insects toward its center (Pull Strength: <color=green><b>[({baseDrag:F2}) + ({dpl:F2}/Lvl.)]</b></color>) while dealing <color=green><b>[({VortexDamageFlat:F0}) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} damage every <color=green><b>{tickInterval:F1}s</b></color> within a <color=green><b>[({baseRadius:F1}) + ({rpl:F2}/Lvl.)]</b></color> radius, for <color=green><b>{skillDuration:F0}s</b></color>."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Vortex Radius</b></color> by <color=green><b>{rpl:F2}</b></color> per level. [<color=green><b>+{rpl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Pull Strength</b></color> by <color=green><b>{dpl:F2}</b></color> per level. [<color=green><b>+{dpl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"The Vortex becomes airborne.\n\nOn expiry, the vortex detonates, dealing <color=green><b>{VortexDetonationFlat:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} damage and pushing back all insects caught in it.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
