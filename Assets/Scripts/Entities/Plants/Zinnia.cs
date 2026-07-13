using UnityEngine;
using System.Collections.Generic;

public class Zinnia : Aura
{
    private ZinniaData ZData => data as ZinniaData;
    [SerializeField] private GameObject fireBurstPrefab;

    private float auraTickTimer = 0f;
    private const float auraTickInterval   = 0.25f;
    private const float auraEffectDuration = 0.35f;

    private float _sunnyTimer  = 0f;
    private bool  _sunnyActive = false;

    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();

    private float FireDamageBonusBase => (ZData?.baseFireDamageBonus ?? 0f)   + (ZData?.path2FireDamageBonusPerLevel ?? 0.04f) * effectivePath2Level;
    private float FireDamageBonusMP   => (ZData?.basePassiveMultiplier ?? 0f) * magicPower / 100f;
    private float FireDamageBonus     => FireDamageBonusBase + FireDamageBonusMP;
    private float MagicPowerBonus     => (ZData?.baseMagicPowerBonus ?? 0f)   + (ZData?.path2MagicPowerBonusPerLevel ?? 5f)    * effectivePath2Level;
    private float EABonusAtMaxLevel   => IsPath2Maxed ? (ZData?.baseElementalAffinityBonus ?? 0.1f) : 0f;
    public  float AblazeBonusDamage   => (ZData?.baseDetonationMultiplier ?? 0.6f) * magicPower + (ZData?.baseDetonationFlat ?? 15f);
    private int   SunIntensity        => (ZData?.baseSkillIntensity ?? 0) + (ZData?.path3SunIntensityPerLevel ?? 1) * effectivePath3Level;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
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

        UpdateAura();
        UpdateSunnyTimer();
        UpdateHighlights();
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
            plant.ApplyEffect(new AblazeEffect(plant, 8f, 1, this, AblazeBonusDamage));
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

    private void UpdateAura()
    {
        auraTickTimer += Time.deltaTime;
        if (auraTickTimer < auraTickInterval) return;
        auraTickTimer -= auraTickInterval;

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > attackRange) continue;
            plant.ApplyEffect(new ZinniaAuraEffect(plant, auraEffectDuration, 1, this, FireDamageBonus, MagicPowerBonus, EABonusAtMaxLevel));
        }
    }

    private void UpdateSunnyTimer()
    {
        if (!_sunnyActive) return;
        _sunnyTimer -= Time.deltaTime;
        if (_sunnyTimer <= 0f)
        {
            _sunnyActive = false;
            WeatherManager.instance.RemoveWeather(WeatherType.Sunny);
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
        foreach (Plant p in _highlightedPlants)
            if (p != null) p.ClearHighlight();
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        WeatherManager.instance.ClearAllWeather();
        WeatherManager.instance.SetWeather(WeatherType.Sunny, SunIntensity);
        _sunnyTimer  = skillDuration;
        _sunnyActive = true;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + (ZData?.path1AttackSpeedPerLevel ?? 0.08f) * level;
        baseAttackRange = data.baseAttackRange + (ZData?.path1AttackRangePerLevel ?? 0.15f) * level;
    }

    public override void OnPath2Upgrade(int level) { }

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
        string desc = details
            ? $"Releases fiery sparks dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects in range. Nearby plants instead take no damage and are marked with <color=orange><b>Ablaze</b></color>, causing their next attack to deal <color=green><b>[({ZData?.baseDetonationFlat ?? 15f:F0}) + ({(ZData?.baseDetonationMultiplier ?? 0.6f) * 100f:F0}% <color=#FFB6C1>Magic Power</color>)]</b></color> bonus <color=orange><b>Fire</b></color> <color=#FFB6C1><b>Magic</b></color> damage."
            : $"Releases fiery sparks dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects in range. Nearby plants instead take no damage and are marked with <color=orange><b>Ablaze</b></color>, causing their next attack to deal <color=orange><b>{AblazeBonusDamage:F0}</b></color> bonus <color=orange><b>Fire</b></color> <color=#FFB6C1><b>Magic</b></color> damage.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Attack Range</b></color> by <color=green><b>{arpl:F2}</b></color> per level. [<color=green><b>+{arpl * effectivePath1Level:F2}</b></color>]\n\n" +
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
        string desc = details
            ? $"Call upon the sun, changing the weather to <color=orange><b>Sunny</b></color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds or until another plant changes the weather."
            : $"Call upon the sun, changing the weather to <color=orange><b>Sunny</b></color> for <color=green><b>{skillDuration:F0}s</b></color> or until another plant changes the weather.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}s</b></color> per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"Base <color=orange><b>Sun Intensity</b></color>: <color=green><b>{ZData?.baseSkillIntensity ?? 0}</b></color>. Increase by <color=green><b>{sipl}</b></color> per level. [<color=green><b>{SunIntensity}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetAttackDescription() =>
        $"Releases fiery sparks dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects in range. Nearby plants take no damage and are instead marked with <color=orange><b>Ablaze</b></color>.";

    public override string GetPassiveDescription() =>
        $"Plants within her radius gain <color=orange><b>Zinnia's Warmth</b></color>: <color=orange><b>+{FireDamageBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{FireDamageBonusMP * 100f:F0}%</b></color>] Fire Damage, <color=#FFB6C1><b>+{MagicPowerBonus:F0}</b></color> Magic Power.";

    public override string GetSkillDesription() =>
        $"Change the weather to <color=orange><b>Sunny</b></color> for <color=green><b>{skillDuration:F0}s</b></color>.";
}
