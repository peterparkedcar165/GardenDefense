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
    private const float indicatorLength = 30f;

    private SnowdropData SData => data as SnowdropData;

    private float baseSlow                 => SData?.baseSlow                 ?? 0.24f;
    private float scalingSlow              => SData?.scalingSlow              ?? 0.06f;
    private float coolingPerSecond         => SData?.coolingPerSecond         ?? 2f;
    private float blizzardChillMultiplier  => SData?.blizzardChillMultiplier  ?? 1.5f;
    private float blizzardDamagePerLevel    => SData?.blizzardDamagePerLevel    ?? 15f;
    private float blizzardDurationPerLevel  => SData?.blizzardDurationPerLevel  ?? 1f;
    private float blizzardCoolingMultiplier => SData?.blizzardCoolingMultiplier ?? 2f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        activeCooldown = 40f;
        blizzardWidth  = data.baseSkillRadius;
    }

    protected override void Update()
    {
        base.Update();

        // ── Passive: Chill on grounded insects (constant aura, refreshed every frame) ──
        List<Insect> inRange = GetInsectsInRange();
        foreach (Insect insect in inRange)
        {
            insect.ApplyEffect(new ChillEffect(insect, 0.5f, chillLevel, this, baseSlow, scalingSlow));
        }

        // ── Attack: damage burst scaled by attack speed ─────────────────────
        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else
            Attack();

        // ── Passive: Cooling on nearby plants ──────────────────────────────
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;
            plant.ApplyEffect(new CoolingEffect(plant, 0.5f, 1, this, coolingPerSecond));
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

        blizzardIndicatorInstance.transform.SetPositionAndRotation(
            transform.position + (Vector3)(dir * indicatorLength * 0.5f),
            Quaternion.Euler(0f, 0f, angle));
        blizzardIndicatorInstance.transform.localScale = new Vector3(indicatorLength, blizzardWidth, 1f);
        blizzardIndicatorInstance.GetComponent<SpriteRenderer>().enabled = true;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * 1f;
        baseAttackRange  = data.baseAttackRange  + level * 0.1f;
    }

    public override void OnPath2Upgrade(int level)
    {
        chillLevel = 1 + level;
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        blizzardDamage = (SData?.baseBlizzardDamage ?? 0f) + blizzardDamagePerLevel * effectivePath3Level + skillDamageMultiplier * magicPower;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = (SData?.baseBlizzardDuration ?? data.baseSkillDuration) + blizzardDurationPerLevel * level;
        blizzardWidth     = data.baseSkillRadius + 0.5f * level;
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
        Vector2 direction  = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        _blizzardInstance  = Instantiate(blizzardPrefab, transform.position, Quaternion.identity);
        _blizzardInstance.GetComponent<Blizzard>()?.Initialize(
            transform.position, direction, blizzardWidth, skillDuration, blizzardDamage,
            chillLevel + 1, this,
            baseSlow    * blizzardChillMultiplier,
            scalingSlow * blizzardChillMultiplier,
            coolingPerSecond * blizzardCoolingMultiplier);
    }

    protected override void Attack()
    {
        base.Attack();
        List<Insect> inRange = GetInsectsInRange();
        foreach (Insect insect in inRange)
        {
            insect.Damage(attackDamage, damageType, elementalType, this, false,
                new DamageTag[] { DamageTag.Attack, DamageTag.DoT, DamageTag.AoE });
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_blizzardInstance != null) Destroy(_blizzardInstance);
    }

    // ── Descriptions ───────────────────────────────────────────────────────

    public override string GetName() => "<b><color=#00FFFF>Snowdrop</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a frosty flower whose icy presence continuously damages and chills nearby insects, while cooling the plants around her.";

    public override string GetAttackDescription() =>
        $"Continuously deals <color=green><b>{attackDamage:F0}</b></color> <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage to all insects within range.";

    public override string GetPassiveDescription() =>
        $"Applies <color=#00FFFF>Chill</color> to nearby insects, slowing their movement by <color=green><b>{baseSlow * 100f:F0}%</b></color>.\n\n" +
        $"Plants within the radius receive <color=#00FFFF>Cooling</color>, reducing temperature by <color=green><b>{coolingPerSecond:F1}</b></color> per second, until comfort.";

    public override string GetSkillDesription() =>
        $"Aim a powerful blizzard in a chosen direction, dealing <color=green><b>{(SData?.baseBlizzardDamage ?? 0f) + blizzardDamagePerLevel * effectivePath3Level:F0}</b></color> " +
        $"[<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage per second " +
        $"to all insects in its path for <color=green><b>{skillDuration:F0}s</b></color>. " +
        $"Applies <color=#00FFFF>Chill</color> at <color=green><b>{blizzardChillMultiplier:F1}×</b></color> strength. " +
        $"Plants within the Blizzard also receive <color=#00FFFF>Cooling</color> effect for <color=green><b>{blizzardCoolingMultiplier:F1}×</b></color> the effect.";

    public override string GetPath1Description() =>
        $"Attack:\n\n{GetAttackDescription()}\n\n" +
        $"Increase Attack Damage by <color=green><b>1</b></color> per level. [<color=green><b>+{1f * effectivePath1Level:F0}</b></color>]\n\n" +
        $"Increase Attack Range by <color=green><b>0.1</b></color> per level. [<color=green><b>+{0.1f * effectivePath1Level:F1}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description() =>
        $"Passive:\n\n{GetPassiveDescription()}\n\n" +
        $"Increase <color=#00FFFF>Chill</color> level by <color=green><b>1</b></color> per level, adding <color=green><b>{scalingSlow * 100f:F0}%</b></color> slow. [<color=green><b>+{scalingSlow * 100f * effectivePath2Level:F0}%</b></color>]\n\n" +
        $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description() =>
        $"Skill:\n\n{GetSkillDesription()}\n\n" +
        $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
        $"Increase Blizzard Damage by <color=green><b>{blizzardDamagePerLevel:F0}</b></color> per second per level. [<color=green><b>+{blizzardDamagePerLevel * effectivePath3Level:F0}</b></color>]\n\n" +
        $"Increase Blizzard Duration by <color=green><b>{blizzardDurationPerLevel:F1}s</b></color> per level. [<color=green><b>+{blizzardDurationPerLevel * effectivePath3Level:F1}s</b></color>]\n\n" +
        $"Increase Blizzard Width by <color=green><b>0.5</b></color> per level. [<color=green><b>+{0.5f * effectivePath3Level:F1}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
