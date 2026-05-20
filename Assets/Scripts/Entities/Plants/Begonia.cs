using UnityEngine;
using System.Collections.Generic;

public class Begonia : Shooter
{
    private float auraTickTimer = 0f;
    private const float auraTickInterval = 0.25f;
    private const float auraEffectDuration = 0.35f;

    private bool _isSkillTargeting = false;
    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();

    private BegoniaData BData => data as BegoniaData;

    private float ElementalPowerBonusBase => (BData?.baseElementalPowerBonus ?? 0f) + 0.06f * effectivePath2Level;
    private float ElementalPowerBonusMP   => (BData?.basePassiveMultiplier ?? 0f) * magicPower / 100f;
    private float ElementalPowerBonus     => ElementalPowerBonusBase + ElementalPowerBonusMP;

    private float NatureDamageBonusBase => (BData?.baseNatureDamageBonus ?? 0f) + 0.04f * effectivePath3Level;
    private float NatureDamageBonusMP   => (BData?.baseSkillMultiplier ?? 0f) * magicPower / 100f;
    private float NatureDamageBonus     => NatureDamageBonusBase + NatureDamageBonusMP;

    private float AttackSpeedBonusBase => (BData?.baseAttackSpeedBonus ?? 0f) + 0.04f * effectivePath3Level;
    private float AttackSpeedBonusMP   => (BData?.baseSkillMultiplier ?? 0f) * magicPower / 100f;
    private float AttackSpeedBonus     => AttackSpeedBonusBase + AttackSpeedBonusMP;
    private float BlossomRadius       => baseSkillRadius + 0.15f * effectivePath3Level;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();
        UpdateAura();
        UpdateHighlights();
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
            ElementalPowerBoostEffect existing = plant.GetEffect<ElementalPowerBoostEffect>();
            if (existing != null && existing.bonus > ElementalPowerBonus) continue;
            plant.ApplyEffect(new ElementalPowerBoostEffect(plant, auraEffectDuration, 1, this, ElementalPowerBonus));
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
                plant.ApplyEffect(new BlossomingEffect(plant, skillDuration, effectivePath3Level + 1, this, NatureDamageBonus, AttackSpeedBonus));
        }
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
        baseAttackDamage = data.baseAttackDamage + 4f * level;
        baseAttackRange  = data.baseAttackRange  + 0.2f * level;
    }

    public override void OnPath2Upgrade(int level) { }

    public override void OnPath3Upgrade(int level) { }

    public override string GetName() => "<b><color=green>Begonia</color></b>";
    public override string GetDescription() =>
        $"The {GetName()} infuses nearby allies with elemental power and can bless them with the power of nature.";

    public override string GetPath1Description() =>
        $"Attack:\n\n" +
        $"Fire a magical bolt dealing <color=green><b>{attackDamage:F0}</b></color> <color=green>Nature</color> <color=#FFB6C1>Magic</color> damage.\n\n" +
        $"Increase Attack Damage by <color=green><b>4</b></color> per level. [<color=green><b>+{4 * effectivePath1Level}</b></color>]\n\n" +
        $"Increase Attack Range by <color=green><b>0.2</b></color> per level. [<color=green><b>+{0.2f * effectivePath1Level:F1}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description() =>
        $"Passive:\n\n" +
        $"Plants within her attack radius are granted <color=green><b>Begonia's Blessing</b></color>, increasing Elemental Power by <color=green><b>{ElementalPowerBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{ElementalPowerBonusMP * 100f:F0}%</b></color>].\n\n" +
        $"Scaling: <color=#FFB6C1><b>{(BData?.basePassiveMultiplier ?? 0f) * 100f:F0}%</b></color> Magic Power\n\n" +
        $"Increase Elemental Power bonus by <color=green><b>6%</b></color> per level. [<color=green><b>+{6 * effectivePath2Level}%</b></color>]\n\n" +
        $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description() =>
        $"Skill:\n\n" +
        $"Target an area on the field (radius <color=green><b>{BlossomRadius:F2}</b></color>). Plants within are granted <color=green><b>Blossoming</b></color> for <color=green><b>{skillDuration:F0}s</b></color>, " +
        $"increasing Nature Power by <color=green><b>{NatureDamageBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{NatureDamageBonusMP * 100f:F0}%</b></color>] " +
        $"and Attack Speed by <color=green><b>{AttackSpeedBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{AttackSpeedBonusMP * 100f:F0}%</b></color>].\n\n" +
        $"Scaling: <color=#FFB6C1><b>{(BData?.baseSkillMultiplier ?? 0f) * 100f:F0}%</b></color> Magic Power\n\n" +
        $"Increase Nature Power bonus by <color=green><b>4%</b></color> per level. [<color=green><b>+{4 * effectivePath3Level}%</b></color>]\n\n" +
        $"Increase Attack Speed bonus by <color=green><b>4%</b></color> per level. [<color=green><b>+{4 * effectivePath3Level}%</b></color>]\n\n" +
        $"Increase radius by <color=green><b>0.15</b></color> per level. [<color=green><b>+{0.15f * effectivePath3Level:F2}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";

    public override string GetAttackDescription() =>
        $"Fire a magical bolt dealing <color=green><b>{attackDamage:F0}</b></color> <color=green>Nature</color> <color=#FFB6C1>Magic</color> damage.";

    public override string GetPassiveDescription() =>
        $"Plants within her attack radius are granted <color=green><b>Begonia's Blessing</b></color>, " +
        $"increasing Elemental Power by <color=green><b>{ElementalPowerBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{ElementalPowerBonusMP * 100f:F0}%</b></color>].";

    public override string GetSkillDesription() =>
        $"Target an area on the field. Plants within are granted <color=green><b>Blossoming</b></color> for <color=green><b>{skillDuration:F0}s</b></color>, " +
        $"increasing Nature Power by <color=green><b>{NatureDamageBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{NatureDamageBonusMP * 100f:F0}%</b></color>] " +
        $"and Attack Speed by <color=green><b>{AttackSpeedBonusBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{AttackSpeedBonusMP * 100f:F0}%</b></color>].";
}
