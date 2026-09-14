using UnityEngine;
using System.Collections.Generic;

public class Begonia : Shooter
{
    private bool _isSkillTargeting = false;
    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();

    private bool autoCastEnabled = false;
    private Vector3 autoCastPosition;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    private BegoniaData BData => data as BegoniaData;

    private float ElementalAffinityBonusBase => (BData?.baseElementalAffinityBonus ?? 0.24f) + (BData?.path2ElementalAffinityPerLevel ?? 0.08f) * effectivePath2Level;
    private float ElementalAffinityBonusMP   => (BData?.basePassiveMultiplier ?? 0f) * magicPower / 100f;
    private float ElementalAffinityBonus     => ElementalAffinityBonusBase + ElementalAffinityBonusMP;

    private float GrassDamageBonusBase => (BData?.baseGrassDamageBonus ?? 0.2f) + (BData?.path3GrassDamagePerLevel ?? 0.04f) * effectivePath3Level;
    private float GrassDamageBonusMP   => (BData?.baseSkillMultiplier ?? 0f) * magicPower / 100f;
    private float GrassDamageBonus     => GrassDamageBonusBase + GrassDamageBonusMP;

    private float AttackSpeedBonusBase => (BData?.baseAttackSpeedBonus ?? 0f) + (BData?.path3AttackSpeedBonusPerLevel ?? 0.04f) * effectivePath3Level;
    private float AttackSpeedBonusMP   => (BData?.baseSkillMultiplier ?? 0f) * magicPower / 100f;
    private float AttackSpeedBonus     => AttackSpeedBonusBase + AttackSpeedBonusMP;
    private float BlossomRadius        => baseSkillRadius + (BData?.path3RadiusPerLevel ?? 0.15f) * effectivePath3Level;

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

    protected override void Update()
    {
        base.Update();
        UpdateHighlights();
        UpdateAutoCast();
    }

    private void UpdateAutoCast()
    {
        if (!autoCastEnabled) return;
        if (SkillReady) OnTargetConfirmed(autoCastPosition);
    }

    // applied once on placement, on a Path2 upgrade, or whenever any new plant appears on the
    // field (via Plant.OnPlantPlaced) — not re-scanned every tick. removal is handled entirely
    // by BegoniaBlessingEffect itself (PlantAuraBuffEffect base)
    private void ApplyAuraToAllInRange()
    {
        bool grantsGerminateCrit = path2Level >= Plant.absoluteLevelCap;
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > attackRange) continue;
            plant.ApplyEffect(new BegoniaBlessingEffect(plant, 1, this, attackRange, ElementalAffinityBonus, grantsGerminateCrit));
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        BegoniaProjectile petal = proj.GetComponent<BegoniaProjectile>();
        if (petal != null)
        {
            petal.SetTarget(FindTarget());
            petal.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        _isSkillTargeting = true;
        SkillTargetingManager.instance.BeginTargeting(BlossomRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        _isSkillTargeting = false;
        skillCooldownTimer = skillCooldown;
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(position, plant.transform.position) <= BlossomRadius)
                plant.ApplyEffect(new BlossomingEffect(plant, skillDuration, effectivePath3Level + 1, this, GrassDamageBonus, AttackSpeedBonus));
        }
    }

    // click Auto Cast to lock in an area, click again to turn it off
    public override void ToggleAutoCast()
    {
        if (autoCastEnabled)
        {
            autoCastEnabled = false;
            return;
        }
        _isSkillTargeting = true;
        SkillTargetingManager.instance.BeginTargeting(BlossomRadius, OnAutoCastTargetConfirmed);
    }

    private void OnAutoCastTargetConfirmed(Vector3 position)
    {
        _isSkillTargeting = false;
        autoCastEnabled = true;
        autoCastPosition = position;
    }

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled, targetPosition = autoCastPosition };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled) return;
        autoCastEnabled = true;
        autoCastPosition = state.targetPosition;
    }

    private void UpdateHighlights()
    {
        if (!SkillTargetingManager.instance.IsTargeting) _isSkillTargeting = false;

        bool isSelected = PlantUpgradeUI.instance?.GetSelectedPlant() == this;

        var desired = new HashSet<Plant>();
        Color highlightColor = Color.green;

        if (_isSkillTargeting)
        {
            Vector3 mousePos = SkillTargetingManager.instance.GetMouseWorldPosition();
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == null) continue;
                if (Vector2.Distance(mousePos, plant.transform.position) <= BlossomRadius)
                    desired.Add(plant);
            }
            highlightColor = Color.red;
        }
        else if (autoCastEnabled && isSelected)
        {
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == null) continue;
                if (Vector2.Distance(autoCastPosition, plant.transform.position) <= BlossomRadius)
                    desired.Add(plant);
            }
            highlightColor = Color.yellow;
        }
        else if (isSelected)
        {
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == null) continue;
                if (Vector2.Distance(transform.position, plant.transform.position) <= attackRange)
                    desired.Add(plant);
            }
            highlightColor = Color.green;
        }

        foreach (Plant p in _highlightedPlants)
            if (p != null && !desired.Contains(p)) p.ClearHighlight();

        foreach (Plant p in desired)
            p.SetHighlight(highlightColor);

        _highlightedPlants.Clear();
        foreach (Plant p in desired)
            _highlightedPlants.Add(p);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Plant.OnPlantPlaced -= HandlePlantPlaced;
        foreach (Plant p in _highlightedPlants)
            if (p != null) p.ClearHighlight();
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (BData?.path1AttackDamagePerLevel ?? 4f)  * level;
        baseAttackRange  = data.baseAttackRange  + (BData?.path1AttackRangePerLevel  ?? 0.2f) * level;
        baseAttackSpeed  = data.baseAttackSpeed  + (BData?.path1AttackSpeedPerLevel  ?? 0.02f) * level;
        ApplyAuraToAllInRange();
    }

    public override void OnPath2Upgrade(int level) => ApplyAuraToAllInRange();

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (BData?.path3SkillDurationPerLevel ?? 1f) * level;
    }

    public override string GetName() => "<b><color=green>Begonia</color></b>";
    public override string GetDescription() =>
        $"The {GetName()} sharpens nearby allies' precision and power, and can bless them with the strength of grass.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl    = BData?.path1AttackDamagePerLevel ?? 4f;
        float rangepl = BData?.path1AttackRangePerLevel  ?? 0.2f;
        float aspl    = BData?.path1AttackSpeedPerLevel  ?? 0.02f;
        string desc = details
            ? $"Fire a magical bolt dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F2}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Attacks bypass the <color=green><b>Grass</b></color> primer's internal cooldown.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float eapl = BData?.path2ElementalAffinityPerLevel ?? 0.08f;
        float mpMult = BData?.basePassiveMultiplier ?? 0f;
        string desc = details
            ? $"Plants within her attack radius are granted <color=green><b>Begonia's Blessing</b></color>, increasing <color=green><b>Elemental Affinity</b></color> by <color=green><b>[({(BData?.baseElementalAffinityBonus ?? 0.24f) * 100f:F0}%) + ({eapl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{mpMult * 100f:F0}% Magic Power</color>]</b></color>."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Elemental Affinity</b></color> bonus by <color=green><b>{eapl * 100f:F0}%</b></color> per level. [<color=green><b>+{eapl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, "Begonia's Blessing allows <color=green><b>Germinate</b></color> triggers to critically strike.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float adpl     = BData?.path3GrassDamagePerLevel ?? 0.04f;
        float aspl     = BData?.path3AttackSpeedBonusPerLevel  ?? 0.04f;
        float radiuspl = BData?.path3RadiusPerLevel            ?? 0.15f;
        float durpl    = BData?.path3SkillDurationPerLevel     ?? 1f;
        float mpMult   = BData?.baseSkillMultiplier ?? 0f;
        string desc = details
            ? $"Target an area on the field (radius <color=green><b>[({data.baseSkillRadius:F2}) + ({radiuspl:F2}/Lvl.)]</b></color>). Plants within are granted <color=green><b>Blossoming</b></color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds, " +
              $"increasing <color=green><b>Grass Damage</b></color> by <color=green><b>[({(BData?.baseGrassDamageBonus ?? 0.2f) * 100f:F0}%) + ({adpl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{mpMult * 100f:F0}% Magic Power</color>]</b></color> " +
              $"and <color=green><b>Attack Speed</b></color> by <color=green><b>[({(BData?.baseAttackSpeedBonus ?? 0f) * 100f:F0}%) + ({aspl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{mpMult * 100f:F0}% Magic Power</color>]</b></color>."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Grass Damage</b></color> bonus by <color=green><b>{adpl * 100f:F0}%</b></color> per level. [<color=green><b>+{adpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Attack Speed</b></color> bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase radius by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Blossoming plants' <color=green><b>Germinate</b></color> effect has its radius increased by <color=green><b>50%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetAttackDescription() =>
        $"Fire a magical bolt dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        $"Plants within her attack radius are granted <color=green><b>Begonia's Blessing</b></color>, " +
        $"increasing <color=green><b>Elemental Affinity</b></color> by <color=green><b>{ElementalAffinityBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{ElementalAffinityBonusMP * 100f:F0}%</b></color>].";

    public override string GetSkillDesription() =>
        $"Target an area on the field. Plants within are granted <color=green><b>Blossoming</b></color> for <color=green><b>{skillDuration:F0}s</b></color>, " +
        $"increasing <color=green><b>Grass Damage</b></color> by <color=green><b>{GrassDamageBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{GrassDamageBonusMP * 100f:F0}%</b></color>] " +
        $"and <color=green><b>Attack Speed</b></color> by <color=green><b>{AttackSpeedBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{AttackSpeedBonusMP * 100f:F0}%</b></color>].";
}
