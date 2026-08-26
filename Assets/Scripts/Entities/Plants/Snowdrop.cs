using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Snowdrop : Aura
{
    public int chillLevel = 1;
    [SerializeField] private GameObject blizzardPrefab;
    [SerializeField] private GameObject blizzardIndicatorPrefab;
    private float blizzardWidth;
    public float blizzardDamage;
    private GameObject blizzardIndicatorInstance;
    private GameObject _blizzardInstance;
    private const int IndicatorSlices = 15;
    private int _obstacleMask;

    private SnowdropData SData => data as SnowdropData;

    private float baseSlow                 => SData?.baseSlow                 ?? 0.24f;
    private float scalingSlow              => SData?.scalingSlow              ?? 0.06f;
    private float baseCoolingPerSecond     => SData?.coolingPerSecond         ?? 2f;
    private float coolingPerSecondPerLevel => SData?.coolingPerSecondPerLevel ?? 0.1f;
    private float CoolingPerSecond         => baseCoolingPerSecond + coolingPerSecondPerLevel * effectivePath2Level;
    private float blizzardChillMultiplier  => SData?.blizzardChillMultiplier  ?? 1.5f;
    private float blizzardDamagePerLevel    => SData?.blizzardDamagePerLevel    ?? 15f;
    private float blizzardDurationPerLevel  => SData?.blizzardDurationPerLevel  ?? 1f;
    private float blizzardCoolingMultiplier => SData?.blizzardCoolingMultiplier ?? 2f;
    private float BlizzardRange => (SData?.baseBlizzardRange ?? 10f) + (SData?.path3BlizzardRangePerLevel ?? 0.5f) * effectivePath3Level;
    private const float GlobalBlizzardRange = 30f;

    // flat, non-upgradable bonus on top of the general Ice-type Cold Resistance
    private const float BonusHeatResistance = 0.5f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        activeCooldown = 40f;
        blizzardWidth  = data.baseSkillRadius;
        _obstacleMask  = LayerMask.GetMask("Obstacle");
        heatResistanceAdder += BonusHeatResistance;
    }

    protected override void Update()
    {
        base.Update();

        List<Insect> inRange = GetInsectsInRange();
        foreach (Insect insect in inRange)
        {
            insect.ApplyEffect(new ChillEffect(insect, 0.5f, chillLevel, this, baseSlow, scalingSlow));
        }

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();

        if (WeatherManager.instance != null && WeatherManager.instance.temperature == TemperatureType.Hot)
        {
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == null || !plant.IsAlive) continue;
                if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;
                plant.ApplyEffect(new CoolingEffect(plant, 0.5f, 1, this, CoolingPerSecond));
            }
        }

        UpdateBlizzardIndicator();
    }

    private void UpdateBlizzardIndicator()
    {
        if (blizzardIndicatorInstance == null) return;

        if (!SkillTargetingManager.instance.IsTargeting)
        {
            Destroy(blizzardIndicatorInstance);
            blizzardIndicatorInstance = null;
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld  = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        mouseWorld.z = 0f;

        Vector2 dir   = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        bool globalIndicator = IsPath3Maxed;
        float indicatorRange = globalIndicator ? GlobalBlizzardRange : BlizzardRange;
        Vector3 indicatorCenter = globalIndicator
            ? transform.position
            : transform.position + (Vector3)(dir * indicatorRange * 0.5f);
        float indicatorLength = globalIndicator ? indicatorRange * 2f : indicatorRange;

        blizzardIndicatorInstance.transform.SetPositionAndRotation(indicatorCenter, Quaternion.Euler(0f, 0f, angle));
        blizzardIndicatorInstance.transform.localScale = new Vector3(indicatorLength, blizzardWidth, 1f);
        blizzardIndicatorInstance.GetComponent<SpriteRenderer>().enabled = true;

        /* multi-slice obstacle clipping â€” re-enable when ready
        Vector2 perp = new Vector2(-dir.y, dir.x);
        EnsureSlices(blizzardIndicatorInstance, IndicatorSlices);
        float sliceWidth = blizzardWidth / IndicatorSlices;
        for (int i = 0; i < IndicatorSlices; i++)
        {
            float offset = (i + 0.5f - IndicatorSlices * 0.5f) * sliceWidth;
            Vector2 origin = (Vector2)transform.position + perp * offset;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, BlizzardRange, _obstacleMask);
            float len = hit.collider != null ? hit.distance : BlizzardRange;
            Transform slice = blizzardIndicatorInstance.transform.GetChild(i);
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
                sr.sprite           = root.sprite;
                sr.color            = root.color;
                sr.sortingLayerID   = root.sortingLayerID;
                sr.sortingOrder     = root.sortingOrder;
            }
            sr.enabled = false;
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * (SData?.path1AttackDamagePerLevel ?? 1f);
        baseAttackRange  = data.baseAttackRange  + level * (SData?.path1AttackRangePerLevel  ?? 0.1f);
    }

    public override void OnPath2Upgrade(int level)
    {
        chillLevel = 1 + level;
    }

    public override void UpdateStats()
    {
        float eeCBonus   = IsPath1Maxed ? (SData?.path1MaxElementalEffectChanceBonus ?? 0.04f) : 0f;
        float dmgPenalty = IsPath1Maxed ? (SData?.path1MaxAttackDamagePenalty        ?? 0.5f)  : 0f;
        float asBonus    = IsPath1Maxed ? (SData?.path1MaxAttackSpeedBonus           ?? 1f)    : 0f;
        elementalEffectChanceAdder  += eeCBonus;
        attackDamageTotalMultiplier -= dmgPenalty;
        attackSpeedTotalMultiplier  += asBonus;
        base.UpdateStats();
        elementalEffectChanceAdder  -= eeCBonus;
        attackDamageTotalMultiplier += dmgPenalty;
        attackSpeedTotalMultiplier  -= asBonus;
        blizzardDamage = (SData?.baseBlizzardDamage ?? 0f) + blizzardDamagePerLevel * effectivePath3Level + skillDamageMultiplier * magicPower;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = (SData?.baseBlizzardDuration ?? data.baseSkillDuration) + blizzardDurationPerLevel * level;
        blizzardWidth     = data.baseSkillRadius + (SData?.path3BlizzardWidthPerLevel ?? 0.5f) * level;
    }

    public override void ActivateSkill()
    {
        if (blizzardIndicatorInstance != null) return;
        SkillTargetingManager.instance.BeginTargeting(0f, OnTargetConfirmed);
        if (blizzardIndicatorPrefab != null)
        {
            blizzardIndicatorInstance = Instantiate(blizzardIndicatorPrefab, transform.position, Quaternion.identity);
            blizzardIndicatorInstance.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void OnTargetConfirmed(Vector3 targetPosition)
    {
        skillCooldownTimer = skillCooldown;
        if (!IsPath3Maxed) BeginChannel();
        Vector2 direction  = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        bool isGlobal = IsPath3Maxed;
        float range = isGlobal ? GlobalBlizzardRange : BlizzardRange;
        _blizzardInstance  = Instantiate(blizzardPrefab, transform.position, Quaternion.identity);
        _blizzardInstance.GetComponent<Blizzard>()?.Initialize(
            transform.position, direction, blizzardWidth, skillDuration, blizzardDamage,
            chillLevel + 1, this,
            baseSlow    * blizzardChillMultiplier,
            scalingSlow * blizzardChillMultiplier,
            CoolingPerSecond * blizzardCoolingMultiplier,
            range, isGlobal);
    }

    protected override void Attack()
    {
        base.Attack();
        List<Insect> inRange = GetInsectsInRange();
        DamageTag[] tags = new DamageTag[] { DamageTag.Attack, DamageTag.AoE };
        foreach (Insect insect in inRange)
        {
            insect.Damage(attackDamage, damageType, elementalType, this, false, tags);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_blizzardInstance != null) Destroy(_blizzardInstance);
    }

    public override string GetName() => "<b><color=#00FFFF>Snowdrop</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a frosty flower whose icy presence continuously damages and chills nearby insects, while cooling the plants around her.";

    public override string GetAttackDescription() =>
        $"Continuously deals <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects within range.";

    public override string GetPassiveDescription() =>
        $"Increase <color=orange><b>Heat Resistance</b></color> by <color=green><b>{BonusHeatResistance * 100f:F0}%</b></color>.\n\n" +
        $"Applies <color=#00FFFF>Chill</color> to nearby insects, slowing their movement and attack speed by <color=green><b>{(baseSlow + scalingSlow * effectivePath2Level) * 100f:F0}%</b></color>.\n\n" +
        $"Plants within the radius receive <color=#00FFFF>Cooling</color>, reducing temperature by <color=green><b>{CoolingPerSecond:F1}</b></color> per second, until comfort.";

    public override string GetSkillDesription() =>
        $"Aim a powerful blizzard in a chosen direction, dealing <color=green><b>{(SData?.baseBlizzardDamage ?? 0f) + blizzardDamagePerLevel * effectivePath3Level:F0}</b></color> " +
        $"[<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second " +
        $"to all insects in its path for <color=green><b>{skillDuration:F0}s</b></color>. " +
        $"Applies <color=#00FFFF>Chill</color> at <color=green><b>{blizzardChillMultiplier:F1}x</b></color> strength. " +
        $"Plants within the Blizzard also receive <color=#00FFFF>Cooling</color> effect for <color=green><b>{blizzardCoolingMultiplier:F1}x</b></color> the effect.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl    = SData?.path1AttackDamagePerLevel ?? 1f;
        float rangepl = SData?.path1AttackRangePerLevel  ?? 0.1f;
        string desc = details
            ? $"Continuously deals <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects within range."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Increase <color=green><b>Elemental Effect Chance</b></color> by <color=green><b>{(SData?.path1MaxElementalEffectChanceBonus ?? 0.04f) * 100f:F0}%</b></color>, reduce <color=green><b>Total Attack Damage</b></color> by <color=green><b>{(SData?.path1MaxAttackDamagePenalty ?? 0.5f) * 100f:F0}%</b></color>, and increase <color=green><b>Attack Speed</b></color> by <color=green><b>{(SData?.path1MaxAttackSpeedBonus ?? 1f) * 100f:F0}%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        string desc = details
            ? $"Increase <color=orange><b>Heat Resistance</b></color> by <color=green><b>{BonusHeatResistance * 100f:F0}%</b></color>.\n\n" +
              $"Applies <color=#00FFFF>Chill</color> to nearby insects at level <color=green><b>[1 + (1/Lvl.)]</b></color>, slowing their movement and attack speed by <color=green><b>[({baseSlow * 100f:F0}%) + ({scalingSlow * 100f:F0}%/Lvl.)]</b></color>.\n\n" +
              $"Plants within the radius receive <color=#00FFFF>Cooling</color>, reducing temperature by <color=green><b>[({baseCoolingPerSecond:F1}) + ({coolingPerSecondPerLevel:F1}/Lvl.)]</b></color> per second, until comfort."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=#00FFFF>Chill</color> level by <color=green><b>1</b></color> per level, adding <color=green><b>{scalingSlow * 100f:F0}%</b></color> slow per level. [<color=green><b>+{scalingSlow * 100f * effectivePath2Level:F0}%</b></color>]\n\n" +
               $"Increase <color=#00FFFF>Cooling</color> by <color=green><b>{coolingPerSecondPerLevel:F1}</b></color> per second per level. [<color=green><b>+{coolingPerSecondPerLevel * effectivePath2Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path2Level, "<color=#00FFFF>Chill</color> also reduces <color=#00FFFF><b>Ice Resistance</b></color> by <color=red><b>24%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float widthpl  = SData?.path3BlizzardWidthPerLevel ?? 0.5f;
        float rangepl  = SData?.path3BlizzardRangePerLevel ?? 0.5f;
        string desc = details
            ? $"Aim a powerful blizzard in a chosen direction, dealing <color=green><b>[({SData?.baseBlizzardDamage ?? 0f:F0}) + ({blizzardDamagePerLevel:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second for <color=green><b>[({SData?.baseBlizzardDuration ?? 5f:F0}) + ({blizzardDurationPerLevel:F1}/Lvl.)]</b></color> seconds. Applies <color=#00FFFF>Chill</color> at <color=green><b>{blizzardChillMultiplier:F1}x</b></color> strength. Plants within the Blizzard also receive <color=#00FFFF>Cooling</color> at <color=green><b>{blizzardCoolingMultiplier:F1}x</b></color> strength."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase Blizzard Damage by <color=green><b>{blizzardDamagePerLevel:F0}</b></color> per second per level. [<color=green><b>+{blizzardDamagePerLevel * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase Blizzard Duration by <color=green><b>{blizzardDurationPerLevel:F1}</b></color> seconds per level. [<color=green><b>+{blizzardDurationPerLevel * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase Blizzard Width by <color=green><b>{widthpl:F1}</b></color> per level. [<color=green><b>+{widthpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase Blizzard Range by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "No longer channeling while the Blizzard is active. The Blizzard now extends <color=green><b>globally</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
