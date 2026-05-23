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
    }

    private void OnDisable()
    {
        Entity.OnPlantAttackHit -= HandleNearbyAttack;
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        _sunGenerated          = (AData?.baseSunGenerated ?? 3) + effectivePath2Level;
        _sunTimerReductionBase = (AData?.baseSunTimerReduction ?? 0.3f) + 0.1f * effectivePath2Level;
        _sunTimerReductionMP   = (AData?.sunTimerMPMultiplier ?? 0.003f) * magicPower;
        _healAmountBase        = (AData?.baseHealAmount ?? 8f) + 4f * effectivePath1Level;
        _healAmountMP          = (AData?.healMPMultiplier ?? 0.24f) * magicPower;
        _cdrReduction          = (AData?.baseSkillCooldownReduction ?? 1f) + 0.2f * effectivePath1Level;
        _skillRangeBonus       = (AData?.baseSkillRangeBonus ?? 0.15f) + (AData?.skillRangeBonusPerLevel ?? 0.05f) * effectivePath3Level;
        _skillSpeedBonusBase   = (AData?.baseSkillSpeedBonus ?? 0.30f) + (AData?.skillSpeedBonusPerLevel ?? 0.10f) * effectivePath3Level;
        _skillSpeedBonusMP     = (AData?.skillSpeedMPMultiplier ?? 0.80f) * magicPower / 100f;
        _bonusSunPerKill       = 4 + 2 * effectivePath3Level;
    }

    protected override void Update()
    {
        base.Update();
        UpdateHighlights();

        // Sun generation — passiveCooldownTimer is counted down by Plant.Update()
        if (passiveCooldownTimer <= 0)
        {
            GameManager.instance.AddSun(_sunGenerated);
            passiveCooldownTimer = passiveCooldown;
            GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), transform.position + new Vector3(0.25f, 0.5f, 0f), Quaternion.identity);
            indicator.GetComponent<DamageIndicator>().Initialize($"+{_sunGenerated} Sun", new Color(1f, 1f, 0f));
        }

        // Heal + CDR aura pulse
        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else
            Attack();

        // Keep skill indicator scaled to current attack range
        if (skillRangeIndicator != null)
            skillRangeIndicator.localScale = new Vector3(attackRange * 2f, attackRange * 2f, 1f);
    }

    protected override void Attack()
    {
        base.Attack(); // resets attackCooldownTimer = 0

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

    private void HandleNearbyAttack(Plant plant)
    {
        if (plant == this) return;
        if (Vector3.Distance(transform.position, plant.transform.position) <= attackRange)
            passiveCooldownTimer = Mathf.Max(0f, passiveCooldownTimer - _sunTimerReduction);
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
        baseAttackRange = data.baseAttackRange + 0.2f * level;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration;
    }

    public override string GetName() => "<b><color=green>Aeonium Sunburst</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a radiant succulent that nurtures the garden. She heals and empowers nearby plants, and blesses the ground to yield extra sun from fallen insects.";

    public override string GetAttackDescription() =>
        $"Every <color=green><b>{attackCooldown:F1}s</b></color>, pulses restorative energy to all plants within range, healing them for <color=green><b>{_healAmountBase:F0}</b></color> [<color=#FFB6C1><b>+{_healAmountMP:F0}</b></color>] health (<color=green><b>{_healAmount * 0.5f:F0}</b></color> to herself), and reducing their Skill cooldown by <color=green><b>{_cdrReduction:F1}s</b></color>.";

    public override string GetPassiveDescription() =>
        $"Generates <color=yellow><b>{_sunGenerated}</b></color> <color=yellow>Sun</color> every <color=green><b>{passiveCooldown:F0}s</b></color>. Each Attack-tagged hit by a plant within range reduces the timer by <color=green><b>{_sunTimerReductionBase:F1}s</b></color> [<color=#FFB6C1><b>+{_sunTimerReductionMP:F2}s</b></color>].";

    public override string GetSkillDesription() =>
        $"The Aeonium blesses the ground around her, growing flowers that empower her presence. Increases her own Attack Range by <color=green><b>{(_skillRangeBonus * 100f):F0}%</b></color> and Attack Speed by <color=green><b>{(_skillSpeedBonusBase * 100f):F0}%</b></color> [<color=#FFB6C1><b>+{(_skillSpeedBonusMP * 100f):F0}%</b></color>] for <color=green><b>{skillDuration:F0}s</b></color>. Insects that die within her range during this time yield <color=yellow><b>+{_bonusSunPerKill}</b></color> bonus <color=yellow>Sun</color>.";

    public override string GetPath1Description() =>
        $"Attack:\n\n{GetAttackDescription()}\n\n" +
        $"Increase Attack Range by <color=green><b>0.2</b></color> per level. [<color=green><b>+{0.2f * effectivePath1Level:F1}</b></color>]\n\n" +
        $"Increase Heal by <color=green><b>4</b></color> per level. [<color=green><b>+{4 * effectivePath1Level}</b></color>]\n\n" +
        $"Scaling: <color=#FFB6C1><b>{(AData?.healMPMultiplier ?? 0.24f) * 100f:F0}%</b></color> Magic Power\n\n" +
        $"Increase Skill Cooldown Reduction by <color=green><b>0.2s</b></color> per level. [<color=green><b>+{0.2f * effectivePath1Level:F1}s</b></color>]\n\n" +
        $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description() =>
        $"Passive:\n\n{GetPassiveDescription()}\n\n" +
        $"Increase Sun Generated by <color=green><b>1</b></color> per level. [<color=green><b>+{effectivePath2Level}</b></color>]\n\n" +
        $"Increase On-Hit Timer Reduction by <color=green><b>0.1s</b></color> per level. [<color=green><b>+{0.1f * effectivePath2Level:F1}s</b></color>]\n\n" +
        $"Scaling: <color=#FFB6C1><b>{(AData?.sunTimerMPMultiplier ?? 0.003f) * 100f:F1}%</b></color> Magic Power\n\n" +
        $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description() =>
        $"Skill:\n\n{GetSkillDesription()}\n\n" +
        $"Range Bonus: <color=green><b>15% + 5%</b></color> per level. [<color=green><b>+{5 * effectivePath3Level:F0}%</b></color>]\n\n" +
        $"Speed Bonus: <color=green><b>30% + 10%</b></color> per level. [<color=green><b>+{10 * effectivePath3Level:F0}%</b></color>]\n\n" +
        $"Scaling: <color=#FFB6C1><b>{(AData?.skillSpeedMPMultiplier ?? 0.80f) * 100f:F0}%</b></color> Magic Power\n\n" +
        $"Bonus Sun per Kill: <color=green><b>4 + 2</b></color> per level. [<color=yellow><b>+{_bonusSunPerKill}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
