using UnityEngine;
using System.Collections.Generic;

public class MorningGlory : Shooter
{
    [SerializeField] private GameObject updraftFieldPrefab;

    private float auraTickTimer = 0f;
    private const float auraTickInterval = 0.25f;
    private const float auraEffectDuration = 0.35f;

    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();
    private static readonly Color HighlightColor = new Color(0f, 0.8f, 0.8f);

    private MorningGloryData MGData => data as MorningGloryData;

    private float AttackSpeedBonusBase => (MGData?.baseAttackSpeedBonus ?? 0.15f) + (MGData?.path2AttackSpeedBonusPerLevel ?? 0.03f) * effectivePath2Level;
    private float AttackSpeedBonusMP   => (MGData?.attackSpeedBonusMPMultiplier ?? 0.16f) * magicPower / 100f;
    private float AttackSpeedBonus     => AttackSpeedBonusBase + AttackSpeedBonusMP;

    private float ProjectileSpeedBonusBase => (MGData?.baseProjectileSpeedBonus ?? 0.15f) + (MGData?.path2ProjectileSpeedBonusPerLevel ?? 0.03f) * effectivePath2Level;
    private float ProjectileSpeedBonusMP   => (MGData?.projectileSpeedBonusMPMultiplier ?? 0.12f) * magicPower / 100f;
    private float ProjectileSpeedBonus     => ProjectileSpeedBonusBase + ProjectileSpeedBonusMP;

    private float FieldDuration     => (MGData?.baseFieldDuration ?? 4f)   + (MGData?.path3FieldDurationPerLevel ?? 0.5f)  * effectivePath3Level;
    private float FieldRadius       => (MGData?.fieldRadius ?? 2f)         + (MGData?.path3RadiusPerLevel        ?? 0.15f) * effectivePath3Level;
    private float LiftForce         => MGData?.liftForce ?? 2.5f;
    private float LiftMaxHeight     => MGData?.liftMaxHeight ?? 1.5f;
    private float LevitateCritBonusBase => (MGData?.baseLevitateCritBonus ?? 0.25f) + (MGData?.path3CritBonusPerLevel ?? 0.05f) * effectivePath3Level;
    private float LevitateCritBonusMP   => (MGData?.critBonusMPMultiplier ?? 0.24f) * magicPower / 100f;
    private float LevitateCritBonus     => LevitateCritBonusBase + LevitateCritBonusMP;

    private float SpeedDamageScale => MGData?.projectileSpeedDamageScale ?? 2.5f;
    // attack scales with projectile speed (which her own passive also boosts)
    private float BladeDamage => attackDamage + SpeedDamageScale * projectileSpeed;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();
        UpdateAura();
        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        bool isSelected = PlantUpgradeUI.instance?.GetSelectedPlant() == this;
        var desired = new HashSet<Plant>();

        if (isSelected)
        {
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == null) continue;
                if (Vector2.Distance(transform.position, plant.transform.position) <= attackRange)
                    desired.Add(plant);
            }
        }

        foreach (Plant p in _highlightedPlants)
            if (p != null && !desired.Contains(p)) p.ClearHighlight();
        foreach (Plant p in desired)
            p.SetHighlight(HighlightColor);

        _highlightedPlants.Clear();
        foreach (Plant p in desired)
            _highlightedPlants.Add(p);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (Plant p in _highlightedPlants)
            if (p != null) p.ClearHighlight();
    }

    private void UpdateAura()
    {
        auraTickTimer += Time.deltaTime;
        if (auraTickTimer < auraTickInterval) return;
        auraTickTimer -= auraTickInterval;

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > attackRange) continue;
            TailwindEffect existing = plant.GetEffect<TailwindEffect>();
            if (existing != null && existing.attackSpeedBonus > AttackSpeedBonus) continue;
            plant.ApplyEffect(new TailwindEffect(plant, auraEffectDuration, 1, this, AttackSpeedBonus, ProjectileSpeedBonus));
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        MorningGloryProjectile blade = proj.GetComponent<MorningGloryProjectile>();
        if (blade != null)
        {
            blade.SetTarget(FindTarget());
            blade.speedDamageScale = SpeedDamageScale;
            // pass raw attack damage; the projectile computes attackDamage + scale * ITS OWN speed
            // on hit, so a mid-flight projectile-speed change is reflected in the damage
            blade.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginTargeting(FieldRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        skillCooldownTimer = skillCooldown;
        if (updraftFieldPrefab == null) return;
        GameObject obj = Instantiate(updraftFieldPrefab, position, Quaternion.identity);
        obj.GetComponent<UpdraftField>()?.Initialize(position, FieldRadius, FieldDuration, LiftForce, LiftMaxHeight, LevitateCritBonus, this);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (MGData?.path1AttackDamagePerLevel ?? 4f)  * level;
        baseAttackRange  = data.baseAttackRange  + (MGData?.path1AttackRangePerLevel  ?? 0.2f) * level;
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override string GetName() => "<b><color=#B2EBF2>Morning Glory</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a swift wind support, hastening allies and suspending enemies helplessly in the air.";

    public override string GetAttackDescription() =>
        $"Sends a wind blade at the first target, dealing <color=green><b>{BladeDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} damage (<color=green><b>{attackDamage:F0}</b></color> Attack Damage + <color=green><b>{SpeedDamageScale:F1}×</b></color> <color=green><b>{projectileSpeed:F1}</b></color> Projectile Speed).";

    public override string GetPassiveDescription() =>
        $"Plants within range — including herself — gain <color=#B2EBF2><b>Tailwind</b></color>: " +
        $"<color=green><b>+{AttackSpeedBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{AttackSpeedBonusMP * 100f:F0}%</b></color>] Attack Speed and " +
        $"<color=green><b>+{ProjectileSpeedBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{ProjectileSpeedBonusMP * 100f:F0}%</b></color>] Projectile Speed.";

    public override string GetSkillDesription() =>
        $"Summons an updraft field (radius <color=green><b>{FieldRadius:F1}</b></color>) for <color=green><b>{FieldDuration:F0}s</b></color>. " +
        $"Insects inside are kept airborne and <color=#B2EBF2><b>Levitating</b></color> — taking <color=#FFD700><b>+{LevitateCritBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{LevitateCritBonusMP * 100f:F0}%</b></color>] Critical Chance from all damage — until they land. Deals no damage.";

    public override string GetPath1Description()
    {
        float adpl    = MGData?.path1AttackDamagePerLevel ?? 4f;
        float rangepl = MGData?.path1AttackRangePerLevel  ?? 0.2f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase Attack Range by <color=green><b>{rangepl:F2}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        float aspl = MGData?.path2AttackSpeedBonusPerLevel     ?? 0.03f;
        float pspl = MGData?.path2ProjectileSpeedBonusPerLevel ?? 0.03f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(MGData?.attackSpeedBonusMPMultiplier ?? 0.16f) * 100f:F0}%</b></color> Magic Power (Attack Speed), <color=#FFB6C1><b>{(MGData?.projectileSpeedBonusMPMultiplier ?? 0.12f) * 100f:F0}%</b></color> Magic Power (Projectile Speed)\n\n" +
               $"Increase Attack Speed bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase Projectile Speed bonus by <color=green><b>{pspl * 100f:F0}%</b></color> per level. [<color=green><b>+{pspl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float durpl  = MGData?.path3FieldDurationPerLevel ?? 0.5f;
        float critpl = MGData?.path3CritBonusPerLevel     ?? 0.05f;
        float radpl  = MGData?.path3RadiusPerLevel        ?? 0.15f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(MGData?.critBonusMPMultiplier ?? 0.24f) * 100f:F0}%</b></color> Magic Power (Levitating Crit Chance)\n\n" +
               $"Increase field duration by <color=green><b>{durpl:F1}s</b></color> per level. [<color=green><b>+{durpl * effectivePath3Level:F1}s</b></color>]\n\n" +
               $"Increase Levitating Critical Chance by <color=green><b>{critpl * 100f:F0}%</b></color> per level. [<color=green><b>+{critpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase field radius by <color=green><b>{radpl:F2}</b></color> per level. [<color=green><b>+{radpl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
