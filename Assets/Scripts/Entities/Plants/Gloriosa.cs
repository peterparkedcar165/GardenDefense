using UnityEngine;
using System.Collections.Generic;

public class Gloriosa : Shooter
{
    private GloriosaData GData => data as GloriosaData;

    [SerializeField] private GameObject emberPrefab;
    [SerializeField] private GameObject wispPrefab;

    private IAttackable _currentTarget;
    private readonly List<Cinderwisp> _activeWisps = new List<Cinderwisp>();
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

    // no targeting needed: the skill just summons wisps wherever Gloriosa stands
    private bool autoCastEnabled = false;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

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
        if (IsPath1Maxed)
            piercing += 1;
    }

    protected override void Update()
    {
        base.Update();
        UpdateHighlights();

        if (autoCastEnabled && SkillReady)
            ActivateSkill();
    }

    // click Auto Cast to toggle it on, click again to turn it off — no target to pick
    public override void ToggleAutoCast() => autoCastEnabled = !autoCastEnabled;

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled) return;
        autoCastEnabled = true;
    }

    protected override GameObject FindTarget()
    {
        _currentTarget = FindCurrentTarget();
        return _currentTarget != null ? ((Entity)_currentTarget).gameObject : null;
    }

    protected override void Shoot(Vector3 target) => SpawnEmber();

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

        // priority 2: most injured friendly minion in range (lowest hp fraction)
        Insect bestAlly = null;
        float  bestAllyFrac = 1f;
        foreach (Insect ally in Insect.friendlyInsects)
        {
            if (ally == null || !ally.IsAlive) continue;
            if (ally.health >= ally.maxHealth) continue;
            if (Vector3.Distance(transform.position, ally.transform.position) > attackRange) continue;
            float frac = ally.health / ally.maxHealth;
            if (frac < bestAllyFrac) { bestAllyFrac = frac; bestAlly = ally; }
        }
        if (bestAlly != null) return bestAlly;

        // priority 3: nearest enemy insect in range
        Insect bestInsect = null;
        float  nearest    = Mathf.Infinity;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, insect.GetApproachPoint(transform.position));
            if (dist <= attackRange && dist < nearest && IsValidNightTarget(insect, dist)) { nearest = dist; bestInsect = insect; }
        }
        if (bestInsect != null) return bestInsect;

        // priority 4 (last resort): nothing hurt, nothing to fight — keep the coldest nearby
        // plant warm. only worth it in Cold weather, since that's the only time warming
        // actually does anything (Detonate() only adjusts temperature when the weather is
        // Cold), and only for plants still below comfort (10) — above that they're fine
        if (WeatherManager.instance?.temperature == TemperatureType.Cold)
        {
            Plant coldest = null;
            float lowestTemp = 10f;
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == this || plant == null || !plant.IsAlive) continue;
                if (plant.temperature >= 10f) continue;
                if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;
                if (plant.temperature < lowestTemp) { lowestTemp = plant.temperature; coldest = plant; }
            }
            if (coldest != null) return coldest;
        }

        return null;
    }

    private void SpawnEmber()
    {
        if (emberPrefab == null || _currentTarget == null) return;
        GameObject obj        = Instantiate(emberPrefab, transform.position, Quaternion.identity);
        EmberProjectile ember = obj.GetComponent<EmberProjectile>();
        ember.Initialize(this, _currentTarget,
            data?.baseProjectileSpeed ?? 5f,
            healAmount, temperatureAmount,
            GData?.auraRadius ?? 1.5f,
            attackDamage, damageType, elementalType);
        if (IsPath1Maxed) ember.SetBouncesRemaining(piercing);
    }

    public override void ActivateSkill()
    {
        skillCooldownTimer = skillCooldown;
        DespawnAllWisps();
        int count = GData?.wispCount ?? 2;
        if (IsPath3Maxed) count++;
        for (int i = 0; i < count; i++)
            SpawnWisp();
    }

    public void SpawnBounceEmber(Vector3 fromPos, IAttackable target, int bouncesRemaining, HashSet<IAttackable> hitHistory)
    {
        if (emberPrefab == null || target == null) return;
        GameObject obj    = Instantiate(emberPrefab, fromPos, Quaternion.identity);
        EmberProjectile e = obj.GetComponent<EmberProjectile>();
        if (e == null) return;
        e.Initialize(this, target,
            data?.baseProjectileSpeed ?? 5f,
            healAmount, temperatureAmount,
            GData?.auraRadius ?? 1.5f,
            attackDamage, damageType, elementalType);
        e.SetBouncesRemaining(bouncesRemaining);
        e.MarkAsBounce();
        e.SetHitHistory(hitHistory);
    }

    public void UnregisterWisp(Cinderwisp wisp) => _activeWisps.Remove(wisp);

    private void DespawnAllWisps()
    {
        foreach (Cinderwisp w in new List<Cinderwisp>(_activeWisps))
            w.Despawn();
        _activeWisps.Clear();
    }

    private void SpawnWisp()
    {
        if (wispPrefab == null) return;
        float effectiveTempPerSec = (GData?.wispTemperaturePerSecond ?? 1f)  + (GData?.path3TemperaturePerSecondPerLevel ?? 0.2f) * effectivePath3Level;
        float effectiveLatchDur   = ((GData?.latchDuration ?? 3f) + (GData?.path3LatchDurationPerLevel ?? 0.5f) * effectivePath3Level) * (1f + skillDurationMultiplier) + skillDurationAdder;

        Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
        GameObject obj = Instantiate(wispPrefab, transform.position + offset, Quaternion.identity);
        Cinderwisp wisp = obj.GetComponent<Cinderwisp>();
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

    // a dead gloriosa can't sustain its wisps: they must go with it. Kill() catches death
    // immediately (before any death animation delay); OnDestroy catches every other way the
    // gloriosa can leave the field (sold, scene unload, etc.)
    public override void Kill()
    {
        DespawnAllWisps();
        base.Kill();
    }

    public override void Kill(Entity source)
    {
        DespawnAllWisps();
        base.Kill(source);
    }

    protected override void OnDestroy()
    {
        DespawnAllWisps();
        base.OnDestroy();
        foreach (Plant p in _highlightedPlants)
            if (p != null) p.ClearHighlight();
    }

    public override string GetName() => "<b><color=orange>Gloriosa</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a fiery flower that nurtures its allies with warm embers, healing injured plants and scorching any insect that dares stand in its way.";

    public override string GetAttackDescription() =>
        $"Sends embers of soothing fire towards its target, dealing <color=green><b>{attackDamage:F0}</b></color> " +
        $"{PlantData.DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        $"Targets injured plants before insects. Embers heal plants for " +
        $"<color=green><b>{HealAmountBase:F0}</b></color> [<color=#FFB6C1><b>+{HealAmountMP:F0}</b></color>] health " +
        $"and increase temperature by <color=orange><b>{TemperatureAmountBase:F1}</b></color> " +
        $"[<color=#FFB6C1><b>+{TemperatureAmountMP:F1}</b></color>] until comfort. " +
        $"Plants within <color=green><b>{GData?.auraRadius ?? 1.5f:F1}</b></color> radius receive half the effect.";

    public override string GetSkillDesription()
    {
        int   count = GData?.wispCount ?? 2;
        float wRad  = GData?.wispRadius ?? 1.5f;
        float wTemp = (GData?.wispTemperaturePerSecond ?? 1f) + (GData?.path3TemperaturePerSecondPerLevel ?? 0.2f) * effectivePath3Level;
        float lDur  = ((GData?.latchDuration ?? 3f) + (GData?.path3LatchDurationPerLevel ?? 0.5f) * effectivePath3Level) * (1f + skillDurationMultiplier) + skillDurationAdder;
        return $"Summons <color=green><b>{count}</b></color> <color=orange><b>Cinderwisps</b></color> that seek injured plants, " +
               $"healing <color=green><b>{WispHealBase:F0}</b></color> [<color=#FFB6C1><b>+{WispHealMP:F0}</b></color>] health " +
               $"and heating <color=orange><b>{wTemp:F1}°</b></color> per second around them. " +
               $"Upon reaching a target, it latches, applying <color=orange><b>Boon of The Wisp</b></color>, " +
               $"which heals <color=green><b>{LatchHealBase:F0}</b></color> [<color=#FFB6C1><b>+{LatchHealMP:F0}</b></color>] per second " +
               $"and increases <color=orange><b>Fire Damage</b></color> by " +
               $"<color=orange><b>{LatchFireBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{LatchFireMP * 100f:F1}%</b></color>]. " +
               $"The effect lingers for <color=orange><b>{lDur:F1}s</b></color>.";
    }

    public override string GetPath1Description(bool details = false)
    {
        float speedpl = GData?.path1AttackSpeedPerLevel ?? 0.1f;
        float rangepl = GData?.path1AttackRangePerLevel ?? 0.15f;
        string desc = details
            ? $"Sends embers of soothing fire towards its target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{speedpl:F2}</b></color> per level. [<color=green><b>+{speedpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "When targeting a friendly unit, the ember bounces to nearby injured friendly units.\n\nIncrease <color=green><b>Piercing</b></color> by <color=green><b>1</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float healpl = GData?.path2HealPerLevel        ?? 5f;
        float temppl = GData?.path2TemperaturePerLevel ?? 0.3f;
        float healMP = GData?.healMP                   ?? 0.3f;
        float tempMP = GData?.temperatureMP            ?? 0.02f;
        string desc = details
            ? $"Targets injured plants before insects. Embers heal plants for <color=green><b>[({GData?.healAmount ?? 25f:F0}) + ({healpl:F0}/Lvl.) + <color=#FFB6C1>{healMP * 100f:F0}% Magic Power</color>]</b></color> health and increase temperature by <color=orange><b>[({GData?.temperatureAmount ?? 2f:F1}) + ({temppl:F1}/Lvl.) + <color=#FFB6C1>{tempMP * 100f:F0}% Magic Power</color>]</b></color> until comfort. Plants within <color=green><b>{GData?.auraRadius ?? 1.5f:F1}</b></color> radius receive half the effect."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Healing</b></color> by <color=green><b>{healpl:F0}</b></color> per level. [<color=green><b>+{healpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase temperature regulation by <color=green><b>{temppl:F1}</b></color> per level. [<color=green><b>+{temppl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Healing applies <color=orange><b>Heated Comfort</b></color>, regenerating the full amount of health and temperature over <color=green><b>4s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl   = GData?.path3DurationPerLevel             ?? 3f;
        float healpl  = GData?.path3HealPerSecondPerLevel        ?? 1f;
        float temppl  = GData?.path3TemperaturePerSecondPerLevel ?? 0.2f;
        float lhealpl = GData?.path3LatchHealPerSecondPerLevel   ?? 2f;
        float lfirepl = GData?.path3LatchFireDamageBonusPerLevel ?? 0.05f;
        float ldurpl  = GData?.path3LatchDurationPerLevel        ?? 0.5f;
        float wispHealMP  = GData?.wispHealMP  ?? 0.05f;
        float latchHealMP = GData?.latchHealMP ?? 0.1f;
        float latchFireMP = GData?.latchFireDamageBonusMP ?? 0.05f;
        string desc = details
            ? $"Summons <color=green><b>{GData?.wispCount ?? 2}</b></color> Cinderwisps that fly across the map seeking the most injured plant. " +
              $"Plants within <color=green><b>{GData?.wispRadius ?? 1.5f:F1}</b></color> radius heal " +
              $"<color=green><b>[({GData?.wispHealPerSecond ?? 4f:F0}) + ({healpl:F0}/Lvl.) + <color=#FFB6C1>{wispHealMP * 100f:F0}% Magic Power</color>]</b></color> health and " +
              $"warm <color=orange><b>[({GData?.wispTemperaturePerSecond ?? 1f:F1}) + ({temppl:F1}/Lvl.)]</b></color> per second. " +
              $"When a wisp latches: heals <color=green><b>[({GData?.latchHealPerSecond ?? 8f:F0}) + ({lhealpl:F0}/Lvl.) + <color=#FFB6C1>{latchHealMP * 100f:F0}% Magic Power</color>]</b></color> per second and " +
              $"increases <color=orange><b>Fire Damage</b></color> by <color=orange><b>[({(GData?.latchFireDamageBonus ?? 0.2f) * 100f:F0}%) + ({lfirepl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{latchFireMP * 100f:F0}% Magic Power</color>]</b></color>. " +
              $"Lingers for <color=orange><b>[({GData?.latchDuration ?? 3f:F1}) + ({ldurpl:F1}/Lvl.)]</b></color>s. " +
              $"Wisps last <color=green><b>[({GData?.wispDuration ?? 20f:F0}) + ({durpl:F0}/Lvl.)]</b></color>s."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase lifetime by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase healing by <color=green><b>{healpl:F0}</b></color> per second per level. [<color=green><b>+{healpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase heating by <color=orange><b>{temppl:F1}</b></color>° per second per level. [<color=orange><b>+{temppl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase <color=orange><b>Boon of The Wisp</b></color> healing by <color=green><b>{lhealpl:F0}</b></color> per second per level. [<color=green><b>+{lhealpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=orange><b>Fire Damage</b></color> bonus by <color=orange><b>{lfirepl * 100f:F0}%</b></color> per level. [<color=orange><b>+{lfirepl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=orange><b>Boon of The Wisp</b></color> duration by <color=green><b>{ldurpl:F1}</b></color> seconds per level. [<color=green><b>+{ldurpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Summons an additional <color=orange><b>Cinderwisp</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
