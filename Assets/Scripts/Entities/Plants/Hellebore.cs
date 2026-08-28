using UnityEngine;
using System.Collections.Generic;

public class Hellebore : Shooter
{
    private HelleboreData HData => data as HelleboreData;

    private float _autoShieldCooldownTimer;
    private const float AutoShieldThreshold = 0.25f;

    private string AutoShieldCooldownText
    {
        get
        {
            if (_autoShieldCooldownTimer <= 0f) return "Ready";
            int m = (int)(_autoShieldCooldownTimer / 60f);
            int s = UnityEngine.Mathf.CeilToInt(_autoShieldCooldownTimer % 60f);
            return $"{m}:{s:D2}";
        }
    }
    private static readonly Color PurpleHighlight = new Color(0.6f, 0.2f, 0.8f);
    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();

    private bool autoCastEnabled = false;
    private Tile autoCastTargetTile = null;
    private Plant _autoCastHighlighted;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    private float CDRPerHit  => (HData?.passiveCDRPerHit ?? 0.5f) + effectivePath2Level * (HData?.path2CDRPerLevel ?? 0.1f);

    private float AuraShare => (HData?.auraShareBase ?? 0.5f) + effectivePath2Level * (HData?.path2AuraSharePerLevel ?? 0.05f);
    private float AuraArmor => baseArmor * AuraShare;

    private float SkillShieldBase => (HData?.shieldAmount ?? 120f) + effectivePath3Level * (HData?.path3ShieldPerLevel ?? 30f);
    private float SkillShieldMP   => (HData?.shieldMP ?? 0.5f) * magicPower;
    private float SkillShield     => SkillShieldBase + SkillShieldMP;
    private float SkillDur    => (HData?.shieldDuration    ?? 12f)  + effectivePath3Level * (HData?.path3DurationPerLevel ?? 2f);
    private float ReflectBase => (HData?.reflectPoisonBase ?? 15f)  + effectivePath3Level * (HData?.path3ReflectPerLevel  ?? 5f);
    private float ReflectMP   => HData?.reflectPoisonMP ?? 0.2f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        Plant.OnPlantPlaced += HandlePlantPlaced;
        ApplyAuraToAllInRange();
    }

    private void HandlePlantPlaced(Plant plant)
    {
        if (!IsAlive) return;
        ApplyAuraToAllInRange();
    }

    public override void UpdateStats()
    {
        baseArmor = (int)(
            (HData?.selfArmorBase     ?? 14)
            + effectivePath2Level * (HData?.selfArmorPerLevel ?? 5)
            + magicPower          * (HData?.selfArmorMP       ?? 0.14f));
        base.UpdateStats();
    }

    public override void OnPath2Upgrade(int level) => ApplyAuraToAllInRange();

    protected override void Update()
    {
        base.Update();
        UpdateHighlights();

        if (IsPath3Maxed && path3Unlocked && IsAlive)
        {
            if (_autoShieldCooldownTimer > 0f)
                _autoShieldCooldownTimer -= Time.deltaTime;
            else
                CheckAutoShield();
        }

        if (autoCastEnabled)
        {
            // resolved live from the tile (not a pinned instance), so if the target plant dies
            // and gets revived, the auto-cast picks the new instance back up on its own.
            // unlike the Path3 max-level auto-shield below, this can stack a new Thorned Guard
            // on top of one already there (its own instance just refreshes; a stack only
            // actually happens if a different Hellebore's shield is already present)
            Plant currentTarget = Plant.GetPlantOnTile(autoCastTargetTile);
            if (currentTarget != null && currentTarget.IsAlive && SkillReady)
                CastProtection(currentTarget);
        }

        UpdateAutoCastHighlight();
    }

    // while this Hellebore is selected and auto casting, highlight its locked target in yellow
    private void UpdateAutoCastHighlight()
    {
        Plant desired = (IsSelected && autoCastEnabled) ? Plant.GetPlantOnTile(autoCastTargetTile) : null;
        if (_autoCastHighlighted != null && _autoCastHighlighted != desired)
            _autoCastHighlighted.ClearHighlight();
        if (desired != null)
            desired.SetHighlight(Color.yellow);
        _autoCastHighlighted = desired;
    }

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled, targetTile = autoCastTargetTile };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled || state.targetTile == null) return;
        autoCastEnabled = true;
        autoCastTargetTile = state.targetTile;
    }

    // click Auto Cast to pick a target, click again to turn it off
    public override void ToggleAutoCast()
    {
        if (autoCastEnabled)
        {
            autoCastEnabled = false;
            autoCastTargetTile = null;
            return;
        }
        SkillTargetingManager.instance.BeginPlantTargeting(OnAutoCastTargetConfirmed, this);
    }

    private void OnAutoCastTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return;
        autoCastEnabled = true;
        autoCastTargetTile = targetPlant.occupiedTile;
    }

    private void CheckAutoShield()
    {
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (plant.maxHealth <= 0f || plant.health / plant.maxHealth >= AutoShieldThreshold) continue;
            if (plant.GetEffect<HelleboreProtectionEffect>() != null) continue;
            _autoShieldCooldownTimer = skillCooldown;
            plant.ApplyEffect(new HelleboreProtectionEffect(
                plant, SkillDur, effectivePath3Level + 1, this, SkillShield, ReflectBase, ReflectMP));
            return;
        }
    }

    // applied once on placement, on a Path2 upgrade, or whenever any new plant appears on the
    // field (via Plant.OnPlantPlaced) — not re-scanned every tick. removal is handled entirely
    // by HelleboreAuraEffect itself (PlantAuraBuffEffect base), which checks every frame whether
    // this Hellebore is still alive and in range, and expires itself if not
    private void ApplyAuraToAllInRange()
    {
        float armorBonus      = AuraArmor;
        float magicArmorBonus = IsPath2Maxed ? armorBonus * 0.5f : 0f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == this) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;
            plant.ApplyEffect(new HelleboreAuraEffect(plant, 1, this, attackRange, armorBonus, magicArmorBonus));
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        HelleboreProjectile proj = obj.GetComponent<HelleboreProjectile>();
        if (proj == null) return;
        proj.SetTarget(FindTarget());
        proj.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
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
            p.SetHighlight(PurpleHighlight);
        _highlightedPlants.Clear();
        foreach (Plant p in desired)
            _highlightedPlants.Add(p);
    }

    public void OnProjectileHit()
    {
        skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - CDRPerHit);
        if (IsPath3Maxed)
            _autoShieldCooldownTimer = Mathf.Max(0f, _autoShieldCooldownTimer - CDRPerHit);
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
    }

    private void OnTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return;
        CastProtection(targetPlant);
    }

    // shared by the manual skill cast and auto cast, does not reopen targeting on its own
    private void CastProtection(Plant targetPlant)
    {
        skillCooldownTimer = skillCooldown;
        targetPlant.ApplyEffect(new HelleboreProtectionEffect(
            targetPlant, SkillDur, effectivePath3Level + 1, this, SkillShield, ReflectBase, ReflectMP));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + (HData?.path1AttackSpeedPerLevel ?? 0.05f) * level;
        baseMagicPower  = data.baseMagicPower  + (HData?.path1MagicPowerPerLevel  ?? 5f)    * level;
        baseAttackRange = data.baseAttackRange + (HData?.path1AttackRangePerLevel ?? 0.2f)  * level;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Plant.OnPlantPlaced -= HandlePlantPlaced;
        foreach (Plant p in _highlightedPlants)
            if (p != null) p.ClearHighlight();
        _autoCastHighlighted?.ClearHighlight();
    }

    public override string GetName() => "<b><color=#9B30D0>Hellebore</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} weaves poison and shelter together, protecting allies while punishing those who dare attack them.";

    public override string GetAttackDescription() =>
        $"Fires a thorned projectile dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        $"Each attack hit reduces skill cooldown by <color=green><b>{CDRPerHit:F1}s</b></color>. " +
        $"Hellebore gains <color=#00CED1><b>{baseArmor}</b></color> Base Armor " +
        $"[<color=#FFB6C1><b>+{(HData?.selfArmorMP ?? 0.14f) * 100f:F0}% Magic Power</b></color>]. " +
        $"Plants within attack range (excluding herself) gain <color=#9B30D0><b>Hellebore's Protection</b></color>: " +
        $"increasing their Armor by <color=green><b>{AuraShare * 100f:F0}%</b></color> of Hellebore's Armor " +
        $"(<color=#00CED1><b>{(int)AuraArmor}</b></color>).";

    public override string GetSkillDesription() =>
        $"Targets a plant anywhere on the field, granting <color=#9B30D0><b>Thorned Guard</b></color>: " +
        $"a shield of <color=green><b>{SkillShieldBase:F0}</b></color> [<color=#FFB6C1><b>+{SkillShieldMP:F0}</b></color>] health for <color=green><b>{SkillDur:F0}s</b></color>. " +
        $"While shielded, attackers receive <color=purple><b>{ReflectBase:F0}</b></color> " +
        $"[<color=#FFB6C1><b>+{magicPower * ReflectMP:F0}</b></color>] " +
        $"<color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per hit. " +
        $"Negative effects are reflected back to the attacker. The protection fades when the shield breaks.";

    public override string GetPath1Name() => "Thorns";
    public override string GetPath2Name() => "Shelter";
    public override string GetPath3Name() => "Protection";

    public override string GetPath1Description(bool details = false)
    {
        float aspl = HData?.path1AttackSpeedPerLevel ?? 0.05f;
        float mppl = HData?.path1MagicPowerPerLevel  ?? 5f;
        float rgpl = HData?.path1AttackRangePerLevel ?? 0.2f;
        string desc = details
            ? $"Fires a thorned projectile dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=#FFB6C1><b>Base Magic Power</b></color> by <color=green><b>{mppl:F0}</b></color> per level. [<color=#FFB6C1><b>+{mppl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rgpl:F1}</b></color> per level. [<color=green><b>+{rgpl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Attacks deal additional damage equal to <color=green><b>28%</b></color> of Hellebore's <color=#00CED1><b>Armor</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float cdrpl    = HData?.path2CDRPerLevel       ?? 0.1f;
        int   armorpl  = HData?.selfArmorPerLevel      ?? 5;
        float auraShpl = HData?.path2AuraSharePerLevel ?? 0.05f;
        float armorMP  = HData?.selfArmorMP            ?? 0.14f;
        string desc = details
            ? $"Each attack hit reduces skill cooldown by <color=green><b>[({HData?.passiveCDRPerHit ?? 0.5f:F1}) + ({cdrpl:F1}/Lvl.)]</b></color> seconds. " +
              $"Hellebore gains <color=#00CED1><b>[({HData?.selfArmorBase ?? 14}) + ({armorpl}/Lvl.)]</b></color> Base Armor <color=#FFB6C1>[+{armorMP * 100f:F0}% Magic Power]</color>. " +
              $"Plants within attack range (excluding herself) gain <color=#9B30D0><b>Hellebore's Protection</b></color>: " +
              $"increasing their Armor by <color=green><b>[({HData?.auraShareBase ?? 0.5f:F0}%) + ({auraShpl * 100f:F0}%/Lvl.)]</b></color> of Hellebore's Armor."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase Cooldown Reduction per hit by <color=green><b>{cdrpl:F1}</b></color> seconds per level. [<color=green><b>+{cdrpl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"Increase <color=#00CED1><b>Base Armor</b></color> by <color=green><b>{armorpl}</b></color> per level. [<color=#00CED1><b>+{armorpl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase aura share by <color=green><b>{auraShpl * 100f:F0}%</b></color> per level. [<color=green><b>+{auraShpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Plants within aura range also gain <color=#FFB6C1><b>Magic Armor</b></color> equal to half of the provided <color=#00CED1><b>Armor</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float shieldpl = HData?.path3ShieldPerLevel   ?? 30f;
        float durpl    = HData?.path3DurationPerLevel ?? 2f;
        float reflpl   = HData?.path3ReflectPerLevel  ?? 5f;
        float shieldMP = HData?.shieldMP              ?? 0.5f;
        float reflMP   = HData?.reflectPoisonMP       ?? 0.2f;
        string desc = details
            ? $"Targets a plant anywhere on the field, granting <color=#9B30D0><b>Thorned Guard</b></color>: " +
              $"a shield of <color=green><b>[({HData?.shieldAmount ?? 120f:F0}) + ({shieldpl:F0}/Lvl.) + <color=#FFB6C1>{shieldMP * 100f:F0}% Magic Power</color>]</b></color> health " +
              $"for <color=green><b>[({HData?.shieldDuration ?? 12f:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. " +
              $"While shielded, attackers receive <color=purple><b>[({HData?.reflectPoisonBase ?? 15f:F0}) + ({reflpl:F0}/Lvl.)]</b></color> " +
              $"<color=#FFB6C1>[+{reflMP * 100f:F0}% Magic Power]</color> " +
              $"<color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per hit. " +
              $"Negative effects are reflected back to the attacker. The protection fades when the shield breaks."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase shield by <color=green><b>{shieldpl:F0}</b></color> per level. [<color=green><b>+{shieldpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase protection duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase reflect damage by <color=green><b>{reflpl:F0}</b></color> per level. [<color=green><b>+{reflpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Thorned Guard regenerates <color=green><b>6%</b></color> of its shield every second.\n\nWhenever a plant on the field drops below <color=green><b>25%</b></color> health, if they do not already have <color=#9B30D0><b>Thorned Guard</b></color>, Hellebore automatically applies it. The cooldown is not shared with the manual skill." + (IsPath3Maxed ? $"\n\nCooldown: [<color=green><b>{AutoShieldCooldownText}</b></color>]" : ""))}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
