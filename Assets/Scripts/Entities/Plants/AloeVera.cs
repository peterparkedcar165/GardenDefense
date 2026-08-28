using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AloeVera : Lobber
{
    [Header("Aloe Vera")]
    public float baseHealAmount = 24f;
    public float baseTempReduction = 4.5f;
    public float healAmount;
    public float tempReduction;
    public float baseSkillHealPerTick = 10f;
    public float baseSkillHealInterval = 1f;
    public float baseSkillTempReduction = 2f;
    public float skillHealPerTick;
    public float channelDuration = 1.5f;
    [SerializeField] private GameObject soothingRainPrefab;

    private bool _isSkillTargeting = false;
    private readonly HashSet<Plant> _highlightedPlants = new HashSet<Plant>();

    private bool autoCastEnabled = false;
    private Vector3 autoCastPosition;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    private AloeVeraData AVData => data as AloeVeraData;
    private float DrizzleBarrierDuration => 16f * (1f + skillDurationMultiplier) + skillDurationAdder;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        if (AVData != null)
        {
            baseHealAmount        = AVData.baseHealAmount;
            baseTempReduction     = AVData.baseTempReduction;
            baseSkillHealPerTick   = AVData.baseSkillHealPerTick;
            baseSkillHealInterval  = AVData.baseSkillHealInterval;
            baseSkillTempReduction = AVData.baseSkillTempReduction;
        }
    }

    public override void UpdateStats()
    {
        float path1MPBonus = IsPath1Maxed ? 50f : 0f;
        magicPowerAdder += path1MPBonus;
        // hidden, undocumented, skill charges faster per level of rain exposure
        int rainLevel = GetEffect<RainExposedEffect>()?.level ?? 0;
        skillChargeRateAdder = 0.2f * rainLevel;
        base.UpdateStats();
        magicPowerAdder -= path1MPBonus;
        temperatureMax = comfortMax;
        float healpl  = AVData?.path2HealPerLevel           ?? 8f;
        float temppl  = AVData?.path2TempReductionPerLevel  ?? 0.5f;
        float healpl3 = AVData?.path3SkillHealPerLevel      ?? 2f;
        healAmount       = baseHealAmount       + healpl  * effectivePath2Level + magicPower * 0.22f;
        tempReduction    = baseTempReduction    + temppl  * effectivePath2Level;
        skillHealPerTick = baseSkillHealPerTick + healpl3 * effectivePath3Level + magicPower * 0.14f;
    }

    protected override void Update()
    {
        base.Update();
        UpdateHighlights();

        if (autoCastEnabled && SkillReady)
            CastSoothingRain(autoCastPosition);
    }

    // click Auto Cast to pick a target, click again to turn it off
    public override void ToggleAutoCast()
    {
        if (autoCastEnabled)
        {
            autoCastEnabled = false;
            return;
        }
        _isSkillTargeting = true;
        SkillTargetingManager.instance.BeginTargeting(baseSkillRadius, OnAutoCastTargetConfirmed);
    }

    private void OnAutoCastTargetConfirmed(Vector3 position)
    {
        _isSkillTargeting = false;
        autoCastEnabled = true;
        autoCastPosition = position;
    }

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled, targetPosition = autoCastPosition };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled) return;
        autoCastEnabled = true;
        autoCastPosition = state.targetPosition;
    }

    public override GameObject FindLobberTarget()
    {
        // priority 1: an injured plant (lowest health wins)
        Plant bestPlant = null;
        float lowestPlantHealth = Mathf.Infinity;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == this || plant == null || !plant.IsAlive) continue;
            if (plant.health >= plant.maxHealth) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;
            if (plant.health < lowestPlantHealth) { lowestPlantHealth = plant.health; bestPlant = plant; }
        }
        if (bestPlant != null) return bestPlant.gameObject;

        // priority 2 (only if no injured plant in range): an injured friendly minion
        Insect bestAlly = null;
        float lowestAllyHealth = Mathf.Infinity;
        foreach (Insect ally in Insect.friendlyInsects)
        {
            if (ally == null || !ally.IsAlive) continue;
            if (ally.health >= ally.maxHealth) continue;
            if (Vector3.Distance(transform.position, ally.transform.position) > attackRange) continue;
            if (ally.health < lowestAllyHealth) { lowestAllyHealth = ally.health; bestAlly = ally; }
        }
        if (bestAlly != null) return bestAlly.gameObject;

        return base.FindLobberTarget();
    }

    protected override void Fire(GameObject target, Vector3 landingPos)
    {
        if (projectilePrefab == null) return;
        // heal mode for plants and for friendly minions; otherwise it's an attack on an enemy
        bool isHealMode = target.GetComponent<Plant>() != null
                       || (target.GetComponent<Insect>() is Insect ins && ins.team == Team.Friendly);

        float bobDuration = Mathf.Clamp(0.4f / Mathf.Max(attackSpeed, 0.01f), 0.1f, 0.8f);

        GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        AloeVeraProjectile proj = obj.GetComponent<AloeVeraProjectile>();
        if (proj == null) return;
        proj.arcPeakHeight = projectileHeight;
        proj.Initialize(target.transform, landingPos,
                        attackDamage, projectileSpeed, aoERadius,
                        healAmount, tempReduction, isHealMode, bobDuration,
                        damageType, elementalType, this);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + level * (AVData?.path1AttackSpeedPerLevel ?? 0.02f);
        baseAttackRange = data.baseAttackRange + level * (AVData?.path1AttackRangePerLevel ?? 0.2f);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (AVData?.path3SkillDurationPerLevel ?? 1f) * level;
        baseSkillRadius   = data.baseSkillRadius   + (AVData?.path3RadiusPerLevel        ?? 0.3f) * level;
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        _isSkillTargeting = true;
        SkillTargetingManager.instance.BeginTargeting(baseSkillRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        _isSkillTargeting = false;
        CastSoothingRain(position);
    }

    // shared by the manual skill cast and auto cast, does not reopen targeting on its own
    private void CastSoothingRain(Vector3 position)
    {
        skillCooldownTimer = skillCooldown;
        BeginChannel();   // can't attack until the rain appears (set channelDuration in data)
        StartCoroutine(ChannelAndSpawn(position));
    }

    private void UpdateHighlights()
    {
        if (!SkillTargetingManager.instance.IsTargeting) _isSkillTargeting = false;

        bool isSelected = PlantUpgradeUI.instance?.GetSelectedPlant() == this;

        var desired = new HashSet<Plant>();
        Color highlightColor = Color.cyan;

        if (_isSkillTargeting)
        {
            Vector3 mousePos = SkillTargetingManager.instance.GetMouseWorldPosition();
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == null) continue;
                if (Vector2.Distance(mousePos, plant.transform.position) <= baseSkillRadius)
                    desired.Add(plant);
            }
            highlightColor = Color.cyan;
        }
        else if (autoCastEnabled && isSelected)
        {
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == null) continue;
                if (Vector2.Distance(autoCastPosition, plant.transform.position) <= baseSkillRadius)
                    desired.Add(plant);
            }
            highlightColor = Color.yellow;
        }
        else if (isSelected)
        {
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == null) continue;
                if (Vector2.Distance(transform.position, plant.transform.position) <= attackRange)
                    desired.Add(plant);
            }
            highlightColor = Color.cyan;
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

    private IEnumerator ChannelAndSpawn(Vector3 position)
    {
        yield return new WaitForSeconds(channelDuration);
        if (soothingRainPrefab == null) yield break;
        GameObject obj = Instantiate(soothingRainPrefab, position, Quaternion.identity);
        obj.GetComponent<SoothingRain>()?.Initialize(baseSkillRadius, skillDuration, skillHealPerTick, baseSkillHealInterval, baseSkillTempReduction, this);
    }

    public override string GetName() => "<b><color=#4FC3F7>Aloe Vera</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} lobs arcing water droplets that burst on impact. She prioritizes healing injured allies before engaging insects.";

    public override string GetAttackDescription() =>
        $"Lob a water droplet that bursts on landing, dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects within <color=green><b>{aoERadius:F1}</b></color> radius.";

    public override string GetPassiveDescription() =>
        $"Water droplets also heal plants, restoring <color=green><b>{healAmount:F0}</b></color> [<color=#FFB6C1><b>+{magicPower * 0.22f:F0}</b></color>] Health and reducing temperature by <color=#4FC3F7><b>{tempReduction:F1}</b></color>, until comfort, for all plants within <color=green><b>{aoERadius:F1}</b></color> radius. If an injured plant is within range, switch targetting to the one with the lowest Health.";

    public override string GetPath1Description(bool details = false)
    {
        float aspl    = AVData?.path1AttackSpeedPerLevel ?? 0.02f;
        float rangepl = AVData?.path1AttackRangePerLevel ?? 0.2f;
        string desc = details
            ? $"Lob a water droplet that bursts on landing, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects within <color=green><b>{aoERadius:F1}</b></color> radius."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Increase <color=#FFB6C1><b>Magic Power</b></color> by <color=green><b>50</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float healpl = AVData?.path2HealPerLevel          ?? 8f;
        float temppl = AVData?.path2TempReductionPerLevel ?? 0.5f;
        string desc = details
            ? $"Water droplets also heal plants, restoring <color=green><b>[({baseHealAmount:F0}) + ({healpl:F0}/Lvl.) + <color=#FFB6C1>22% Magic Power</color>]</b></color> Health and reducing temperature by <color=#4FC3F7><b>[({baseTempReduction:F1}) + ({temppl:F1}/Lvl.)]</b></color>, until comfort, for all plants within <color=green><b>{aoERadius:F1}</b></color> radius. If an injured plant is within range, switch targetting to the one with the lowest Health."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Healing</b></color> by <color=green><b>{healpl:F0}</b></color> per level. [<color=green><b>+{healpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase temperature reduction by <color=green><b>{temppl:F1}</b></color> per level. [<color=green><b>+{temppl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path2Level, "Heals also restore an additional <color=green><b>12%</b></color> of the target's <color=green><b>Missing Health</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetSkillDesription()
    {
        float totalHealing = skillHealPerTick * Mathf.Floor(skillDuration / baseSkillHealInterval);
        return $"Channels for <color=green><b>{channelDuration:F1}s</b></color>, then calls down a Soothing Rain on a targeted area, healing all plants within <color=green><b>{baseSkillRadius:F1}</b></color> radius for <color=green><b>{skillHealPerTick:F0}</b></color> [<color=#FFB6C1><b>+{magicPower * 0.14f:F0}</b></color>] Health and reducing temperature by <color=#4FC3F7><b>{baseSkillTempReduction:F1}</b></color>, until comfort, every <color=green><b>{baseSkillHealInterval:F1}s</b></color> over <color=green><b>{skillDuration:F0}</b></color> seconds. " +
               $"For a total of <color=green><b>{totalHealing:F0}</b></color> Health.";
    }

    public override string GetPath3Description(bool details = false)
    {
        float healpl3  = AVData?.path3SkillHealPerLevel    ?? 2f;
        float durpl    = AVData?.path3SkillDurationPerLevel ?? 1f;
        float radiuspl = AVData?.path3RadiusPerLevel        ?? 0.3f;
        string desc = details
            ? $"Channels for <color=green><b>{channelDuration:F1}</b></color> seconds, then calls down a Soothing Rain on a targeted area, healing all plants within <color=green><b>[({data.baseSkillRadius:F1}) + ({radiuspl:F1}/Lvl.)]</b></color> radius for <color=green><b>[({baseSkillHealPerTick:F0}) + ({healpl3:F0}/Lvl.) + <color=#FFB6C1>14% Magic Power</color>]</b></color> Health and reducing temperature by <color=#4FC3F7><b>{baseSkillTempReduction:F1}</b></color>, until comfort, every <color=green><b>{baseSkillHealInterval:F1}</b></color> seconds over <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Healing</b></color> by <color=green><b>{healpl3:F0}</b></color> per tick per level. [<color=green><b>+{healpl3 * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase rain duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase rain radius by <color=green><b>{radiuspl:F1}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Overhealing from Soothing Rain accumulates into <color=#4FC3F7><b>Drizzle Barrier</b></color>, shielding plants for up to <color=green><b>120</b></color> for <color=green><b>{DrizzleBarrierDuration:F0}s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
