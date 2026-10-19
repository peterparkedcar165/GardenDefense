using UnityEngine;
using System.Collections.Generic;

public class Begonia : Aura
{
    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();

    // no target to remember anymore - just whether to auto-recast on herself every time ready
    private bool autoCastEnabled = false;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    private BegoniaData BData => data as BegoniaData;

    // Attack: her flowers tick independently of the generic attackSpeed/attackCooldown cycle -
    // she has no single target, so there's nothing for that cycle to drive here
    private float _flowerTickTimer;
    private const float FlowerTickInterval = 0.1f;
    // short buffer so BegoniaFlowersEffect naturally lapses shortly after an insect leaves range,
    // instead of needing to be manually removed - same trick Cactus's taunt pulse uses
    private const float FlowerEffectRefreshDuration = 0.35f;

    private float WaterCooldownReduction => (BData?.baseWaterCooldownReduction ?? 2f) + (BData?.path1CooldownReductionPerLevel ?? 0.6f) * effectivePath1Level;
    private float GrassResistanceShredAmount => BData?.maxLevelGrassResistanceShred ?? 0.35f;

    // skill tree node unlock ids
    public const string InstantSkillUnlock      = "begonia_instant_skill";
    public const string BloomingAffinityUnlock  = "begonia_blooming_affinity";
    public const string BloomingVigorUnlock     = "begonia_blooming_vigor";
    public const string FertileWatersUnlock     = "begonia_fertile_waters";
    public const string RebloomUnlock           = "begonia_rebloom";

    private float ElementalAffinityBonusBase => (BData?.baseElementalAffinityBonus ?? 0.24f) + (BData?.path2ElementalAffinityPerLevel ?? 0.08f) * effectivePath2Level;
    private float ElementalAffinityBonusMP   => (BData?.basePassiveMultiplier ?? 0f) * magicPower / 100f;
    private float ElementalAffinityBonus     => ElementalAffinityBonusBase + ElementalAffinityBonusMP;

    private float GrassDamageBonusBase => (BData?.baseGrassDamageBonus ?? 0.2f) + (BData?.path2GrassDamagePerLevel ?? 0.04f) * effectivePath2Level;
    private float GrassDamageBonusMP   => (BData?.basePassiveMultiplier ?? 0f) * magicPower / 100f;
    private float GrassDamageBonus     => GrassDamageBonusBase + GrassDamageBonusMP;

    private float GerminateSpeedupBonus => (BData?.baseGerminateSpeedup ?? 0.5f) + (BData?.path2GerminateSpeedupPerLevel ?? 0.1f) * effectivePath2Level;

    private float AttackSpeedBonusBase => (BData?.baseAttackSpeedBonus ?? 0f) + (BData?.path3AttackSpeedBonusPerLevel ?? 0.04f) * effectivePath3Level;
    private float AttackSpeedBonusMP   => (BData?.baseSkillMultiplier ?? 0f) * magicPower / 100f;
    private float AttackSpeedBonus     => AttackSpeedBonusBase + AttackSpeedBonusMP;

    private float PrimerCooldownReduction =>
        (BData?.basePrimerCooldownReduction ?? 0.5f) + (BData?.path3PrimerCooldownReductionPerLevel ?? 0.1f) * effectivePath3Level
        + (SkillTreeManager.HasUnlock(this, FertileWatersUnlock) ? 0.5f : 0f);

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        Plant.OnPlantPlaced += HandlePlantPlaced;
        ApplyAuraToAllInRange();

        // free skill readiness on placement - deliberately bypasses UnlockPath3() (which spends
        // sun and adds to totalSunSpent) so this can't be abused for an inflated uproot refund
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }
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

        if (autoCastEnabled && SkillReady) ActivateSkill();

        _flowerTickTimer += Time.deltaTime;
        if (_flowerTickTimer >= FlowerTickInterval)
        {
            _flowerTickTimer -= FlowerTickInterval;
            TickFlowers();
        }
    }

    // every tick: refresh (or apply) the standing-in-her-flowers debuff on every insect in range,
    // and reprime with Water if that insect's (now-shortened) internal cooldown has lapsed
    private void TickFlowers()
    {
        float reduction = WaterCooldownReduction;
        float shred = IsPath1Maxed ? GrassResistanceShredAmount : 0f;

        foreach (Insect insect in GetInsectsInRange())
        {
            BegoniaFlowersEffect existing = insect.GetEffect<BegoniaFlowersEffect>();
            if (existing != null)
                existing.duration = FlowerEffectRefreshDuration;
            else
                insect.ApplyEffect(new BegoniaFlowersEffect(insect, FlowerEffectRefreshDuration, this, reduction, shred));

            if (insect.waterInternalCooldown <= 0f)
            {
                insect.waterInternalCooldown = insect.ElementInternalCooldown(insect.waterInternalCooldownAdder, insect.waterInternalCooldownMultiplier);
                insect.ApplyEffect(new WaterPrimer(insect, insect.elementalDebuffDuration, 1, this));
            }
        }
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
            plant.ApplyEffect(new BegoniaBlessingEffect(plant, 1, this, attackRange, ElementalAffinityBonus, GrassDamageBonus, grantsGerminateCrit, GerminateSpeedupBonus));
        }
    }

    // immediate, self-centered: no more "click a point on the field" step. plants within her
    // attack range (the same range her passive aura already uses) are granted Blossoming
    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;

        // Blooming Affinity/Vigor (skill tree): a snapshot of her own current Begonia's Blessing
        // bonus, granted directly to Blossoming's targets - zero (no-op) when the matching
        // unlock isn't picked
        float blossomAffinityBonus = SkillTreeManager.HasUnlock(this, BloomingAffinityUnlock) ? ElementalAffinityBonus : 0f;
        float blossomGrassBonus    = SkillTreeManager.HasUnlock(this, BloomingVigorUnlock)    ? GrassDamageBonus       : 0f;

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) <= attackRange)
                plant.ApplyEffect(new BlossomingEffect(plant, skillDuration, effectivePath3Level + 1, this, AttackSpeedBonus, PrimerCooldownReduction,
                    blossomAffinityBonus, blossomGrassBonus));
        }
    }

    // simple on/off now - no target to pick, so there's no targeting step to begin
    public override void ToggleAutoCast() => autoCastEnabled = !autoCastEnabled;

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled };

    public override void RestoreAutoCastState(AutoCastState state) =>
        autoCastEnabled = state.enabled;

    private void UpdateHighlights()
    {
        bool isSelected = PlantUpgradeUI.instance?.GetSelectedPlant() == this;

        var desired = new HashSet<Plant>();
        Color highlightColor = Color.green;

        if (isSelected)
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
        baseAttackRange = data.baseAttackRange + (BData?.path1AttackRangePerLevel ?? 0.2f) * level;
        ApplyAuraToAllInRange();
    }

    public override void OnPath2Upgrade(int level) => ApplyAuraToAllInRange();

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (BData?.path3SkillDurationPerLevel ?? 1f) * level;
    }

    public override string GetName() => "<b><color=#4FC3F7>Begonia</color></b>";
    public override string GetDescription() =>
        $"The {GetName()} grows wet flowers that prime nearby insects with water, and sharpens nearby allies' precision and power, blessing them with the strength of water.";

    public override string GetPath1Description(bool details = false)
    {
        float rangepl = BData?.path1AttackRangePerLevel ?? 0.2f;
        float cdrpl   = BData?.path1CooldownReductionPerLevel ?? 0.6f;
        string desc = details
            ? $"Grows wet flowers around her, applying <color=#4FC3F7><b>Water</b></color> primer to insects in contact with them and reducing its internal cooldown by <color=green><b>[({(BData?.baseWaterCooldownReduction ?? 2f):F1}s) + ({cdrpl:F1}s/Lvl.)]</b></color>."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F2}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=#4FC3F7><b>Water</b></color> internal cooldown reduction by <color=green><b>{cdrpl:F1}s</b></color> per level. [<color=green><b>+{cdrpl * effectivePath1Level:F1}s</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Insects in contact with her flowers have <color=green><b>{GrassResistanceShredAmount * 100f:F0}%</b></color> reduced <color=green><b>Grass Resistance</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float eapl = BData?.path2ElementalAffinityPerLevel ?? 0.08f;
        float gdpl = BData?.path2GrassDamagePerLevel ?? 0.04f;
        float gspl = BData?.path2GerminateSpeedupPerLevel ?? 0.1f;
        float mpMult = BData?.basePassiveMultiplier ?? 0f;
        string desc = details
            ? $"Plants within her attack radius are granted <color=#4FC3F7><b>Begonia's Blessing</b></color>, increasing <color=green><b>Elemental Affinity</b></color> by <color=green><b>[({(BData?.baseElementalAffinityBonus ?? 0.24f) * 100f:F0}%) + ({eapl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{mpMult * 100f:F0}% Magic Power</color>]</b></color> and <color=green><b>Grass Damage</b></color> by <color=green><b>[({(BData?.baseGrassDamageBonus ?? 0.2f) * 100f:F0}%) + ({gdpl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{mpMult * 100f:F0}% Magic Power</color>]</b></color>. Their <color=green><b>Germinate</b></color> triggers detonate <color=green><b>[({(BData?.baseGerminateSpeedup ?? 0.5f):F1}s) + ({gspl:F1}s/Lvl.)]</b></color> earlier."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Elemental Affinity</b></color> bonus by <color=green><b>{eapl * 100f:F0}%</b></color> per level. [<color=green><b>+{eapl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Grass Damage</b></color> bonus by <color=green><b>{gdpl * 100f:F0}%</b></color> per level. [<color=green><b>+{gdpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Germinate</b></color> speedup by <color=green><b>{gspl:F1}s</b></color> per level. [<color=green><b>+{gspl * effectivePath2Level:F1}s</b></color>]\n\n" +
               $"{Level5Section(path2Level, "<color=#4FC3F7><b>Begonia's Blessing</b></color> allows <color=green><b>Germinate</b></color> triggers to critically strike.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float aspl     = BData?.path3AttackSpeedBonusPerLevel  ?? 0.04f;
        float durpl    = BData?.path3SkillDurationPerLevel     ?? 1f;
        float pcrpl    = BData?.path3PrimerCooldownReductionPerLevel ?? 0.1f;
        float mpMult   = BData?.baseSkillMultiplier ?? 0f;
        string desc = details
            ? $"Plants within her attack range are granted <color=green><b>Blossoming</b></color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds, " +
              $"increasing <color=green><b>Attack Speed</b></color> by <color=green><b>[({(BData?.baseAttackSpeedBonus ?? 0f) * 100f:F0}%) + ({aspl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{mpMult * 100f:F0}% Magic Power</color>]</b></color>. " +
              $"While active, dealing <color=#4FC3F7><b>Water</b></color> or <color=green><b>Grass</b></color> damage reduces that insect's own primer cooldown for that element by <color=green><b>[({(BData?.basePrimerCooldownReduction ?? 0.5f):F1}s) + ({pcrpl:F1}s/Lvl.)]</b></color>."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Attack Speed</b></color> bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase primer cooldown reduction by <color=green><b>{pcrpl:F1}s</b></color> per level. [<color=green><b>+{pcrpl * effectivePath3Level:F1}s</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Blossoming plants' <color=green><b>Germinate</b></color> effect has its radius increased by <color=green><b>50%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetAttackDescription() =>
        $"Grows wet flowers around her, applying <color=#4FC3F7><b>Water</b></color> primer to insects in contact with them and reducing its internal cooldown by <color=green><b>{WaterCooldownReduction:F1}s</b></color>.";

    public override string GetPassiveDescription() =>
        $"Plants within her attack radius are granted <color=#4FC3F7><b>Begonia's Blessing</b></color>, " +
        $"increasing <color=green><b>Elemental Affinity</b></color> by <color=green><b>{ElementalAffinityBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{ElementalAffinityBonusMP * 100f:F0}%</b></color>] " +
        $"and <color=green><b>Grass Damage</b></color> by <color=green><b>{GrassDamageBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{GrassDamageBonusMP * 100f:F0}%</b></color>]. " +
        $"Their <color=green><b>Germinate</b></color> triggers detonate <color=green><b>{GerminateSpeedupBonus:F1}s</b></color> earlier.";

    public override string GetSkillDesription() =>
        $"Plants within her attack range are granted <color=green><b>Blossoming</b></color> for <color=green><b>{skillDuration:F0}s</b></color>, " +
        $"increasing <color=green><b>Attack Speed</b></color> by <color=green><b>{AttackSpeedBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{AttackSpeedBonusMP * 100f:F0}%</b></color>]. " +
        $"While active, dealing <color=#4FC3F7><b>Water</b></color> or <color=green><b>Grass</b></color> damage reduces that insect's own primer cooldown for that element by <color=green><b>{PrimerCooldownReduction:F1}s</b></color>.";
}
