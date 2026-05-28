using UnityEngine;
using System.Collections.Generic;

public class Aeonium : Aura
{
    private AeoniumData AData => data as AeoniumData;

    [SerializeField] private Transform skillRangeIndicator;
    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();
    private int _sunGenerated;
    private float _sunTimerReductionBase;
    private float _sunTimerReductionMP;
    private float _sunTimerReduction => _sunTimerReductionBase + _sunTimerReductionMP;
    private float _healAmountBase;
    private float _healAmountMP;
    private float _healAmount => _healAmountBase + _healAmountMP;
    private float _cdrReduction;

    private float _skillRangeBonus;
    private float _skillSpeedBonusBase;
    private float _skillSpeedBonusMP;
    private float _skillSpeedBonus => _skillSpeedBonusBase + _skillSpeedBonusMP;
    private int _bonusSunPerKill;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        passiveCooldownTimer = data.basePassiveCooldown;
    }

    private void OnEnable()
    {
        Entity.OnPlantAttackHit += HandleNearbyAttack;
        Insect.OnInsectDied    += HandleInsectDied;
    }

    private void OnDisable()
    {
        Entity.OnPlantAttackHit -= HandleNearbyAttack;
        Insect.OnInsectDied    -= HandleInsectDied;
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        int sunpl    = AData?.path2SunPerLevel                ?? 1;
        float sunTimerpl = AData?.path2SunTimerReductionPerLevel ?? 0.1f;
        float healpl     = AData?.path1HealPerLevel              ?? 4f;
        float cdrpl      = AData?.path1CDRPerLevel               ?? 0.2f;
        int bonusSunBase = AData?.path3BonusSunBase              ?? 4;
        int bonusSunpl   = AData?.path3BonusSunPerLevel          ?? 2;

        _sunGenerated          = (AData?.baseSunGenerated ?? 3) + sunpl * effectivePath2Level;
        _sunTimerReductionBase = (AData?.baseSunTimerReduction ?? 0.3f) + sunTimerpl * effectivePath2Level;
        _sunTimerReductionMP   = (AData?.sunTimerMPMultiplier ?? 0.003f) * magicPower;
        _healAmountBase        = (AData?.baseHealAmount ?? 8f) + healpl * effectivePath1Level;
        _healAmountMP          = (AData?.healMPMultiplier ?? 0.24f) * magicPower;
        _cdrReduction          = (AData?.baseSkillCooldownReduction ?? 1f) + cdrpl * effectivePath1Level;
        _skillRangeBonus       = (AData?.baseSkillRangeBonus ?? 0.15f) + (AData?.skillRangeBonusPerLevel ?? 0.05f) * effectivePath3Level;
        _skillSpeedBonusBase   = (AData?.baseSkillSpeedBonus ?? 0.30f) + (AData?.skillSpeedBonusPerLevel ?? 0.10f) * effectivePath3Level;
        _skillSpeedBonusMP     = (AData?.skillSpeedMPMultiplier ?? 0.80f) * magicPower / 100f;
        _bonusSunPerKill       = bonusSunBase + bonusSunpl * effectivePath3Level;
    }

    protected override void Update()
    {
        base.Update();
        UpdateHighlights();

        if (passiveCooldownTimer <= 0)
        {
            GameManager.instance.AddSun(_sunGenerated);
            passiveCooldownTimer += passiveCooldown;
            SunIndicator.Spawn(transform.position + new Vector3(0.25f, 0.5f, 0f), _sunGenerated);
        }

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else
            Attack();

        if (skillRangeIndicator != null)
            skillRangeIndicator.localScale = new Vector3(baseAttackRange * 2f, baseAttackRange * 2f, 1f);
    }

    protected override void Attack()
    {
        base.Attack();

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;

            float amount = plant == this ? _healAmount * 0.5f : _healAmount;
            plant.Heal(amount, this);

            if (plant != this)
                plant.skillCooldownTimer = Mathf.Max(0f, plant.skillCooldownTimer - _cdrReduction);
        }
    }

    private void HandleNearbyAttack(Plant plant, DamageTag[] tags)
    {
        if (plant == this) return;
        if (!System.Array.Exists(tags, t => t == DamageTag.Attack)) return;
        if (!System.Array.Exists(tags, t => t == DamageTag.Projectile)) return;
        if (Vector3.Distance(transform.position, plant.transform.position) <= attackRange)
            passiveCooldownTimer -= _sunTimerReduction;
    }

    private void HandleInsectDied(Insect insect)
    {
        if (!HasEffect<AeoniumBloomEffect>()) return;
        if (Vector3.Distance(transform.position, insect.transform.position) > attackRange) return;

        SunIndicator.SpawnBonus(insect.transform.position + new Vector3(0f, 0.75f, 0f), insect.sunDrop);
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

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;

        ApplyEffect(new AeoniumBloomEffect(this, skillDuration, effectivePath3Level, this, _skillRangeBonus, _skillSpeedBonus, skillRangeIndicator));

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= attackRange)
                insect.ApplyEffect(new AeoniumBlessedEffect(insect, skillDuration, effectivePath3Level, this, _bonusSunPerKill));
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackRange = data.baseAttackRange + (AData?.path1AttackRangePerLevel ?? 0.2f) * level;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration;
    }

    public override string GetName() => $"<b><color=green>{(data != null ? data.displayName : "Aeonium")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a radiant succulent that nurtures the garden. She heals and empowers nearby plants, and blesses the ground to yield extra sun from fallen insects.";

    public override string GetAttackDescription() =>
        $"Every <color=green><b>{attackCooldown:F1}s</b></color>, pulses restorative energy to all plants within range, healing them for <color=green><b>{_healAmountBase:F0}</b></color> [<color=#FFB6C1><b>+{_healAmountMP:F0}</b></color>] health (<color=green><b>{_healAmount * 0.5f:F0}</b></color> to herself), and reducing their Skill cooldown by <color=green><b>{_cdrReduction:F1}s</b></color>.";

    public override string GetPassiveDescription() =>
        $"Generates <color=yellow><b>{_sunGenerated}</b></color> <color=yellow>Sun</color> every <color=green><b>{passiveCooldown:F0}s</b></color>. Each projectile attack hit by a plant within her radius reduces the timer by <color=green><b>{_sunTimerReductionBase:F1}s</b></color> [<color=#FFB6C1><b>+{_sunTimerReductionMP:F2}s</b></color>].";

    public override string GetSkillDesription() =>
        $"The Aeonium blesses the ground around her, growing flowers that empower her presence. Increases her own Attack Range by <color=green><b>{(_skillRangeBonus * 100f):F0}%</b></color> and Attack Speed by <color=green><b>{(_skillSpeedBonusBase * 100f):F0}%</b></color> [<color=#FFB6C1><b>+{(_skillSpeedBonusMP * 100f):F0}%</b></color>] for <color=green><b>{skillDuration:F0}s</b></color>. Insects that die within her range during this time yield <color=yellow><b>+{_bonusSunPerKill}</b></color> bonus <color=yellow>Sun</color>.";

    public override string GetPath1Description()
    {
        float rangepl = AData?.path1AttackRangePerLevel ?? 0.2f;
        float healpl  = AData?.path1HealPerLevel        ?? 4f;
        float cdrpl   = AData?.path1CDRPerLevel         ?? 0.2f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Range by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Increase Heal by <color=green><b>{healpl:F0}</b></color> per level. [<color=green><b>+{healpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(AData?.healMPMultiplier ?? 0.24f) * 100f:F0}%</b></color> Magic Power\n\n" +
               $"Increase Skill Cooldown Reduction by <color=green><b>{cdrpl:F1}s</b></color> per level. [<color=green><b>+{cdrpl * effectivePath1Level:F1}s</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        int sunpl        = AData?.path2SunPerLevel                ?? 1;
        float sunTimerpl = AData?.path2SunTimerReductionPerLevel  ?? 0.1f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Increase Sun Generated by <color=green><b>{sunpl}</b></color> per level. [<color=green><b>+{sunpl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase On-Hit Timer Reduction by <color=green><b>{sunTimerpl:F1}s</b></color> per level. [<color=green><b>+{sunTimerpl * effectivePath2Level:F1}s</b></color>]\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(AData?.sunTimerMPMultiplier ?? 0.003f) * 100f:F1}%</b></color> Magic Power\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        int bonusSunBase = AData?.path3BonusSunBase    ?? 4;
        int bonusSunpl   = AData?.path3BonusSunPerLevel ?? 2;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Range Bonus: <color=green><b>{(AData?.baseSkillRangeBonus ?? 0.15f) * 100f:F0}% + {(AData?.skillRangeBonusPerLevel ?? 0.05f) * 100f:F0}%</b></color> per level. [<color=green><b>+{(AData?.skillRangeBonusPerLevel ?? 0.05f) * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Speed Bonus: <color=green><b>{(AData?.baseSkillSpeedBonus ?? 0.30f) * 100f:F0}% + {(AData?.skillSpeedBonusPerLevel ?? 0.10f) * 100f:F0}%</b></color> per level. [<color=green><b>+{(AData?.skillSpeedBonusPerLevel ?? 0.10f) * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(AData?.skillSpeedMPMultiplier ?? 0.80f) * 100f:F0}%</b></color> Magic Power\n\n" +
               $"Bonus Sun per Kill: <color=green><b>{bonusSunBase} + {bonusSunpl}</b></color> per level. [<color=yellow><b>+{_bonusSunPerKill}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
