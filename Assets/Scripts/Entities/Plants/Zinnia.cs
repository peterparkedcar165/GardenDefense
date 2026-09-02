using UnityEngine;
using System.Collections.Generic;

public class Zinnia : Aura
{
    private ZinniaData ZData => data as ZinniaData;
    [SerializeField] private GameObject fireBurstPrefab;
    [SerializeField] private float sunRadiusMultiplier = 2f;

    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();

    // no targeting needed: the skill just conjures the sun at her own position
    private bool autoCastEnabled = false;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    private float FireDamageBonusBase => (ZData?.baseFireDamageBonus ?? 0f)   + (ZData?.path2FireDamageBonusPerLevel ?? 0.04f) * effectivePath2Level;
    private float FireDamageBonusMP   => (ZData?.basePassiveMultiplier ?? 0f) * magicPower / 100f;
    private float FireDamageBonus     => FireDamageBonusBase + FireDamageBonusMP;
    private float MagicPowerBonus     => (ZData?.baseMagicPowerBonus ?? 0f)   + (ZData?.path2MagicPowerBonusPerLevel ?? 5f)    * effectivePath2Level;
    private float EABonusAtMaxLevel   => IsPath2Maxed ? (ZData?.baseElementalAffinityBonus ?? 0.1f) : 0f;
    public  float AblazeBonusDamage   => (ZData?.baseDetonationMultiplier ?? 0.6f) * magicPower + (ZData?.baseDetonationFlat ?? 15f);
    public  float AblazeMaxHealthPercent => (ZData?.baseAblazeMaxHealthPercent ?? 0.03f) + (ZData?.path1AblazeMaxHealthPercentPerLevel ?? 0.01f) * effectivePath1Level;
    private int   SunIntensity        => (ZData?.baseSkillIntensity ?? 0) + (ZData?.path3SunIntensityPerLevel ?? 1) * effectivePath3Level;
    private float SunHeatingPerSecond => (ZData?.baseSunHeatingPerSecond ?? 1f) + (ZData?.path3HeatingPerLevel ?? 0.2f) * effectivePath3Level;

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
        float path1ASBonus = IsPath1Maxed ? (ZData?.path1MaxAttackSpeedBonus ?? 0.4f) : 0f;
        attackSpeedTotalMultiplier += path1ASBonus;
        base.UpdateStats();
        attackSpeedTotalMultiplier -= path1ASBonus;
    }

    protected override void Update()
    {
        base.Update();

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && (HasInsectsInRange() || HasPlantsInRange()))
            Attack();

        if (autoCastEnabled && SkillReady)
            ActivateSkill();

        UpdateHighlights();
    }

    // click Auto Cast to toggle it on, click again to turn it off — no target to pick
    public override void ToggleAutoCast() => autoCastEnabled = !autoCastEnabled;

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled) return;
        autoCastEnabled = true;
    }

    protected override void Attack()
    {
        base.Attack();
        if (fireBurstPrefab != null)
        {
            GameObject burst = Instantiate(fireBurstPrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = burst.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                const float lifetime = 0.25f;

                var main = ps.main;
                main.startLifetime = lifetime;
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    attackRange * 0.7f / lifetime,
                    attackRange        / lifetime);

                var lvol = ps.limitVelocityOverLifetime;
                lvol.enabled = true;
                lvol.separateAxes = false;
                lvol.dampen = 0.4f;
                AnimationCurve limitCurve = new AnimationCurve(
                    new Keyframe(0f,   1f, 0f, 0f),
                    new Keyframe(0.5f, 1f, 0f, 0f),
                    new Keyframe(1f,   0f, 0f, 0f)
                );
                lvol.limit = new ParticleSystem.MinMaxCurve(attackRange / lifetime, limitCurve);

                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                col.color = new ParticleSystem.MinMaxGradient(gradient);
            }
        }
        List<Insect> insects = GetInsectsInRange();
        foreach (Insect insect in insects)
        {
            insect.Damage(attackDamage, damageType, elementalType, this, true,
                new DamageTag[] { DamageTag.AoE, DamageTag.Attack });
        }

        List<Plant> plants = GetPlantsInRange();
        foreach (Plant plant in plants)
        {
            plant.ApplyEffect(new AblazeEffect(plant, 8f, 1, this, AblazeBonusDamage, AblazeMaxHealthPercent));
        }
    }

    private List<Plant> GetPlantsInRange()
    {
        List<Plant> result = new List<Plant>();
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == this) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) <= attackRange)
                result.Add(plant);
        }
        return result;
    }

    private bool HasPlantsInRange()
    {
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == this) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) <= attackRange)
                return true;
        }
        return false;
    }

    // applied once on placement, on a Path2 upgrade, or whenever any new plant appears on the
    // field (via Plant.OnPlantPlaced) — not re-scanned every tick. removal is handled entirely
    // by ZinniaAuraEffect itself (PlantAuraBuffEffect base)
    private void ApplyAuraToAllInRange()
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > attackRange) continue;
            plant.ApplyEffect(new ZinniaAuraEffect(plant, 1, this, attackRange, FireDamageBonus, MagicPowerBonus, EABonusAtMaxLevel));
        }
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
            p.SetHighlight(Color.green);

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

    // lightEmissionRangeMultiplier is applied here directly rather than through lightEmissionRange
    // itself, since that stat scales off baseLightEmissionRange (0 for Zinnia) and would otherwise
    // always multiply out to zero regardless of any Illumination Range Multiplier fertilizer roll
    private float ArtificialSunRadius => (attackRange * sunRadiusMultiplier + lightEmissionRange) * (1f + lightEmissionRangeMultiplier);

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        ApplyEffect(new ArtificialSunEffect(this, skillDuration, this, ArtificialSunRadius, SunIntensity, LightIntensity, SunHeatingPerSecond, IsPath3Maxed));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + (ZData?.path1AttackSpeedPerLevel ?? 0.08f) * level;
        baseAttackRange = data.baseAttackRange + (ZData?.path1AttackRangePerLevel ?? 0.15f) * level;
    }

    public override void OnPath2Upgrade(int level) => ApplyAuraToAllInRange();

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (ZData?.path3SkillDurationPerLevel ?? 2f) * level;
    }

    public override string GetName() => $"<b><color=orange>{(data != null ? data.displayName : "Zinnia")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} ignites the battlefield with fiery synergy, burning nearby insects and marking nearby plants with <color=orange><b>Ablaze</b></color>, empowering them with fire and magic.";

    public override string GetPath1Description(bool details = false)
    {
        float aspl  = ZData?.path1AttackSpeedPerLevel ?? 0.08f;
        float arpl  = ZData?.path1AttackRangePerLevel ?? 0.15f;
        float maxAS = ZData?.path1MaxAttackSpeedBonus ?? 0.4f;
        float ablazepl = ZData?.path1AblazeMaxHealthPercentPerLevel ?? 0.01f;
        string desc = details
            ? $"Releases fiery sparks dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects in range. Nearby plants instead take no damage and are marked with <color=orange><b>Ablaze</b></color>, causing their next projectile attack to deal <color=green><b>[({ZData?.baseDetonationFlat ?? 15f:F0}) + ({(ZData?.baseDetonationMultiplier ?? 0.6f) * 100f:F0}% <color=#FFB6C1>Magic Power</color>)]</b></color> + <color=green><b>[({(ZData?.baseAblazeMaxHealthPercent ?? 0.03f) * 100f:F0}%) + ({ablazepl * 100f:F0}%/Lvl.)]</b></color> of the target's Max Health as bonus <color=orange><b>Fire</b></color> <color=#FFB6C1><b>Magic</b></color> damage."
            : $"Releases fiery sparks dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects in range. Nearby plants instead take no damage and are marked with <color=orange><b>Ablaze</b></color>, causing their next projectile attack to deal <color=orange><b>{AblazeBonusDamage:F0}</b></color> + <color=orange><b>{AblazeMaxHealthPercent * 100f:F0}%</b></color> of the target's Max Health as bonus <color=orange><b>Fire</b></color> <color=#FFB6C1><b>Magic</b></color> damage.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Attack Range</b></color> by <color=green><b>{arpl:F2}</b></color> per level. [<color=green><b>+{arpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=orange><b>Ablaze</b></color> Max Health damage by <color=green><b>{ablazepl * 100f:F1}%</b></color> per level. [<color=green><b>+{ablazepl * effectivePath1Level * 100f:F1}%</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Increases {GetName()}'s own <color=green><b>Attack Speed</b></color> by <color=green><b>{maxAS * 100f:F0}%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float fdpl   = ZData?.path2FireDamageBonusPerLevel ?? 0.04f;
        float mppl   = ZData?.path2MagicPowerBonusPerLevel ?? 5f;
        float fdMult = ZData?.basePassiveMultiplier ?? 0f;
        float eaBon  = ZData?.baseElementalAffinityBonus ?? 0.1f;
        string desc = details
            ? $"Plants within her attack radius gain <color=orange><b>Zinnia's Warmth</b></color>, increasing <color=orange><b>Fire Damage</b></color> by <color=green><b>[({(ZData?.baseFireDamageBonus ?? 0f) * 100f:F0}%) + ({fdpl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{fdMult * 100f:F0}% Magic Power</color>]</b></color> and <color=#FFB6C1><b>Magic Power</b></color> by <color=green><b>[({(ZData?.baseMagicPowerBonus ?? 0f):F0}) + ({mppl:F0}/Lvl.)]</b></color>."
            : $"Plants within her attack radius gain <color=orange><b>Zinnia's Warmth</b></color>, increasing <color=orange><b>Fire Damage</b></color> by <color=green><b>{FireDamageBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{FireDamageBonusMP * 100f:F0}%</b></color>] and <color=#FFB6C1><b>Magic Power</b></color> by <color=green><b>{MagicPowerBonus:F0}</b></color>.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=orange><b>Fire Damage</b></color> bonus by <color=green><b>{fdpl * 100f:F0}%</b></color> per level. [<color=green><b>+{fdpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=#FFB6C1><b>Magic Power</b></color> bonus by <color=green><b>{mppl:F0}</b></color> per level. [<color=green><b>+{mppl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Plants affected by <color=orange><b>Zinnia's Warmth</b></color> also gain <color=green><b>{eaBon * 100f:F0}% Elemental Affinity</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl = ZData?.path3SkillDurationPerLevel ?? 2f;
        int   sipl  = ZData?.path3SunIntensityPerLevel ?? 1;
        float baseHeat = ZData?.baseSunHeatingPerSecond ?? 1f;
        float heatpl   = ZData?.path3HeatingPerLevel    ?? 0.2f;
        string desc = details
            ? $"Gather energy from within to conjure an <color=orange><b>Artificial Sun</b></color> that shines within a radius of <color=green><b>{ArtificialSunRadius:F1}</b></color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. Plants illuminated by the sunlight receive the effects of being <color=green><b>Exposed to Sunlight</b></color> at intensity <color=green><b>[({ZData?.baseSkillIntensity ?? 0}) + ({sipl}/Lvl.)]</b></color>. The <color=orange><b>Artificial Sun</b></color> also emits heat, warming up plants for <color=green><b>[({baseHeat:F1}) + ({heatpl:F1}/Lvl.)]</b></color> per second."
            : GetSkillDesription();
        string exposureBreakdown = details
            ? $"<color=green><b>[Exposed to Sunlight]</b></color>: Increase <color=orange><b>Fire Damage</b></color> by <color=green><b>{SunlightExposedEffect.baseBonus * 100f:F0}% + {SunlightExposedEffect.bonusPerLevel * 100f:F0}%</b></color> per level of intensity. Decrease <color=#4FC3F7><b>Water Damage</b></color> by <color=green><b>{SunlightExposedEffect.baseBonus * 100f:F0}% + {SunlightExposedEffect.bonusPerLevel * 100f:F0}%</b></color> per level of intensity.\n\n"
            : "";
        return $"Skill:\n\n{desc}\n\n" +
               exposureBreakdown +
               $"Increase <color=orange><b>Artificial Sun</b></color> lifetime by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"Increase intensity by <color=green><b>{sipl}</b></color> per level. [<color=green><b>{SunIntensity}</b></color>]\n\n" +
               $"Increase warming by <color=green><b>{heatpl:F1}</b></color> per second per level. [<color=green><b>+{heatpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"<color=orange><b>Fire</b></color> plants illuminated by the <color=orange><b>Artificial Sun</b></color> benefit from an additional <color=green><b>Passive</b></color> level bonus.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetAttackDescription() =>
        $"Releases fiery sparks dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects in range. Nearby plants take no damage and are instead marked with <color=orange><b>Ablaze</b></color>.";

    public override string GetPassiveDescription() =>
        $"Plants within her radius gain <color=orange><b>Zinnia's Warmth</b></color>: <color=orange><b>+{FireDamageBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{FireDamageBonusMP * 100f:F0}%</b></color>] Fire Damage, <color=#FFB6C1><b>+{MagicPowerBonus:F0}</b></color> Magic Power.";

    public override string GetSkillDesription() =>
        $"Gather energy from within to conjure an <color=orange><b>Artificial Sun</b></color> that shines within a radius of <color=green><b>{ArtificialSunRadius:F1}</b></color> for <color=green><b>{skillDuration:F0}</b></color> seconds. Plants illuminated by the sunlight receive the effects of being <color=green><b>Exposed to Sunlight</b></color> at intensity <color=green><b>{SunIntensity}</b></color>. The <color=orange><b>Artificial Sun</b></color> also emits heat, warming up plants for <color=green><b>{SunHeatingPerSecond:F1}</b></color> per second.";
}
