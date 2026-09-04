using UnityEngine;
using System.Collections;

public class BogIris : Shooter
{
    [SerializeField] private GameObject geyserPrefab;
    [SerializeField] private SpriteRenderer closedVisual;
    [SerializeField] private SpriteRenderer openVisual;

    private SpriteRenderer _rootRenderer;
    private float sunTickTimer = 0f;
    private float regenTickTimer = 0f;
    private const float RegenTickInterval = 1f;

    // open/closed is HP-reactive ("healthy" = at full HP, closes the instant she takes damage),
    // but the actual flip is rate-limited by _stateChangeCooldownTimer so hovering right at max
    // HP (e.g. small chip damage racing the regen tick) can't flicker the state back and forth
    private bool _isOpen;
    private float _stateChangeCooldownTimer;
    private const float StateChangeCooldown = 3f;

    private BogIrisData BogData => data as BogIrisData;

    private const float ClosedArmorBonus = 30f; // Path2 max, while closed

    private float SunInterval => (BogData?.baseSunInterval ?? 4f) * (1f + sunGenerationCooldown);
    private int   BaseSunGenerated => BogData?.baseSunGenerated ?? 2;
    private int   OpenBonusSun => (BogData?.baseOpenBonusSun ?? 2) + (BogData?.path2OpenBonusSunPerLevel ?? 1) * effectivePath2Level;
    private float RegenPercentPerSecond => (BogData?.baseRegenPercent ?? 0.02f) + (BogData?.path2RegenPercentPerLevel ?? 0.01f) * effectivePath2Level;
    private float ReduceChance => (BogData?.baseReduceChance ?? 0.35f) + (BogData?.path2ReduceChancePerLevel ?? 0.05f) * effectivePath2Level;
    private float GeyserRadius => skillRadius + (BogData?.path3GeyserRadiusPerLevel ?? 0.15f) * effectivePath3Level;
    private float KnockUpHeight => ScaleCC(((BogData?.baseKnockUpHeight ?? 0f) + (BogData?.path3KnockUpPerLevel ?? 1f) * effectivePath3Level) * skillDuration);
    private float KnockUpForce => Mathf.Sqrt(2f * Insect.gravity * KnockUpHeight);
    private float GeyserDamage => (BogData?.baseGeyserDamage ?? 0f) + (BogData?.path3GeyserDamagePerLevel ?? 15f) * effectivePath3Level + skillDamageMultiplier * magicPower;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        _rootRenderer = GetComponent<SpriteRenderer>();
        _isOpen = health >= maxHealth;
        SetVisualState(_isOpen);
    }

    protected override void Update()
    {
        base.Update();
        UpdatePassive();
    }

    protected override bool GetPassiveBarVisible() => true;
    protected override float GetPassiveBarFill() =>
        SunInterval > 0f ? Mathf.Clamp01(sunTickTimer / SunInterval) : 0f;

    private void UpdatePassive()
    {
        if (_stateChangeCooldownTimer > 0f) _stateChangeCooldownTimer -= Time.deltaTime;

        bool healthy = health >= maxHealth;
        if (healthy != _isOpen && _stateChangeCooldownTimer <= 0f)
        {
            _isOpen = healthy;
            _stateChangeCooldownTimer = StateChangeCooldown;
            SetVisualState(_isOpen);
        }

        if (!_isOpen)
        {
            regenTickTimer += Time.deltaTime;
            if (regenTickTimer >= RegenTickInterval)
            {
                regenTickTimer -= RegenTickInterval;
                // Path2 max removes the out-of-combat requirement: the doubled rate is always active
                bool doubled = IsPath2Maxed || !IsInCombat;
                float regenPerTick = RegenPercentPerSecond * (doubled ? 2f : 1f);
                Heal(maxHealth * regenPerTick * (1f + healingReceived) * (1f + healingBonus));
            }
        }
        else
        {
            regenTickTimer = 0f;
        }

        sunTickTimer += Time.deltaTime;
        if (sunTickTimer >= SunInterval)
        {
            sunTickTimer -= SunInterval;
            GenerateSun(BaseSunGenerated + (_isOpen ? OpenBonusSun : 0));
        }
    }

    // called on every attack hit (see BogIrisProjectile.OnHit) - a chance to advance the Sun
    // timer by 1 second, making the next tick arrive sooner
    public void TryReduceSunTimer()
    {
        float procChance = ReduceChance * (1f + bonusEffectChance);
        if (Random.value >= procChance) return;
        sunTickTimer += 1f;
    }

    protected override SpriteRenderer GetMainRenderer()
    {
        return _isOpen ? openVisual : closedVisual;
    }

    private void SetVisualState(bool open)
    {
        ResetOutlineRenderers();
        if (_rootRenderer != null) _rootRenderer.enabled = !open;
        if (closedVisual != null) closedVisual.gameObject.SetActive(!open);
        if (openVisual != null) openVisual.gameObject.SetActive(open);
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        BogIrisProjectile bogProj = proj.GetComponent<BogIrisProjectile>();
        if (bogProj != null)
        {
            bogProj.SetTarget(FindTarget());
            bogProj.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(GeyserRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        skillCooldownTimer = skillCooldown;
        StartCoroutine(SpawnGeyser(position));
    }

    private IEnumerator SpawnGeyser(Vector3 position)
    {
        bool isRaining = WeatherManager.instance != null && WeatherManager.instance.HasWeather(WeatherType.Rain);
        yield return new WaitForSeconds(isRaining ? 0.5f : 1f);
        if (geyserPrefab == null) yield break;
        GameObject obj = Instantiate(geyserPrefab, position, Quaternion.identity);
        obj.GetComponent<Geyser>()?.Initialize(position, GeyserRadius, skillDuration, GeyserDamage, KnockUpForce, this);
        if (IsPath3Maxed)
        {
            foreach (Insect insect in new System.Collections.Generic.List<Insect>(Insect.allInsects))
            {
                if (insect == null || !insect.IsAlive) continue;
                if (Vector3.Distance(position, insect.transform.position) <= GeyserRadius)
                    insect.ApplyEffect(new GeyseredEffect(insect, BogData?.geyseredDuration ?? 8f, 1, this,
                        BogData?.geyseredArmorShred ?? 20f, BogData?.geyseredFallDamageResistanceShred ?? 0.15f));
            }
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (BogData?.path1AttackDamagePerLevel ?? 8f) * level;
        baseAttackSpeed  = data.baseAttackSpeed  + (BogData?.path1AttackSpeedPerLevel  ?? 0.05f) * level;
    }

    public override void UpdateStats()
    {
        // hidden, undocumented, skill charges faster per level of rain exposure
        int rainLevel = GetEffect<RainExposedEffect>()?.level ?? 0;
        skillChargeRateAdder = 0.2f * rainLevel;
        base.UpdateStats();
        if (IsPath1Maxed)
        {
            bonusEffectChance += 0.5f;
        }
        if (IsPath2Maxed && !_isOpen)
        {
            armor += (int)ClosedArmorBonus;
        }
    }

    // no per-level side effects to apply here anymore - RegenPercentPerSecond, OpenBonusSun and
    // ReduceChance are all computed live from effectivePath2Level
    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override string GetName() => "<b><color=#4FC3F7>Bog Iris</color></b>";
    public override string GetDescription() =>
        $"The {GetName()} is self-sufficient, providing herself with regeneration as well as generating sun for the garden.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = BogData?.path1AttackDamagePerLevel ?? 8f;
        float aspl = BogData?.path1AttackSpeedPerLevel  ?? 0.05f;
        string desc = details
            ? $"Fires a water bolt at a single target dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}."
            : $"Fires a water bolt at a single target dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Increase <color=green><b>Bonus Effect Chance</b></color> by <color=green><b>50%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float regenpl  = BogData?.path2RegenPercentPerLevel ?? 0.01f;
        int   sunpl    = BogData?.path2OpenBonusSunPerLevel ?? 1;
        float reducepl = BogData?.path2ReduceChancePerLevel ?? 0.05f;
        string desc = details
            ? $"Every <color=green><b>{SunInterval:F0}</b></color> seconds, generates <color=green><b>{BaseSunGenerated}</b></color> <color=yellow>Sun</color>.\n\n" +
              $"When damaged, she <b><color=#4FC3F7>closes</color></b>, regenerating <color=red><b>[({(BogData?.baseRegenPercent ?? 0.02f) * 100f:F0}%) + ({regenpl * 100f:F0}%/Lvl.)]</b></color> Max Health per second (doubled when out of combat).\n\n" +
              $"When healthy, she <b><color=#4FC3F7>opens</color></b>, generating <color=green><b>[({BogData?.baseOpenBonusSun ?? 2}) + ({sunpl}/Lvl.)]</b></color> additional <color=yellow>Sun</color> per production.\n\n" +
              $"Attacks have a <color=green><b>[({(BogData?.baseReduceChance ?? 0.35f) * 100f:F0}%) + ({reducepl * 100f:F0}%/Lvl.)]</b></color> chance to reduce the <color=yellow>Sun</color> generation timer by <color=green><b>1</b></color> second on hit."
            : $"Every <color=green><b>{SunInterval:F0}</b></color> seconds, generates <color=green><b>{BaseSunGenerated}</b></color> <color=yellow>Sun</color>.\n\n" +
              $"When damaged, she <b><color=#4FC3F7>closes</color></b>, regenerating <color=red><b>{RegenPercentPerSecond * 100f:F0}%</b></color> Max Health per second (doubled to <color=red><b>{RegenPercentPerSecond * 200f:F0}%</b></color> when out of combat).\n\n" +
              $"When healthy, she <b><color=#4FC3F7>opens</color></b>, generating <color=green><b>{OpenBonusSun}</b></color> additional <color=yellow>Sun</color> per production.\n\n" +
              $"Attacks have a <color=green><b>{ReduceChance * 100f:F0}%</b></color> chance to reduce the <color=yellow>Sun</color> generation timer by <color=green><b>1</b></color> second on hit.";
        string stateLine = $"<b><color=#4FC3F7>{(_isOpen ? "OPEN" : "CLOSED")}</color></b>";
        return $"Passive:\n\n{stateLine}\n\n{desc}\n\n" +
               $"Increase regeneration by <color=red><b>{regenpl * 100f:F0}%</b></color> per level. [<color=red><b>+{regenpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase open-state Sun production by <color=green><b>{sunpl}</b></color> per level. [<color=green><b>+{sunpl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase reduction chance by <color=green><b>{reducepl * 100f:F0}%</b></color> per level. [<color=green><b>+{reducepl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Remove the out-of-combat condition from the regeneration (always doubled). Increase Armor by <color=green><b>{ClosedArmorBonus:F0}</b></color> when in <b><color=#4FC3F7>closed</color></b> state.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float dmgpl    = BogData?.path3GeyserDamagePerLevel  ?? 15f;
        float knockpl  = BogData?.path3KnockUpPerLevel        ?? 1f;
        float radiuspl = BogData?.path3GeyserRadiusPerLevel   ?? 0.15f;
        string desc = details
            ? $"Target a location. After a brief delay, a geyser erupts, dealing <color=green><b>[({BogData?.baseGeyserDamage:F0}) + ({dmgpl:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.DamageTypeLabel(damageType)} and knocking all insects airborne by <color=green><b>[({BogData?.baseKnockUpHeight:F0}) + ({knockpl:F0}/Lvl.)]</b></color> units within radius <color=green><b>[({data.baseSkillRadius:F2}) + ({radiuspl:F2}/Lvl.)]</b></color>."
            : $"Target a location. After a brief delay, a geyser erupts, dealing <color={PlantData.ElementalColor(elementalType)}><b>{(BogData?.baseGeyserDamage ?? 0f) + dmgpl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} and knocking all insects airborne by <color=green><b>{KnockUpHeight:F0}</b></color> units.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase the flat component of geyser damage by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase the knock-up height by <color=green><b>{knockpl:F0}</b></color> per level. [<color=green><b>+{knockpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase the radius of the geyser by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Successful <color=#4FC3F7><b>Geyser</b></color> hits inflict <color=#4FC3F7><b>Geysered</b></color> for <color=green><b>{BogData?.geyseredDuration ?? 8f:F0}</b></color> seconds, reducing <color=#00CED1><b>Armor</b></color> by <color=red><b>{BogData?.geyseredArmorShred ?? 20f:F0}</b></color> and <color=#A0522D><b>Fall Damage Resistance</b></color> by <color=red><b>{(BogData?.geyseredFallDamageResistanceShred ?? 0.15f) * 100f:F0}%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
