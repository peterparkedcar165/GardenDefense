using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Dandelion : Shooter
{
    [SerializeField] private GameObject windGustPrefab;
    [SerializeField] private GameObject windGustIndicatorPrefab;
    private GameObject windGustIndicatorInstance;
    private GameObject _windGustInstance;
    private const int IndicatorSlices = 15;
    private int _obstacleMask;

    private DandelionData DData => data as DandelionData;

    // Wind Gust (Skill)
    private float WindGustDamage  => (DData?.baseGustDamage ?? 42f) + (DData?.path3GustDamagePerLevel ?? 12f) * effectivePath3Level + attackDamage + skillDamageMultiplier * magicPower;
    private float WindGustRange   => (DData?.baseWindGustRange ?? 10f) + (DData?.path3WindGustRangePerLevel ?? 0.5f) * effectivePath3Level;
    private float GustHitboxSize  => (DData?.baseBeamWidth ?? 1f) + (DData?.path3BeamWidthPerLevel ?? 0.25f) * effectivePath3Level;
    private float GustSpeed       => DData?.baseGustSpeed ?? 2.5f; // deliberately flat/slow - no per-level scaling
    private const float GlobalGustRange = 30f;

    // Pollen Haste - granted to allied plants (never herself) touched by the gust
    private float HasteBonus    => (DData?.basePollenHasteBonus ?? 0.10f) + (DData?.path3HasteBonusPerLevel ?? 0.04f) * effectivePath3Level;
    // reads straight off the generic Skill Duration stat (scaled in OnPath3Upgrade) instead of
    // its own dedicated field, so generic Skill Duration fertilizers/skill-tree nodes affect it too
    private float HasteDuration => skillDuration;

    // how long an insect caught in the gust is Displaced/swept along for
    private const float BaseTrapDuration = 1f;
    private const float Path3MaxTrapExtraDuration = 2f;
    private float TrapDuration => BaseTrapDuration + (IsPath3Maxed ? Path3MaxTrapExtraDuration : 0f);

    // Path3 max: allied plants touched by the gust also get a flat % of their own Skill
    // Cooldown refunded
    private const float Path3MaxCooldownRefundPercent = 0.15f;
    private float CooldownRefundPercent => IsPath3Maxed ? Path3MaxCooldownRefundPercent : 0f;

    // Passive - periodically reduces nearby allied plants' Skill Cooldown
    private float passiveTickTimer;
    private float PassiveInterval   => Mathf.Max(1f, (DData?.basePassiveInterval ?? 16f) - (DData?.path2IntervalReductionPerLevel ?? 1f) * effectivePath2Level);
    private float PassiveProcChance => (DData?.basePassiveProcChance ?? 0.5f) + (DData?.path2ProcChancePerLevel ?? 0.05f) * effectivePath2Level;
    // seconds knocked off nearby plants' cooldown per pulse
    private const float PassiveTickReductionMPScaling = 0.02f;
    private float PassiveTickReductionMP => magicPower * PassiveTickReductionMPScaling;
    private float PassiveTickReduction =>
        (DData?.basePassiveTickReduction ?? 1f) + (DData?.path2TickReductionPerLevel ?? 0.25f) * effectivePath2Level + PassiveTickReductionMP;
    private const float ProcTimerReduction = 1f;   // seconds the attack-hit proc adds toward the next pulse
    private const float Path1MaxExtraProcReduction = 1f;

    // Path2 max: allied plants within her attack radius have increased Skill Damage
    private const float SkillDamageAuraBonus = 0.25f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        _obstacleMask = LayerMask.GetMask("Obstacle");
        Plant.OnPlantPlaced += HandlePlantPlaced;
        ApplyAuraToAllInRange();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Plant.OnPlantPlaced -= HandlePlantPlaced;
        if (_windGustInstance != null) Destroy(_windGustInstance);
    }

    private void HandlePlantPlaced(Plant plant)
    {
        if (!IsAlive) return;
        ApplyAuraToAllInRange();
    }

    // Path2 max only - reapplied whenever a new plant is placed nearby or Path2 reaches max,
    // since plants are stationary once placed and the buff self-expires via PlantAuraBuffEffect
    // the instant a target leaves range or this plant dies
    private void ApplyAuraToAllInRange()
    {
        if (!IsPath2Maxed) return;
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive || plant == this) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > attackRange) continue;
            plant.ApplyEffect(new PollenBoonEffect(plant, 1, this, attackRange, SkillDamageAuraBonus));
        }
    }

    protected override void Update()
    {
        base.Update();
        UpdateWindGustIndicator();
        UpdatePassivePulse();
    }

    // shows progress toward the next passive pulse under the health bar, same as Photosynthesis
    // plants (e.g. Sunflower) and BogIris's own custom up-counting sun timer
    protected override bool GetPassiveBarVisible() => true;
    protected override float GetPassiveBarFill() =>
        PassiveInterval > 0f ? Mathf.Clamp01(passiveTickTimer / PassiveInterval) : 0f;

    private void UpdatePassivePulse()
    {
        passiveTickTimer += Time.deltaTime;
        if (passiveTickTimer < PassiveInterval) return;
        passiveTickTimer -= PassiveInterval;

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive || plant == this) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > attackRange) continue;
            plant.ReduceSkillCooldown(PassiveTickReduction);
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject targetGO = FindTarget();
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        DandelionProjectile seed = proj.GetComponent<DandelionProjectile>();
        if (seed != null)
        {
            seed.SetTarget(targetGO);
            seed.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    // called by DandelionProjectile on every insect it hits along its path (piercing means this
    // can fire several times per shot, once independently per insect struck) - a chance to push
    // the passive's own internal pulse timer closer to firing, same pattern as
    // BogIris.TryReduceSunTimer
    public void TryAcceleratePassive(Insect insect)
    {
        if (!insect.IsAlive) return;
        if (Random.value >= PassiveProcChance * (1f + bonusEffectChance)) return;
        passiveTickTimer += ProcTimerReduction + (IsPath1Maxed ? Path1MaxExtraProcReduction : 0f);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + level * (DData?.path1AttackSpeedPerLevel ?? 0.05f);
        baseMagicPower  = data.baseMagicPower  + level * (DData?.path1MagicPowerPerLevel  ?? 5f);
    }

    public override void OnPath2Upgrade(int level) => ApplyAuraToAllInRange();

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + level * (DData?.path3SkillDurationPerLevel ?? 2f);
    }

    public override void ActivateSkill()
    {
        if (windGustIndicatorInstance != null) return;
        SkillTargetingManager.instance.BeginTargeting(0f, OnTargetConfirmed);
        if (windGustIndicatorPrefab != null)
        {
            windGustIndicatorInstance = Instantiate(windGustIndicatorPrefab, transform.position, Quaternion.identity);
            windGustIndicatorInstance.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void OnTargetConfirmed(Vector3 targetPosition)
    {
        skillCooldownTimer = skillCooldown;
        bool isGlobal = IsPath3Maxed;
        if (!isGlobal) BeginChannel();
        Vector2 direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        float gustRange = isGlobal ? GlobalGustRange : WindGustRange;
        if (windGustPrefab == null) return;
        _windGustInstance = Instantiate(windGustPrefab, transform.position, Quaternion.identity);
        _windGustInstance.GetComponent<WindGust>()?.Initialize(transform.position, direction, GustHitboxSize, GustSpeed, WindGustDamage, this, gustRange,
            TrapDuration, HasteBonus, HasteDuration, CooldownRefundPercent);
    }

    private void UpdateWindGustIndicator()
    {
        if (windGustIndicatorInstance == null) return;

        if (!SkillTargetingManager.instance.IsTargeting)
        {
            Destroy(windGustIndicatorInstance);
            windGustIndicatorInstance = null;
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        mouseWorld.z = 0f;

        float beamWidth = GustHitboxSize;
        float indicatorRange = IsPath3Maxed ? GlobalGustRange : WindGustRange;
        Vector2 dir  = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // the gust always travels forward only (a single projectile, never bidirectional, even
        // at max level), so the preview is always a forward-facing rectangle
        Vector3 indicatorCenter = transform.position + (Vector3)(dir * indicatorRange * 0.5f);
        windGustIndicatorInstance.transform.SetPositionAndRotation(indicatorCenter, Quaternion.Euler(0f, 0f, angle));
        windGustIndicatorInstance.transform.localScale = new Vector3(indicatorRange, beamWidth, 1f);
        windGustIndicatorInstance.GetComponent<SpriteRenderer>().enabled = true;

        /* multi-slice obstacle clipping , re-enable when ready
        Vector2 perp = new Vector2(-dir.y, dir.x);
        EnsureSlices(windGustIndicatorInstance, IndicatorSlices);
        float sliceWidth = beamWidth / IndicatorSlices;
        for (int i = 0; i < IndicatorSlices; i++)
        {
            float offset = (i + 0.5f - IndicatorSlices * 0.5f) * sliceWidth;
            Vector2 origin = (Vector2)transform.position + perp * offset;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, WindGustRange, _obstacleMask);
            float len = hit.collider != null ? hit.distance : WindGustRange;
            Transform slice = windGustIndicatorInstance.transform.GetChild(i);
            slice.position   = (Vector3)(origin + dir * len * 0.5f);
            slice.rotation   = Quaternion.Euler(0f, 0f, angle);
            slice.localScale = new Vector3(len, sliceWidth, 1f);
            slice.GetComponent<SpriteRenderer>().enabled = len > 0.05f;
        }
        */
    }

    private void EnsureSlices(GameObject indicator, int count)
    {
        if (indicator.transform.childCount == count) return;

        for (int i = indicator.transform.childCount - 1; i >= 0; i--)
            Destroy(indicator.transform.GetChild(i).gameObject);

        SpriteRenderer root = indicator.GetComponent<SpriteRenderer>();
        if (root != null) root.enabled = false;

        for (int i = 0; i < count; i++)
        {
            GameObject slice = new GameObject($"Slice_{i}");
            slice.transform.SetParent(indicator.transform);
            SpriteRenderer sr = slice.AddComponent<SpriteRenderer>();
            if (root != null)
            {
                sr.sprite         = root.sprite;
                sr.color          = root.color;
                sr.sortingLayerID = root.sortingLayerID;
                sr.sortingOrder   = root.sortingOrder;
            }
            sr.enabled = false;
        }
    }

    public override string GetName() => $"<b><color=#B2EBF2>{(data != null ? data.displayName : "Dandelion")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} rides the wind to support her allies, speeding up their skills and sweeping away anything in her path.";

    public override string GetPath1Description(bool details = false)
    {
        float aspl = DData?.path1AttackSpeedPerLevel ?? 0.05f;
        float mppl = DData?.path1MagicPowerPerLevel  ?? 5f;
        string desc = details
            ? $"Fires a pollen seed at a target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}."
            : $"Fires a pollen seed at a target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=#FFB6C1><b>Magic Power</b></color> by <color=green><b>{mppl:F0}</b></color> per level. [<color=green><b>+{mppl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Attacks that successfully proc the passive's timer reduction now reduce it by an additional <color=green><b>1</b></color> second.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float intervalpl  = DData?.path2IntervalReductionPerLevel ?? 1f;
        float chancepl    = DData?.path2ProcChancePerLevel ?? 0.05f;
        float reductionpl = DData?.path2TickReductionPerLevel ?? 0.25f;
        string desc = details
            ? $"Every <color=green><b>[({DData?.basePassiveInterval ?? 16f:F0}) - ({intervalpl:F0}/Lvl.)]</b></color> seconds, reduces the <color=green><b>Skill Cooldown</b></color> of allied plants within her attack radius by <color=green><b>[({DData?.basePassiveTickReduction ?? 1f:F2}) + ({reductionpl:F2}/Lvl.)]</b></color> [<color=#FFB6C1><b>+{PassiveTickReductionMPScaling * 100f:F0}% Magic Power</b></color>] seconds. Attacks have a <color=green><b>[({(DData?.basePassiveProcChance ?? 0.5f) * 100f:F0}%) + ({chancepl * 100f:F0}%/Lvl.)]</b></color> chance to reduce this timer by an additional <color=green><b>1</b></color> second."
            : $"Every <color=green><b>{PassiveInterval:F0}</b></color> seconds, reduces the <color=green><b>Skill Cooldown</b></color> of allied plants within her attack radius by <color=green><b>{PassiveTickReduction:F2}</b></color> [<color=#FFB6C1><b>+{PassiveTickReductionMP:F2}</b></color>] seconds. Attacks have a <color=green><b>{PassiveProcChance * 100f:F0}%</b></color> chance to reduce this timer by an additional <color=green><b>1</b></color> second.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Decrease the timer by <color=green><b>{intervalpl:F0}</b></color> second per level. [<color=green><b>-{intervalpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase chance by <color=green><b>{chancepl * 100f:F0}%</b></color> per level. [<color=green><b>+{chancepl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase the Skill Cooldown reduction by <color=green><b>{reductionpl:F2}</b></color> seconds per level. [<color=green><b>+{reductionpl * effectivePath2Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Allied plants within her attack radius have <color=green><b>Skill Damage</b></color> increased by <color=green><b>{SkillDamageAuraBonus * 100f:F0}%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float beampl      = DData?.path3BeamWidthPerLevel     ?? 0.25f;
        float rangepl      = DData?.path3WindGustRangePerLevel ?? 0.5f;
        float hastepl      = DData?.path3HasteBonusPerLevel    ?? 0.04f;
        float durpl        = DData?.path3SkillDurationPerLevel ?? 2f;
        float dmgpl        = DData?.path3GustDamagePerLevel    ?? 12f;
        float baseBeam     = DData?.baseBeamWidth ?? 1f;
        float baseRange    = DData?.baseWindGustRange ?? 10f;
        float baseHaste    = DData?.basePollenHasteBonus ?? 0.10f;
        float baseDmg      = DData?.baseGustDamage ?? 42f;
        string desc = details
            ? $"Fires a slow pollen seed with a <color=green><b>[({baseBeam:F2}) + ({beampl:F2}/Lvl.)]</b></color> unit wide hitbox towards the targeted direction, reaching <color=green><b>[({baseRange:F1}) + ({rangepl:F1}/Lvl.)]</b></color> units away. On impact, deals <color=green><b>[({baseDmg:F0}) + ({dmgpl:F0}/Lvl.)]</b></color> + <color=green><b>[100% Attack Damage]</b></color> <color=#FFB6C1>[+{skillDamageMultiplier * 100f:F0}% Magic Power]</color> {PlantData.DamageTypeLabel(damageType)} to insects and sweeps them along with the wind, <color=#E0E0E0>Displaced</color>, for <color=green><b>1</b></color> second. Allied plants touched by the wind (not herself) gain <color=#B2EBF2><b>Pollen Haste</b></color>, increasing <color=green><b>Skill Charge Rate</b></color> by <color=green><b>[({baseHaste * 100f:F0}%) + ({hastepl * 100f:F0}%/Lvl.)]</b></color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. Hits each target once."
            : $"Fires a slow pollen seed with a <color=green><b>{GustHitboxSize:F2}</b></color> unit wide hitbox towards the targeted direction, reaching <color=green><b>{WindGustRange:F1}</b></color> units away. On impact, deals <color={PlantData.ElementalColor(elementalType)}><b>{WindGustDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to insects and sweeps them along with the wind, <color=#E0E0E0>Displaced</color>, for <color=green><b>1</b></color> second. Allied plants touched by the wind (not herself) gain <color=#B2EBF2><b>Pollen Haste</b></color>, increasing <color=green><b>Skill Charge Rate</b></color> by <color=green><b>{HasteBonus * 100f:F0}%</b></color> for <color=green><b>{HasteDuration:F0}</b></color> seconds. Hits each target once.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase damage by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase hitbox size by <color=green><b>{beampl:F2}</b></color> per level. [<color=green><b>+{beampl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"Increase travel range by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase Pollen Haste's Skill Charge Rate bonus by <color=green><b>{hastepl * 100f:F0}%</b></color> per level. [<color=green><b>+{hastepl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Skill Duration</b></color> by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"The wind keeps insects trapped for <color=green><b>{Path3MaxTrapExtraDuration:F0}</b></color> extra seconds. Allied plants hit are refunded <color=green><b>{Path3MaxCooldownRefundPercent * 100f:F0}%</b></color> of their total Skill Cooldown.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
