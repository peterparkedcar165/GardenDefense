using UnityEngine;
using System.Collections.Generic;

public class Begonia : Shooter
{
    private float auraTickTimer = 0f;
    private const float auraTickInterval = 0.25f;

    private bool _isSkillTargeting = false;
    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();

    private bool autoCastEnabled = false;
    private Vector3 autoCastPosition;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    private BegoniaData BData => data as BegoniaData;

    private float elementalAffinityBonusBase => (BData?.baseelementalAffinityBonus ?? 0f) + (BData?.path2elementalAffinityPerLevel ?? 0.06f) * effectivePath2Level;
    private float elementalAffinityBonusMP   => (BData?.basePassiveMultiplier ?? 0f) * magicPower / 100f;
    private float elementalAffinityBonus     => elementalAffinityBonusBase + elementalAffinityBonusMP;

    private float ElementalEffectChanceBonusBase => (BData?.baseElementalEffectChanceBonus ?? 0.02f) + (BData?.path3ElementalEffectChancePerLevel ?? 0.01f) * effectivePath3Level;
    private float ElementalEffectChanceBonusMP   => (BData?.baseSkillMultiplier ?? 0f) * magicPower / 100f;
    private float ElementalEffectChanceBonus     => ElementalEffectChanceBonusBase + ElementalEffectChanceBonusMP;

    private float AttackSpeedBonusBase => (BData?.baseAttackSpeedBonus ?? 0f) + (BData?.path3AttackSpeedBonusPerLevel ?? 0.04f) * effectivePath3Level;
    private float AttackSpeedBonusMP   => (BData?.baseSkillMultiplier ?? 0f) * magicPower / 100f;
    private float AttackSpeedBonus     => AttackSpeedBonusBase + AttackSpeedBonusMP;
    private float BlossomRadius        => baseSkillRadius + (BData?.path3RadiusPerLevel ?? 0.15f) * effectivePath3Level;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void UpdateStats()
    {
        float path1EABonus = path1Level >= Plant.absoluteLevelCap ? 0.32f : 0f;
        elementalAffinityAdder += path1EABonus;
        base.UpdateStats();
        elementalAffinityAdder -= path1EABonus;
    }

    protected override void Update()
    {
        base.Update();
        UpdateAura();
        UpdateHighlights();
        UpdateAutoCast();
    }

    private void UpdateAutoCast()
    {
        if (!autoCastEnabled) return;
        if (SkillReady) OnTargetConfirmed(autoCastPosition);
    }

    private void UpdateAura()
    {
        auraTickTimer += Time.deltaTime;
        if (auraTickTimer < auraTickInterval) return;
        auraTickTimer -= auraTickInterval;

        float magicPen = path2Level >= Plant.absoluteLevelCap ? 16f : 0f;
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > attackRange) continue;
            plant.ApplyEffect(new elementalAffinityBoostEffect(plant, 1, this, attackRange, elementalAffinityBonus, magicPen));
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
                plant.ApplyEffect(new BlossomingEffect(plant, skillDuration, effectivePath3Level + 1, this, ElementalEffectChanceBonus, AttackSpeedBonus));
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
        foreach (Plant p in _highlightedPlants)
            if (p != null) p.ClearHighlight();
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (BData?.path1AttackDamagePerLevel ?? 4f)  * level;
        baseAttackRange  = data.baseAttackRange  + (BData?.path1AttackRangePerLevel  ?? 0.2f) * level;
    }

    public override void OnPath2Upgrade(int level) { }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (BData?.path3SkillDurationPerLevel ?? 1f) * level;
    }

    public override string GetName() => "<b><color=green>Begonia</color></b>";
    public override string GetDescription() =>
        $"The {GetName()} infuses nearby allies with Elemental Affinity and can bless them with the power of grass.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl    = BData?.path1AttackDamagePerLevel ?? 4f;
        float rangepl = BData?.path1AttackRangePerLevel  ?? 0.2f;
        string desc = details
            ? $"Fire a magical bolt dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F2}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Increases {GetName()}'s <color=green><b>Elemental Affinity</b></color> by <color=green><b>32%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float eppl = BData?.path2elementalAffinityPerLevel ?? 0.06f;
        float mpMult = BData?.basePassiveMultiplier ?? 0f;
        string desc = details
            ? $"Plants within her attack radius are granted <color=green><b>Begonia's Blessing</b></color>, increasing Elemental Affinity by <color=green><b>[({(BData?.baseelementalAffinityBonus ?? 0f) * 100f:F0}%) + ({eppl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{mpMult * 100f:F0}% Magic Power</color>]</b></color>."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase Elemental Affinity bonus by <color=green><b>{eppl * 100f:F0}%</b></color> per level. [<color=green><b>+{eppl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Plants affected by <color=green><b>Begonia's Blessing</b></color> are also granted <color=green><b>16 Magic Penetration</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float eecpl    = BData?.path3ElementalEffectChancePerLevel ?? 0.01f;
        float aspl     = BData?.path3AttackSpeedBonusPerLevel  ?? 0.04f;
        float radiuspl = BData?.path3RadiusPerLevel            ?? 0.15f;
        float durpl    = BData?.path3SkillDurationPerLevel     ?? 1f;
        float mpMult   = BData?.baseSkillMultiplier ?? 0f;
        string desc = details
            ? $"Target an area on the field (radius <color=green><b>[({data.baseSkillRadius:F2}) + ({radiuspl:F2}/Lvl.)]</b></color>). Plants within are granted <color=green><b>Blossoming</b></color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds, " +
              $"increasing <color=green><b>Elemental Effect Chance</b></color> by <color=green><b>[({(BData?.baseElementalEffectChanceBonus ?? 0.02f) * 100f:F0}%) + ({eecpl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{mpMult * 100f:F0}% Magic Power</color>]</b></color> " +
              $"and <color=green><b>Attack Speed</b></color> by <color=green><b>[({(BData?.baseAttackSpeedBonus ?? 0f) * 100f:F0}%) + ({aspl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{mpMult * 100f:F0}% Magic Power</color>]</b></color>."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Elemental Effect Chance</b></color> bonus by <color=green><b>{eecpl * 100f:F0}%</b></color> per level. [<color=green><b>+{eecpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Attack Speed</b></color> bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase radius by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Also increases <color=green><b>Minimum Damage</b></color> by <color=green><b>10%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetAttackDescription() =>
        $"Fire a magical bolt dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        $"Plants within her attack radius are granted <color=green><b>Begonia's Blessing</b></color>, " +
        $"increasing Elemental Affinity by <color=green><b>{elementalAffinityBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{elementalAffinityBonusMP * 100f:F0}%</b></color>].";

    public override string GetSkillDesription() =>
        $"Target an area on the field. Plants within are granted <color=green><b>Blossoming</b></color> for <color=green><b>{skillDuration:F0}s</b></color>, " +
        $"increasing <color=green><b>Elemental Effect Chance</b></color> by <color=green><b>{ElementalEffectChanceBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{ElementalEffectChanceBonusMP * 100f:F0}%</b></color>] " +
        $"and <color=green><b>Attack Speed</b></color> by <color=green><b>{AttackSpeedBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{AttackSpeedBonusMP * 100f:F0}%</b></color>].";
}
