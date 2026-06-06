using UnityEngine;
using System.Collections.Generic;

public class Aeonium : Shooter
{
    private AeoniumData AData => data as AeoniumData;

    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();

    private int   _sunGenerated;
    private float _sunTimerReductionBase;
    private float _sunTimerReductionMP;
    private float _sunTimerReduction => _sunTimerReductionBase + _sunTimerReductionMP;

    private float SunYieldBonus => (AData?.baseSunYieldBonus ?? 0.5f) + (AData?.path3SunYieldPerLevel ?? 0.05f) * effectivePath3Level;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        passiveCooldownTimer = passiveCooldown;
    }

    private void OnEnable()  => Entity.OnPlantAttackHit += HandleNearbyAttack;
    private void OnDisable() => Entity.OnPlantAttackHit -= HandleNearbyAttack;

    public override void UpdateStats()
    {
        base.UpdateStats();
        _sunGenerated          = (AData?.baseSunGenerated ?? 6) + (AData?.path2SunPerLevel ?? 1) * effectivePath2Level;
        _sunTimerReductionBase = (AData?.baseSunTimerReduction ?? 0.3f) + (AData?.path2SunTimerReductionPerLevel ?? 0.1f) * effectivePath2Level;
        _sunTimerReductionMP   = (AData?.sunTimerMPMultiplier ?? 0.003f) * magicPower;
    }

    protected override void Update()
    {
        base.Update();   // Shooter handles the energy-ball attack
        UpdateHighlights();

        // passive: generate sun on a timer
        if (passiveCooldownTimer <= 0)
        {
            int granted = GenerateSun(_sunGenerated);
            passiveCooldownTimer += passiveCooldown;
            SunIndicator.Spawn(transform.position + new Vector3(0.25f, 0.5f, 0f), granted);
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        AeoniumProjectile orb = proj.GetComponent<AeoniumProjectile>();
        if (orb != null)
        {
            orb.SetTarget(FindTarget());
            orb.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    // each nearby plant's projectile or melee attack speeds the sun timer
    private void HandleNearbyAttack(Plant plant, Insect insect, DamageTag[] tags)
    {
        if (plant == this) return;
        if (!System.Array.Exists(tags, t => t == DamageTag.Attack)) return;
        if (!System.Array.Exists(tags, t => t == DamageTag.Projectile || t == DamageTag.Melee)) return;
        if (Vector3.Distance(transform.position, plant.transform.position) <= attackRange)
            passiveCooldownTimer -= _sunTimerReduction;
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) <= attackRange)
                plant.ApplyEffect(new BlessingOfTheSunEffect(plant, skillDuration, effectivePath3Level + 1, this, SunYieldBonus));
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
                if (plant == null || !plant.IsAlive) continue;
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

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (AData?.path1AttackDamagePerLevel ?? 4f)  * level;
        baseAttackSpeed  = data.baseAttackSpeed  + (AData?.path1AttackSpeedPerLevel  ?? 0.04f) * level;
        baseAttackRange  = data.baseAttackRange  + (AData?.path1AttackRangePerLevel  ?? 0.2f) * level;
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override string GetName() => $"<b><color=green>{(data != null ? data.displayName : "Aeonium")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a radiant succulent that fuels the garden's economy — generating sun, and blessing nearby plants to reap far more of it.";

    public override string GetAttackDescription() =>
        $"Launches an energy ball dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the first insect hit.";

    public override string GetPassiveDescription() =>
        $"Generates <color=yellow><b>{_sunGenerated}</b></color> <color=yellow>Sun</color> every <color=green><b>{passiveCooldown:F0}s</b></color>. Each projectile or melee attack hit by a plant within her radius reduces the timer by <color=green><b>{_sunTimerReductionBase:F1}s</b></color> [<color=#FFB6C1><b>+{_sunTimerReductionMP:F2}s</b></color>].";

    public override string GetSkillDesription() =>
        $"Grants all plants within range <color=green><b>Blessing of the Sun</b></color> for <color=green><b>{skillDuration:F0}s</b></color>, increasing their Sun yield — both generation and the Sun their kills drop — by <color=green><b>{SunYieldBonus * 100f:F0}%</b></color>.";

    public override string GetPath1Description()
    {
        float adpl   = AData?.path1AttackDamagePerLevel ?? 4f;
        float aspl   = AData?.path1AttackSpeedPerLevel  ?? 0.04f;
        float rngpl  = AData?.path1AttackRangePerLevel  ?? 0.2f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase Attack Speed by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase Attack Range by <color=green><b>{rngpl:F2}</b></color> per level. [<color=green><b>+{rngpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        int   sunpl      = AData?.path2SunPerLevel               ?? 1;
        float sunTimerpl = AData?.path2SunTimerReductionPerLevel ?? 0.1f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Increase Sun Generated by <color=green><b>{sunpl}</b></color> per level. [<color=green><b>+{sunpl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase On-Hit Timer Reduction by <color=green><b>{sunTimerpl:F1}s</b></color> per level. [<color=green><b>+{sunTimerpl * effectivePath2Level:F1}s</b></color>]\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(AData?.sunTimerMPMultiplier ?? 0.003f) * 100f:F1}%</b></color> Magic Power\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float yieldpl = AData?.path3SunYieldPerLevel ?? 0.05f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Increase Sun yield bonus by <color=green><b>{yieldpl * 100f:F0}%</b></color> per level. [<color=green><b>+{yieldpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
