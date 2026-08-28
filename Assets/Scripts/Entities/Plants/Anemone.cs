using UnityEngine;
using System.Collections;

public class Anemone : Shooter
{
    private AnemoneData AData => data as AnemoneData;

    public float SplashRadius  { get; private set; }
    public float VortexRadius  { get; private set; }

    private int   InitialErosionStacks    => (AData?.baseInitialErosionStacks ?? 3) + Mathf.RoundToInt((AData?.path2InitialStacksPerLevel ?? 1) * effectivePath2Level);
    private float ErosionReductionPerStack => AData?.baseReductionPerStack ?? 0.5f;
    private float ErosionProcChance       => (AData?.baseErosionProcChance ?? 0.5f) + (AData?.path2ProcChancePerLevel ?? 0.05f) * effectivePath2Level;
    private float ErosionDurationPerLevel => AData?.erosionDurationPerLevel ?? 1f;
    private float ErosionDuration         => passiveDuration + ErosionDurationPerLevel * effectivePath2Level;
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
        StartCoroutine(TripleShot(target));
    }

    private IEnumerator TripleShot(Vector3 target)
    {
        for (int i = 0; i < 3; i++)
        {
            FireProjectile(target);
            if (i < 2) yield return new WaitForSeconds(0.1f);
        }
    }

    private void FireProjectile(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject primaryTarget = FindTarget();
        if (primaryTarget == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        AnemoneProjectile projectile = proj.GetComponent<AnemoneProjectile>();
        if (projectile == null) return;
        projectile.SetTarget(primaryTarget);
        projectile.Initialize(target, attackDamage / 3f, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
    }

    protected override void OnShoot()
    {
        if (IsPath1Maxed && Random.value < 0.35f)
            skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - 2f);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + (AData?.path1AttackSpeedPerLevel ?? 0.05f) * level;
        baseAttackRange = data.baseAttackRange + (AData?.path1AttackRangePerLevel ?? 0.2f) * level;
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
            VortexRadius * 2f,
            AData?.vortexKnockbackForce  ?? 8f,
            IsPath3Maxed,
            IsPath3Maxed,
            this);
    }

    public void ApplyWindErosion(Insect insect)
    {
        if (insect == null || !insect.IsAlive) return;
        if (Random.value >= ErosionProcChance) return;
        WindErosionEffect existing = insect.GetEffect<WindErosionEffect>();
        int newStacks;
        if (existing == null)
            newStacks = InitialErosionStacks;
        else if (existing.level < InitialErosionStacks)
            newStacks = InitialErosionStacks;
        else
            newStacks = existing.level + (IsPath2Maxed ? 2 : 1);
        insect.ApplyEffect(new WindErosionEffect(insect, ErosionDuration, newStacks, this, ErosionReductionPerStack));
    }

    public override string GetName() => "<b><color=#D0E8FF>Anemone</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} commands the winds to erode and consume her foes, then pulls them into a devastating vortex.";

    public override string GetAttackDescription() =>
        $"Launches <color=green><b>3</b></color> wind balls at the target, each dealing <color=green><b>{attackDamage / 3f:F0}</b></color> [<color=green><b>33%</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, and <color=green><b>{attackDamage / 3f * 0.5f:F0}</b></color> to surrounding insects within a <color=green><b>{SplashRadius:F1}</b></color> radius.";

    public override string GetPassiveDescription() =>
        $"Dealing Wind Damage has a <color=green><b>{ErosionProcChance * 100f:F0}%</b></color> chance to apply <color=green><b>{InitialErosionStacks}</b></color> stacks of <color=#E0E0E0><b>Wind Erosion</b></color> for <color=green><b>{ErosionDuration:F0}s</b></color>, reducing <color=#00CED1><b>Armor</b></color> and <color=#9370DB><b>Magic Resist</b></color> by <color=red><b>{(int)ErosionReductionPerStack}</b></color> per stack. If the target is already afflicted with <color=#E0E0E0><b>Wind Erosion</b></color>, adds <color=green><b>1</b></color> stack instead.";

    public override string GetSkillDesription() =>
        $"Summon a vortex at target location, dragging insects toward its center while dealing <color=green><b>{VortexDamageFlat:F0}</b></color> [<color=#FFB6C1><b>+{VortexDamageMPBonus:F0}</b></color>] {PlantData.ElementalTag(elementalType)} damage every <color=green><b>{AData?.vortexTickInterval ?? 0.5f:F1}s</b></color> within a <color=green><b>{VortexRadius:F1}</b></color> radius, for <color=green><b>{skillDuration:F0}s</b></color>.";

    public override string GetPath1Name() => "Gale";
    public override string GetPath2Name() => "Erosion";
    public override string GetPath3Name() => "Vortex";

    public override string GetPath1Description(bool details = false)
    {
        float aspl = AData?.path1AttackSpeedPerLevel ?? 0.05f;
        float rpl  = AData?.path1AttackRangePerLevel ?? 0.2f;
        string desc = details
            ? $"Launches <color=green><b>3</b></color> wind balls at the target, each dealing <color=green><b>[33% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, and half that to surrounding insects within a <color=green><b>{SplashRadius:F1}</b></color> radius."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rpl:F2}</b></color> per level. [<color=green><b>+{rpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Attacks have a <color=green><b>35%</b></color> chance to reduce <color=#B2EBF2><b>Wind Vortex</b></color>'s cooldown by <color=green><b>2 seconds</b></color> per hit.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        int   spl        = AData?.path2InitialStacksPerLevel ?? 1;
        int   baseStacks = AData?.baseInitialErosionStacks   ?? 3;
        float baseRed    = AData?.baseReductionPerStack      ?? 0.5f;
        float baseProc   = AData?.baseErosionProcChance      ?? 0.5f;
        float procpl     = AData?.path2ProcChancePerLevel    ?? 0.05f;
        string desc = details
            ? $"Dealing Wind Damage has a <color=green><b>[({baseProc * 100f:F0}%) + ({procpl * 100f:F0}%/Lvl.)]</b></color> chance to apply <color=green><b>[({baseStacks}) + ({spl}/Lvl.)]</b></color> stacks of <color=#E0E0E0><b>Wind Erosion</b></color> for <color=green><b>[({passiveDuration:F0}) + ({ErosionDurationPerLevel:F0}/Lvl.)]</b></color> seconds, reducing <color=#00CED1><b>Armor</b></color> and <color=#9370DB><b>Magic Resist</b></color> by <color=red><b>{(int)baseRed}</b></color> per stack. If the target is already afflicted with <color=#E0E0E0><b>Wind Erosion</b></color>, adds <color=green><b>1</b></color> stack instead."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase initial <color=#E0E0E0>Wind Erosion</color> application by <color=green><b>{spl}</b></color> per level. [<color=green><b>+{spl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase proc chance by <color=green><b>{procpl * 100f:F0}%</b></color> per level. [<color=green><b>+{procpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=#E0E0E0>Wind Erosion</color> duration by <color=green><b>{ErosionDurationPerLevel:F0}s</b></color> per level. [<color=green><b>+{ErosionDurationPerLevel * effectivePath2Level:F0}s</b></color>]\n\n" +
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
               $"{Level5Section(path3Level, $"The Vortex becomes airborne.\n\nOn expiry, the vortex detonates, dealing <color=green><b>{VortexDetonationFlat:F0}</b></color> [<color=#FFB6C1><b>{(details ? $"{skillDamageMultiplier * 100f:F0}% Magic Power" : $"+{skillDamageMultiplier * magicPower:F0}")}</b></color>] {PlantData.ElementalTag(elementalType)} damage and pushing back all insects caught in it.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
