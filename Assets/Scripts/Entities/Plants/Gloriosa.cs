using UnityEngine;
using System.Collections.Generic;

public class Gloriosa : Plant
{
    private GloriosaData GData => data as GloriosaData;

    [SerializeField] private GameObject emberPrefab;
    [SerializeField] private GameObject wispPrefab;

    private IAttackable _currentTarget;
    private readonly List<FieryWisp> _activeWisps = new List<FieryWisp>();
    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();
    private static readonly Color HighlightColor = new Color(1f, 0.5f, 0f);

    private float HealAmountBase       => (GData?.healAmount        ?? 25f) + (GData?.path2HealPerLevel        ?? 5f)  * effectivePath2Level;
    private float HealAmountMP         => (GData?.healMP            ?? 0.3f)  * magicPower;
    public  float healAmount           => HealAmountBase + HealAmountMP;

    private float TemperatureAmountBase => (GData?.temperatureAmount ?? 2f)  + (GData?.path2TemperaturePerLevel ?? 0.3f) * effectivePath2Level;
    private float TemperatureAmountMP   => (GData?.temperatureMP     ?? 0.02f) * magicPower;
    public  float temperatureAmount     => TemperatureAmountBase + TemperatureAmountMP;

    private float WispHealBase  => (GData?.wispHealPerSecond  ?? 4f) + (GData?.path3HealPerSecondPerLevel  ?? 1f) * effectivePath3Level;
    private float WispHealMP    => (GData?.wispHealMP         ?? 0.05f) * magicPower;
    private float WispHeal      => WispHealBase + WispHealMP;

    private float LatchHealBase => (GData?.latchHealPerSecond ?? 8f) + (GData?.path3LatchHealPerSecondPerLevel ?? 2f) * effectivePath3Level;
    private float LatchHealMP   => (GData?.latchHealMP        ?? 0.1f) * magicPower;
    private float LatchHeal     => LatchHealBase + LatchHealMP;

    private float LatchFireBase => (GData?.latchFireDamageBonus ?? 0.2f) + (GData?.path3LatchFireDamageBonusPerLevel ?? 0.05f) * effectivePath3Level;
    private float LatchFireMP   => (GData?.latchFireDamageBonusMP ?? 0.05f) * magicPower / 100f;
    private float LatchFire     => LatchFireBase + LatchFireMP;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override bool GetAttackBarVisible() => attackCooldown > 0f;

    public override void UpdateStats()
    {
        baseSkillDuration = (GData?.wispDuration ?? 20f) + (GData?.path3DurationPerLevel ?? 3f) * effectivePath3Level;
        base.UpdateStats();
    }

    protected override void Update()
    {
        base.Update();
        UpdateHighlights();
        attackCooldown = 1f / Mathf.Max(attackSpeed, 0.01f);

        if (IsStunned || IsChanneling) return;

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else
        {
            _currentTarget = FindCurrentTarget();
            if (_currentTarget != null)
            {
                attackCooldownTimer = 0f;
                SpawnEmber();
            }
        }
    }

    // public so EmberProjectile can retarget when its current target dies
    public IAttackable FindCurrentTarget()
    {
        // priority 1: most injured plant in range (lowest hp fraction)
        Plant bestPlant = null;
        float bestFrac  = 1f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == this || plant == null || !plant.IsAlive) continue;
            if (plant.health >= plant.maxHealth) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;
            float frac = plant.health / plant.maxHealth;
            if (frac < bestFrac) { bestFrac = frac; bestPlant = plant; }
        }
        if (bestPlant != null) return bestPlant;

        // priority 2: nearest insect in range
        Insect bestInsect = null;
        float  nearest    = Mathf.Infinity;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, insect.GetApproachPoint(transform.position));
            if (dist <= attackRange && dist < nearest) { nearest = dist; bestInsect = insect; }
        }
        return bestInsect;
    }

    private void SpawnEmber()
    {
        if (emberPrefab == null || _currentTarget == null) return;
        GameObject obj       = Instantiate(emberPrefab, transform.position, Quaternion.identity);
        EmberProjectile ember = obj.GetComponent<EmberProjectile>();
        ember.Initialize(this, _currentTarget,
            data?.baseProjectileSpeed ?? 5f,
            healAmount,
            temperatureAmount,
            GData?.auraRadius    ?? 1.5f,
            attackDamage,
            damageType, elementalType);
    }

    public override void ActivateSkill()
    {
        skillCooldownTimer = skillCooldown;
        foreach (FieryWisp w in new List<FieryWisp>(_activeWisps))
            w.Despawn();
        _activeWisps.Clear();
        int count = GData?.wispCount ?? 2;
        for (int i = 0; i < count; i++)
            SpawnWisp();
    }

    public void UnregisterWisp(FieryWisp wisp) => _activeWisps.Remove(wisp);

    private void SpawnWisp()
    {
        if (wispPrefab == null) return;
        float effectiveTempPerSec = (GData?.wispTemperaturePerSecond ?? 1f)  + (GData?.path3TemperaturePerSecondPerLevel ?? 0.2f) * effectivePath3Level;
        float effectiveLatchDur   = (GData?.latchDuration ?? 3f) + (GData?.path3LatchDurationPerLevel ?? 0.5f) * effectivePath3Level;

        Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
        GameObject obj = Instantiate(wispPrefab, transform.position + offset, Quaternion.identity);
        FieryWisp wisp = obj.GetComponent<FieryWisp>();
        _activeWisps.Add(wisp);
        wisp.Initialize(this,
            skillDuration,
            GData?.wispSpeed        ?? 3f,
            GData?.wispRadius       ?? 1.5f,
            WispHeal,
            effectiveTempPerSec,
            LatchHeal,
            LatchFire,
            effectiveLatchDur,
            GData?.wispLightRadius    ?? 1.5f,
            GData?.wispLightIntensity ?? 0.6f,
            GData?.wispEmergeSpeed    ?? 3f,
            GData?.wispSeekDelay      ?? 1f,
            GData?.wispTickInterval   ?? 1f);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + level * (GData?.path1AttackSpeedPerLevel ?? 0.1f);
        baseAttackRange = data.baseAttackRange + level * (GData?.path1AttackRangePerLevel ?? 0.15f);
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
            p.SetHighlight(HighlightColor);

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

    public override string GetName() => "<b><color=orange>Gloriosa</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a fiery flower that nurtures its allies with warm embers, healing injured plants and scorching any insect that dares stand in its way.";

    public override string GetAttackDescription() =>
        $"Sends embers of soothing fire towards its target, dealing <color=green><b>{attackDamage:F0}</b></color> " +
        $"{PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        $"Targets injured plants before insects. Embers heal plants for " +
        $"<color=green><b>{HealAmountBase:F0}</b></color> [<color=#FFB6C1><b>+{HealAmountMP:F0}</b></color>] health " +
        $"and increase temperature by <color=orange><b>{TemperatureAmountBase:F1}</b></color> " +
        $"[<color=#FFB6C1><b>+{TemperatureAmountMP:F1}</b></color>] per second until comfort. " +
        $"Plants within <color=green><b>{GData?.auraRadius ?? 1.5f:F1}</b></color> radius receive half the effect.";

    public override string GetSkillDesription()
    {
        int   count = GData?.wispCount ?? 2;
        float wRad  = GData?.wispRadius ?? 1.5f;
        float wTemp = (GData?.wispTemperaturePerSecond ?? 1f) + (GData?.path3TemperaturePerSecondPerLevel ?? 0.2f) * effectivePath3Level;
        float lDur  = (GData?.latchDuration ?? 3f) + (GData?.path3LatchDurationPerLevel ?? 0.5f) * effectivePath3Level;
        return $"Summons <color=green><b>{count}</b></color> Fiery Wisps that fly across the map seeking the most injured plant. " +
               $"Plants within <color=green><b>{wRad:F1}</b></color> radius heal " +
               $"<color=orange><b>{WispHealBase:F0}</b></color> [<color=#FFB6C1><b>+{WispHealMP:F0}</b></color>] health and " +
               $"warm <color=orange><b>{wTemp:F1}°</b></color> per second while the wisp is near. " +
               $"When a wisp reaches its target it latches, applying <color=orange>Fiery Wisp Latched</color>: " +
               $"heals <color=orange><b>{LatchHealBase:F0}</b></color> [<color=#FFB6C1><b>+{LatchHealMP:F0}</b></color>] per second and " +
               $"increases <color=orange>Fire</color> damage by " +
               $"<color=orange><b>{LatchFireBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{LatchFireMP * 100f:F1}%</b></color>]. " +
               $"The effect lingers for <color=orange><b>{lDur:F1}s</b></color> after the wisp detaches. " +
               $"Wisps last <color=green><b>{skillDuration:F0}s</b></color>.";
    }

    public override string GetPath1Description()
    {
        float speedpl = GData?.path1AttackSpeedPerLevel ?? 0.1f;
        float rangepl = GData?.path1AttackRangePerLevel ?? 0.15f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Speed by <color=green><b>{speedpl:F2}</b></color> per level. [<color=green><b>+{speedpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase Attack Range by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        float healpl = GData?.path2HealPerLevel        ?? 5f;
        float temppl = GData?.path2TemperaturePerLevel ?? 0.3f;
        float healMP = GData?.healMP                   ?? 0.3f;
        float tempMP = GData?.temperatureMP            ?? 0.02f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>{healMP * 100f:F0}%</b></color> Magic Power (Healing), <color=#FFB6C1><b>{tempMP * 100f:F0}%</b></color> Magic Power (Temperature)\n\n" +
               $"Increase base healing by <color=green><b>{healpl:F0}</b></color> per level. [<color=green><b>+{healpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase temperature regulation by <color=green><b>{temppl:F1}°</b></color> per level. [<color=green><b>+{temppl * effectivePath2Level:F1}°</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float durpl   = GData?.path3DurationPerLevel             ?? 3f;
        float healpl  = GData?.path3HealPerSecondPerLevel        ?? 1f;
        float temppl  = GData?.path3TemperaturePerSecondPerLevel ?? 0.2f;
        float lhealpl = GData?.path3LatchHealPerSecondPerLevel   ?? 2f;
        float lfirepl = GData?.path3LatchFireDamageBonusPerLevel ?? 0.05f;
        float ldurpl  = GData?.path3LatchDurationPerLevel        ?? 0.5f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(GData?.wispHealMP ?? 0.05f) * 100f:F0}%</b></color> Magic Power (Wisp Heal), " +
               $"<color=#FFB6C1><b>{(GData?.latchHealMP ?? 0.1f) * 100f:F0}%</b></color> Magic Power (Latch Heal), " +
               $"<color=#FFB6C1><b>{(GData?.latchFireDamageBonusMP ?? 0.05f) * 100f:F0}%</b></color> Magic Power (Fire Bonus)\n\n" +
               $"Increase wisp duration by <color=green><b>{durpl:F0}s</b></color> per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"Increase aura healing by <color=green><b>{healpl:F0}</b></color> per second per level. [<color=green><b>+{healpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase aura temperature by <color=orange><b>{temppl:F1}°</b></color> per second per level. [<color=orange><b>+{temppl * effectivePath3Level:F1}°</b></color>]\n\n" +
               $"Increase latch healing by <color=green><b>{lhealpl:F0}</b></color> per second per level. [<color=green><b>+{lhealpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase latch <color=orange>Fire</color> bonus by <color=orange><b>{lfirepl * 100f:F0}%</b></color> per level. [<color=orange><b>+{lfirepl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase latch duration by <color=green><b>{ldurpl:F1}s</b></color> per level. [<color=green><b>+{ldurpl * effectivePath3Level:F1}s</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
