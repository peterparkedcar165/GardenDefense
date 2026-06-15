using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// the Water Sniper. infinite range, single target (no pierce), high attack speed and crit damage.
// passive: crit chance over 100% becomes crit damage, and shots ground flyers. skill: designate a
// spot and rapid-fire it with bonus crit. a secondary toggle prioritizes flying targets
public class Cattail : Shooter
{
    [Header("Skill aim (directional lane)")]
    [SerializeField] private GameObject skillIndicatorPrefab;   // a thin rectangle lane indicator

    private float SkillLaneLength => CData?.skillLaneLength ?? 30f;
    private float SkillLaneWidth  => CData?.skillLaneWidth ?? 1f;

    private bool _skillActive;
    private float _skillTimer;
    private Vector2 _skillDir;
    private GameObject _skillIndicatorInstance;
    private float _overcritBonus;
    private int _skillHitCount;

    public bool IsSkillActive => _skillActive;
    public float SkillWaterBonus => _skillHitCount * 0.01f;

    public override bool ShowRangeCircle => false;        // infinite range, no circle
    public override bool UsesFlyingToggle => true;        // shows the Flying-first / Default toggle

    private CattailData CData => data as CattailData;

    public float GroundDuration => (CData?.groundDuration ?? 6f) + (CData?.path2GroundDurationPerLevel ?? 0.5f) * effectivePath2Level;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        Entity.OnCriticalHit += OnAnyCritHit;
    }

    public override void UpdateStats()
    {
        // passive base crit chance +/Lvl., set before base so the derived criticalChance picks it up
        baseCriticalChance = data.baseCriticalChance
            + (CData?.baseCritChanceBonus ?? 0.1f)
            + (CData?.path2CritChancePerLevel ?? 0.08f) * effectivePath2Level;

        base.UpdateStats();

        // passive: any crit chance above 100% converts into crit damage
        _overcritBonus = 0f;
        if (criticalChance > 1f)
        {
            float conversionRatio = (CData?.baseCritDamageConversion ?? 1f) + (CData?.path2CritDamageConversionPerLevel ?? 0.5f) * effectivePath2Level;
            _overcritBonus = (criticalChance - 1f) * conversionRatio;
            criticalDamage += _overcritBonus;
            criticalChance = 1f;
        }
    }

    protected override void Update()
    {
        base.Update();
        UpdateSkillIndicator();

        if (_skillActive)
        {
            _skillTimer -= Time.deltaTime;
            if (_skillTimer <= 0f) { EndSkill(); return; }

            // channel: fire straight down the aimed lane at the (boosted) attack rate, regardless of
            // whether an enemy is there. the normal auto-attack is blocked by the Channeling effect
            if (!IsStunned && attackCooldownTimer >= attackCooldown)
            {
                attackCooldownTimer = 0f;
                Shoot(transform.position + (Vector3)(_skillDir * SkillLaneLength));
            }
        }
    }

    protected override GameObject FindTarget()
    {
        // while channeling, darts fly straight down the lane (no homing target)
        if (_skillActive) return null;

        bool dark = DarknessManager.instance != null && DarknessManager.instance.isDark;

        if (prioritizeFlying)
        {
            List<Insect> flyers = new List<Insect>();
            foreach (Insect insect in Insect.allInsects)
            {
                if (insect == null || !insect.IsAlive || !insect.isFlying) continue;
                if (dark && !DarknessManager.instance.IsIlluminated(insect.transform.position)) continue;
                flyers.Add(insect);
            }
            GameObject flyer = SelectByMode(flyers);
            if (flyer != null) return flyer;
        }

        if (!dark) return base.FindTarget();

        List<Insect> illuminated = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (DarknessManager.instance.IsIlluminated(insect.transform.position))
                illuminated.Add(insect);
        }
        return targeting switch
        {
            TARGETING.First   => FindFirst(illuminated),
            TARGETING.Nearest => FindNearest(illuminated),
            TARGETING.Last    => FindLast(illuminated),
            _                 => null,
        };
    }

    private GameObject SelectByMode(List<Insect> list) => targeting switch
    {
        TARGETING.First   => FindFirst(list),
        TARGETING.Nearest => FindNearest(list),
        TARGETING.Last    => FindLast(list),
        _                 => null,
    };

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        CattailProjectile dart = projectile.GetComponent<CattailProjectile>();
        if (dart != null)
        {
            dart.SetTarget(FindTarget());
            dart.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (CData?.path1AttackDamagePerLevel ?? 5f)    * level;
        baseAttackSpeed  = data.baseAttackSpeed  + (CData?.path1AttackSpeedPerLevel  ?? 0.05f) * level;
    }

    public override void OnPath2Upgrade(int level) { }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (CData?.path3DurationPerLevel ?? 0.5f) * level;
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        if (_skillIndicatorInstance != null) return;
        SkillTargetingManager.instance.BeginTargeting(0f, OnSkillConfirmed);   // 0 radius: our lane indicator is the visual
        if (skillIndicatorPrefab != null)
        {
            _skillIndicatorInstance = Instantiate(skillIndicatorPrefab, transform.position, Quaternion.identity);
            SpriteRenderer sr = _skillIndicatorInstance.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;   // hidden until the first UpdateSkillIndicator
        }
    }

    // a thin rectangle pivoted at the plant, pointing at the mouse, spanning the lane
    private void UpdateSkillIndicator()
    {
        if (_skillIndicatorInstance == null) return;

        if (SkillTargetingManager.instance == null || !SkillTargetingManager.instance.IsTargeting)
        {
            Destroy(_skillIndicatorInstance);
            _skillIndicatorInstance = null;
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld  = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        mouseWorld.z = 0f;
        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Vector3 center = transform.position + (Vector3)(dir * SkillLaneLength * 0.5f);
        _skillIndicatorInstance.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle));
        _skillIndicatorInstance.transform.localScale = new Vector3(SkillLaneLength, SkillLaneWidth, 1f);
        SpriteRenderer sr = _skillIndicatorInstance.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
    }

    private void OnSkillConfirmed(Vector3 point)
    {
        if (_skillIndicatorInstance != null) { Destroy(_skillIndicatorInstance); _skillIndicatorInstance = null; }
        Vector2 dir = ((Vector2)point - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        _skillDir    = dir;
        _skillActive = true;
        _skillTimer  = skillDuration;
        skillCooldownTimer = skillCooldown;

        float aspl     = CData?.path3AttackSpeedPerLevel ?? 0.25f;
        float asBonus  = (CData?.baseSkillAttackSpeedBonus ?? 2f) + aspl * effectivePath3Level;
        float critpl   = CData?.path3CritGrantPerLevel ?? 0.1f;
        float critBonus = (CData?.baseSkillCritGrant ?? 0.5f) + critpl * effectivePath3Level;
        ApplyEffect(new AquaticSniperEffect(this, skillDuration, asBonus, critBonus, this));
    }

    private void EndSkill()
    {
        _skillActive = false;
        RemoveEffect<AquaticSniperEffect>();
        waterDamageAdder -= _skillHitCount * 0.01f;
        _skillHitCount = 0;
        UpdateStats();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Entity.OnCriticalHit -= OnAnyCritHit;
        if (_skillIndicatorInstance != null) Destroy(_skillIndicatorInstance);
    }

    private void OnAnyCritHit(Entity critSource, Entity target)
    {
        if (critSource != this || !IsPath2Maxed) return;
        skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - 0.5f);
    }

    public void OnSkillHit()
    {
        _skillHitCount++;
        waterDamageAdder += 0.01f;
        UpdateStats();
    }

    public override string GetDescription() =>
        $"The {GetName()} is a marsh sniper that fires high-pressure water darts from any range, melting elites and pinning flyers to the ground.";

    public override string GetName() => $"<b><color=#4FC3F7>{(data != null ? data.displayName : "Cattail")}</color></b>";

    public override string GetPath1Name() => "Attack";
    public override string GetPath2Name() => "Passive";
    public override string GetPath3Name() => "Skill";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = CData?.path1AttackDamagePerLevel ?? 5f;
        float aspl = CData?.path1AttackSpeedPerLevel ?? 0.05f;
        string desc = details
            ? $"Fires a high-pressure water dart, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to a single target at any range."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "For each unit of distance traveled, attacks deal <color=green><b>1.5%</b></color> increased damage.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetAttackDescription() =>
        $"Fires a high-pressure water dart, dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to a single target at any range.";

    public override string GetPath2Description(bool details = false)
    {
        float critpl   = CData?.path2CritChancePerLevel           ?? 0.08f;
        float grndpl   = CData?.path2GroundDurationPerLevel       ?? 0.5f;
        float baseConv = CData?.baseCritDamageConversion          ?? 1f;
        float convpl   = CData?.path2CritDamageConversionPerLevel ?? 0.5f;
        float currentConversion = baseConv + convpl * effectivePath2Level;
        string desc = details
            ? $"Every <color=green><b>1%</b></color> of <color=#FFD700><b>Critical Chance</b></color> above <color=green><b>100%</b></color> will be converted to <color=green><b>[({baseConv:F1}) + ({convpl:F1}/Lvl.)]%</b></color> <color=#FFD700><b>Critical Damage</b></color>.\n\n" +
              $"The Cattail's shots against <color=#B2EBF2>flying</color> insects will knock them <color=#A0522D><b>Grounded</b></color> for <color=green><b>[({CData?.groundDuration ?? 6f:F1}) + ({grndpl:F1}/Lvl.)]</b></color> seconds.\n\n" +
              $"Passive Base <color=#FFD700><b>Critical Chance</b></color> bonus: <color=green><b>[({(CData?.baseCritChanceBonus ?? 0.1f) * 100f:F0}%) + ({critpl * 100f:F0}%/Lvl.)]</b></color>."
            : $"Every <color=green><b>1%</b></color> of <color=#FFD700><b>Critical Chance</b></color> above <color=green><b>100%</b></color> will be converted to <color=green><b>{currentConversion:F1}%</b></color> <color=#FFD700><b>Critical Damage</b></color>.\n\n" +
              $"The Cattail's shots against <color=#B2EBF2>flying</color> insects will knock them <color=#A0522D><b>Grounded</b></color> for <color=green><b>{GroundDuration:F1}</b></color> seconds.\n\n" +
              $"Current <color=#FFD700><b>Critical Damage</b></color> Bonus: <color=green><b>{_overcritBonus * 100f:F1}%</b></color>";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase Base <color=#FFD700><b>Critical Chance</b></color> by <color=green><b>{critpl * 100f:F0}%</b></color> per level. [<color=green><b>+{critpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=#A0522D><b>Grounded</b></color> duration by <color=green><b>{grndpl:F1}</b></color> seconds per level. [<color=green><b>+{grndpl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"Increase the <color=#FFD700><b>Critical Damage</b></color> conversion by <color=green><b>{convpl:F1}%</b></color> per level. [<color=green><b>+{convpl * effectivePath2Level:F1}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"<color=#FFD700><b>Critical hits</b></color> reduce <color=green><b>Skill Cooldown</b></color> by <color=green><b>0.5s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float aspl     = CData?.path3AttackSpeedPerLevel  ?? 0.25f;
        float durpl    = CData?.path3DurationPerLevel      ?? 0.5f;
        float baseCrit = CData?.baseSkillCritGrant         ?? 0.5f;
        float critpl   = CData?.path3CritGrantPerLevel     ?? 0.1f;
        float baseAS   = CData?.baseSkillAttackSpeedBonus  ?? 2f;
        float totalCrit = baseCrit + critpl * effectivePath3Level;
        string desc = details
            ? $"Aim a direction and rain darts in the targeted direction for <color=green><b>[({data.baseSkillDuration:F1}) + ({durpl:F1}/Lvl.)]</b></color> seconds, gaining <color=green><b>+[({baseAS * 100f:F0}%) + ({aspl * 100f:F0}%/Lvl.)]</b></color> <color=green><b>Attack Speed</b></color> and <color=green><b>+[({baseCrit * 100f:F0}%) + ({critpl * 100f:F0}%/Lvl.)]</b></color> <color=#FFD700><b>Critical Chance</b></color>."
            : $"Aim a direction and rain darts in the targeted direction for <color=green><b>{skillDuration:F1}</b></color> seconds, gaining <color=green><b>+{(baseAS + aspl * effectivePath3Level) * 100f:F0}%</b></color> <color=green><b>Attack Speed</b></color> and <color=green><b>+{totalCrit * 100f:F0}%</b></color> <color=#FFD700><b>Critical Chance</b></color>.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Attack Speed</b></color> bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=#FFD700><b>Critical Chance</b></color> bonus by <color=green><b>{critpl * 100f:F0}%</b></color> per level. [<color=green><b>+{critpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F1}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path3Level, $"During <color=#4FC3F7><b>Aquatic Sniper</b></color>, each attack hit increases <color=#4FC3F7><b>Water Damage</b></color> by <color=green><b>1%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
