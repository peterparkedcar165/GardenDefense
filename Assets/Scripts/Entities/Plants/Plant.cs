using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// the base stats the Encyclopedia and loadout screens show for a plant (see GetBaseStats)
public struct PlantBaseStats
{
    public float attackDamage, attackSpeed, attackRange, skillCooldown, passiveCooldown;
    public int piercing;
}

public enum TARGETING { Nearest, First, Last, Strongest, Fastest, Slowest }

// a plant's job in the garden, replaces the old families. no mechanics read it yet
public enum PlantRole { Tank, DPS, Support }

// generic snapshot of a plant's auto-cast state, captured on death and restored on revival.
// targetTile is used by plants that auto-cast onto another plant (e.g. Calendula), so the
// auto-cast keeps working even if that target plant itself dies and gets revived later, since
// it's resolved live from the tile rather than pinned to the old (destroyed) instance.
// targetPosition is used by plants that auto-cast onto a fixed area (e.g. Begonia)
[System.Serializable]
public struct AutoCastState
{
    public bool enabled;
    public Tile targetTile;
    public Vector3 targetPosition;
}

[System.Serializable]
public class DeadPlantRecord
{
    public GameObject prefab;
    public Sprite previewSprite;
    public Vector3 spriteLocalPosition;
    public int sortingLayerID;
    public int sortingOrder;
    public int path1Level, path2Level, path3Level;
    public bool path3Unlocked;
    public int totalSunSpent;
    public int exp;
    public float skillCooldownRemaining;
    public float deathTime;
    public AutoCastState autoCastState;
    public int orbitRadius;
    public Dictionary<string, float> mulchCarry;
    // the mulches it had equipped, stopped. a rhodiola skill revive brings them back
    // (MulchManager.RestoreMulches); any other revive leaves them lost
    public List<MulchEffect> mulches;
}

public abstract class Plant : Entity, IAttackable
{
    public static List<Plant> allPlants = new List<Plant>();
    private static readonly List<GameObject> _ghosts = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        allPlants = new List<Plant>();
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            allPlants.Clear();
            HideDeadPlantGhosts();
        };
    }

    public PlantData data;
    public Tile occupiedTile;
    public GameObject selfPrefab;

    // tracks whether the +0.5 attack range (and Cattail/LeafRanger +15% crit) highground bonus
    // is currently applied, so ApplyHighgroundBonus can add/remove it exactly once instead of
    // stacking - set on initial placement (Tile.OnMouseDown) and recalculated on every
    // TriggerTransplant, since relocating can move a plant on or off highground
    public bool hasHighgroundBonus;

    // highground range bonus, added into the same percent pool as fertilizer and skill tree range
    public const float HighgroundRangeBonus = 0.5f;

    public void ApplyHighgroundBonus(bool shouldHaveBonus)
    {
        if (shouldHaveBonus == hasHighgroundBonus) return;
        float sign = shouldHaveBonus ? 1f : -1f;
        attackRangeMultiplier += HighgroundRangeBonus * sign;
        if (this is Cattail || this is LeafRanger)
            criticalChanceAdder += 0.15f * sign;
        hasHighgroundBonus = shouldHaveBonus;
        OnHighgroundBonusChanged();
    }

    // hook for a plant whose own "grant a buff to everyone currently in range" scan is only
    // ever re-run on specific events (placement, a path upgrade, another plant appearing - see
    // Calendula/Begonia's own ApplyAuraToAllInRange) rather than every frame. without this, such
    // a plant's attackRange/lightEmissionRange NUMBER still correctly includes the highground
    // bonus (that's computed fresh every UpdateStats tick regardless), but anyone only newly in
    // range BECAUSE of that bonus never actually gets the buff until some unrelated plant
    // happens to be placed somewhere else on the field and triggers a rescan. default: no-op
    protected virtual void OnHighgroundBonusChanged() {}

    // how many "tickets" this plant holds in an insect's weighted-random target roll (see
    // Insect.FindTauntWeightedPlantInRange). baseTauntValue is author-set per plant in its
    // PlantData asset (default 3 there) - tauntValueAdder is the only
    // way to shift it at runtime from there (no multiplier, by design). floored at 0: a plant
    // at 0 tickets can never be rolled, i.e. unselectable
    public float baseTauntValue;
    public float tauntValueAdder;
    public float tauntValue;

    // generic hook: when set, FindNearest/FindFirst/FindLast/FindStrongest return this insect
    // first (if still alive, hostile, and within whatever this plant's own IsWithinAttackRange
    // allows) before falling back to their normal selection logic. null for every plant by
    // default - currently only driven by Snowdrop, which points her bonded partner's own
    // instance at the current Snow Mark carrier
    public Insect forcedPriorityTarget;

    public virtual bool ShowRangeCircle => true;
    protected virtual bool GetPassiveBarVisible() => passiveCooldown > 0f && passiveCooldownTimer > 0f;
    protected virtual float GetPassiveBarFill() => passiveCooldown > 0f ? Mathf.Clamp01(1f - passiveCooldownTimer / passiveCooldown) : 1f;
    protected virtual bool GetAttackBarVisible() => false;
    protected virtual float GetAttackBarFill() => attackCooldown > 0f ? Mathf.Clamp01(attackCooldownTimer / attackCooldown) : 0f;
    // a plant's own color for its cooldown bars in the widget. null = the style's color for the
    // bar's position under the health bar
    protected virtual Color? AttackBarColor => null;
    protected virtual Color? PassiveBarColor => null;
    // true puts the passive bar right under the health bar, above the attack bar (rhodiola's sap)
    protected virtual bool PassiveBarFirst => false;
    [SerializeField] private Transform circleRadius;
    private Transform darkCircleRadius;
    private CircleCollider2D _circleCollider;
    private bool _isSelected = false;
    private UnityEngine.Rendering.Universal.Light2D _light2D;
    private LightFader _lightFader;
    private float _lastLightEmissionRange;
    protected virtual float LightIntensity => 0.5f;
    // the bar widget over the plant (health, attack and passive cooldown bars, skill circle),
    // replacing the old health bar and its stacked cooldown bars. fed every frame in Update and
    // TickHealthWidget
    private PlantWidget _widget;
    [SerializeField] private float lightInnerRadius = 1.2f;
    [SerializeField] private float lightFalloffStrength = 0.2f;

    // lightInnerRadius is a fixed absolute distance, which works fine for the normal (larger)
    // light radii most plants use - but for a small light (e.g. Glowing Swarm's 0.75), clamping
    // straight to lightEmissionRange collapses inner == outer, killing the falloff gradient
    // entirely and making the light render as a flat, hard-edged disc instead of fading out like
    // Calendula's. only scale it down when the light is actually too small for the normal value
    // to fit, so every existing (larger) light keeps its exact current look
    private float ComputeLightInnerRadius(float outerRadius) =>
        outerRadius <= lightInnerRadius ? outerRadius * 0.4f : lightInnerRadius;
    // off on day levels, and on dark levels while sunlight has the point lights switched off
    protected virtual bool ShowLight => DarknessManager.LightsVisible;
    public virtual bool ShowDarkCircle => true;

    // stunned plants (e.g. Webbed by a Cave Spider) cannot attack
    public bool IsStunned => HasEffect<HardCrowdControl>();

    // while true this plant can't pick an insect as a target, deal it damage, or put a debuff on it.
    // it can still be hit, and anything that targets plants or runs on a timer (heals, sun) keeps
    // working. nothing sets it right now (the old bud mulch did); kept as a general switch
    [System.NonSerialized] public bool cannotTargetInsects;

    // mulch state that belongs to this one plant and has to survive its death and revival (seasoned
    // oak's armor climb). copied into DeadPlantRecord on death and back onto the revived plant
    public readonly Dictionary<string, float> mulchCarry = new Dictionary<string, float>();

    // equipped loot mulches, in slot order, at most MulchRegistry.plantSlots (see MulchManager)
    [System.NonSerialized] public readonly List<MulchEffect> mulches = new List<MulchEffect>();

    // what this plant's own mulches add, each put in by its MulchEffect and taken back out when it
    // stops. its own sun production is multiplied by 1 + mulchSunProductionBonus (drought), slows
    // it applies get mulchSlowBonus (hinder), primers it applies start their cooldown
    // mulchPrimerCooldownReduction seconds shorter (reaction adept), and regeneration it receives
    // heals it 1 + mulchRegenerationBonus as much (rejuvenator)
    [System.NonSerialized] public float mulchSunProductionBonus;
    [System.NonSerialized] public float mulchSlowBonus;
    [System.NonSerialized] public float mulchPrimerCooldownReduction;
    [System.NonSerialized] public float mulchRegenerationBonus;

    // drop stats, in percents. mulchDropRate is added to the base drop chance of a kill by this plant
    // (1 = one more percent, so 2% becomes 3%; Tommy, 2026-11-21: was Item Drop Rate, a multiplier).
    // rareItemDropRate makes its drops Rare and Epic more often (25 = those weigh 25% more). from
    // mulches (scavenger, four leaf clover) or skill tree nodes (StatType.MulchDropRate, RareItemDropRate)
    [System.NonSerialized] public float mulchDropRate;
    [System.NonSerialized] public float rareItemDropRate;

    // silenced plants cannot cast their Skill. any HardCrowdControl also silences,
    // without the plant carrying the Silence effect itself
    public bool IsSilenced => HasEffect<SilenceEffect>() || HasEffect<HardCrowdControl>();

    // channeling a skill: the plant cannot perform its normal auto-attack while this is active
    public bool IsChanneling => HasEffect<ChannelingEffect>();

    // call when a skill that locks out attacking begins; channels for the data-driven duration
    // (channelDuration on the plant's data). a duration of 0 means no channel
    protected void BeginChannel()
    {
        float dur = data != null ? data.channelDuration : 0f;
        if (dur > 0f) ApplyEffect(new ChannelingEffect(this, dur, 1, this));
    }

    // hover/selection state, used by owned minions to show their range circles alongside the plant
    public bool IsHovered  => _hoverHighlighted;
    public bool IsSelected => PlantUpgradeUI.instance != null && PlantUpgradeUI.instance.GetSelectedPlant() == this;

    // overridden by plants that support UsesAutoCast, so their auto-cast survives their own
    // death and revival. see AutoCastState for how the target is preserved
    public virtual AutoCastState CaptureAutoCastState() => default;
    public virtual void RestoreAutoCastState(AutoCastState state) { }

    // finds whichever plant currently occupies a tile (if any). used to resolve an auto-cast
    // target live rather than pinning to a specific instance, so a target that dies and gets
    // revived (a brand new instance on the same tile) is picked back up automatically
    public static Plant GetPlantOnTile(Tile tile)
    {
        if (tile == null) return null;
        foreach (Plant p in allPlants)
            if (p != null && p.occupiedTile == tile) return p;
        return null;
    }

    // how many currently-alive plants on the field share this exact PlantData asset - used to
    // enforce PlantData.placementLimit (e.g. Elder plants capped at 1 live copy)
    public static int CountAlive(PlantData data)
    {
        if (data == null) return 0;
        int count = 0;
        foreach (Plant p in allPlants)
            if (p != null && p.IsAlive && p.data == data) count++;
        return count;
    }

    // Elder plants are capped at 1 live copy even when placementLimit is left at 0 (unset) - no
    // need to remember to author placementLimit separately for every Elder plant
    public static bool AtPlacementLimit(PlantData data)
    {
        if (data == null) return false;
        int limit = data.placementLimit > 0 ? data.placementLimit
            : data.category == PlantData.PlantCategory.Elder ? 1 : 0;
        return limit > 0 && CountAlive(data) >= limit;
    }

    // most plants keep their sprite on a separate "Visual" child rather than the root (see
    // StoreDeadRecord's own comment on the same point) - centers on that child's SpriteRenderer
    // instead, falling back to the base (transform.position) for the rare plant with none (e.g.
    // Waterlily)
    // looked up once and kept: every heal (regeneration ticks too) asked for it
    private SpriteRenderer _visualCenterRenderer;

    protected override Vector3 GetVisualCenter()
    {
        if (_visualCenterRenderer == null) _visualCenterRenderer = GetComponentInChildren<SpriteRenderer>();
        return _visualCenterRenderer != null ? _visualCenterRenderer.transform.position : base.GetVisualCenter();
    }

    private void StoreDeadRecord()
    {
        if (occupiedTile == null || selfPrefab == null) return;
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        occupiedTile.deadPlant = new DeadPlantRecord
        {
            prefab              = selfPrefab,
            previewSprite       = sr != null ? sr.sprite                  : null,
            // world-space offset from the plant root, not sr.transform.localPosition - for a
            // plant like Waterlily whose sprite sits directly on the root (no separate "Visual"
            // child like most plants), localPosition is relative to the plant's PARENT, which is
            // null for a placed (unparented) plant, making it equal the plant's full world
            // position instead of a small offset. ShowDeadPlantGhosts adds this straight onto
            // the tile's position, so that bug alone used to roughly double the ghost's position
            spriteLocalPosition = sr != null ? sr.transform.position - transform.position : Vector3.zero,
            sortingLayerID      = sr != null ? sr.sortingLayerID          : 0,
            sortingOrder        = sr != null ? sr.sortingOrder            : 0,
            path1Level   = path1Level,
            path2Level   = path2Level,
            path3Level   = path3Level,
            path3Unlocked = path3Unlocked,
            totalSunSpent = totalSunSpent,
            exp          = exp,
            skillCooldownRemaining = skillCooldownTimer,
            deathTime    = Time.time,
            autoCastState = CaptureAutoCastState(),
            orbitRadius  = orbitRadius,
            mulchCarry   = new Dictionary<string, float>(mulchCarry),
        };
    }

    public static void ShowDeadPlantGhosts()
    {
        HideDeadPlantGhosts();
        foreach (var kvp in Tile.allTiles)
        {
            DeadPlantRecord record = kvp.Value.deadPlant;
            if (record == null || record.previewSprite == null) continue;

            GameObject ghost = new GameObject("DeadPlantGhost");
            ghost.transform.position = kvp.Value.transform.position + record.spriteLocalPosition;
            SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = record.previewSprite;
            sr.sortingLayerID = record.sortingLayerID;
            sr.sortingOrder   = record.sortingOrder;
            sr.color = Color.white;
            ghost.AddComponent<GhostFlicker>();
            _ghosts.Add(ghost);
        }
    }

    // called right after placing a new plant: if it's Elder-tier (capped at 1 live copy) and an
    // older dead copy of that exact same plant is still sitting as a revivable ghost elsewhere,
    // that ghost has to be invalidated - reviving it later would create a second live copy of a
    // plant that's only ever supposed to have one
    public static void ClearGhostsOfSameType(Plant placed)
    {
        if (placed == null || placed.selfPrefab == null || placed.data == null) return;
        if (placed.data.category != PlantData.PlantCategory.Elder) return;

        foreach (var kvp in Tile.allTiles)
        {
            Tile other = kvp.Value;
            if (other == placed.occupiedTile) continue;
            if (other.deadPlant != null && other.deadPlant.prefab == placed.selfPrefab)
                other.deadPlant = null;
        }
    }

    public static void HideDeadPlantGhosts()
    {
        foreach (GameObject g in _ghosts)
            if (g != null) Destroy(g);
        _ghosts.Clear();
    }

    // restoreMulches: a rhodiola skill revive, the plant comes back with the mulches it had. every
    // other revive leaves them lost
    public static Plant RevivePlant(Tile tile, bool restoreMulches = false)
    {
        if (tile == null || tile.deadPlant == null || tile.deadPlant.prefab == null) return null;
        DeadPlantRecord record = tile.deadPlant;
        tile.deadPlant = null;

        GameObject obj = Instantiate(record.prefab, tile.transform.position, Quaternion.identity);
        Plant plant = obj.GetComponent<Plant>();
        if (plant == null) { Destroy(obj); return null; }

        plant.selfPrefab    = record.prefab;
        plant.occupiedTile  = tile;
        plant.path1Level    = record.path1Level;
        plant.path2Level    = record.path2Level;
        plant.path3Level    = record.path3Level;
        plant.path3Unlocked = record.path3Unlocked;
        plant.totalSunSpent = record.totalSunSpent;
        plant.exp           = record.exp;
        plant.effectivePath1Level = record.path1Level;
        plant.effectivePath2Level = record.path2Level;
        plant.effectivePath3Level = record.path3Level;
        plant.OnPath1Upgrade(record.path1Level);
        plant.OnPath2Upgrade(record.path2Level);
        plant.OnPath3Upgrade(record.path3Level);
        plant.UpdateStats();

        plant.health = 1f;
        plant.UpdateHealthBar();
        // the cooldown kept ticking while the plant was dead, resume as if it never died
        plant.skillCooldownTimer = Mathf.Max(0f, record.skillCooldownRemaining - (Time.time - record.deathTime));
        plant.RestoreAutoCastState(record.autoCastState);
        plant.orbitRadius = record.orbitRadius > 0 ? record.orbitRadius : 1;
        if (record.mulchCarry != null)
            foreach (KeyValuePair<string, float> entry in record.mulchCarry)
                plant.mulchCarry[entry.Key] = entry.Value;
        plant.ApplyHighgroundBonus(tile.isHighground);
        if (restoreMulches) MulchManager.RestoreMulches(record, plant);

        tile.isOccupied = true;
        Collider2D tileCol = tile.GetComponent<Collider2D>();
        if (tileCol != null) tileCol.enabled = false;
        return plant;
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        tauntValue = Mathf.Max(0f, baseTauntValue + tauntValueAdder);
        comfortMin = baseComfortMin + comfortMinAdder;
        comfortMax = baseComfortMax + comfortMaxAdder;
        temperatureMin = baseTemperatureMin + temperatureMinAdder;
        temperatureMax = baseTemperatureMax + temperatureMaxAdder;
        heatResistance = baseHeatResistance + heatResistanceAdder + (baseHeatResistance * heatResistanceMultiplier);
        coldResistance = baseColdResistance + coldResistanceAdder + (baseColdResistance * coldResistanceMultiplier);
        respiration = baseRespiration + respirationAdder;
        passiveCooldown = Mathf.Max(basePassiveCooldown * 0.2f, basePassiveCooldown + passiveCooldownAdder + (basePassiveCooldown * passiveCooldownMultiplier) - (basePassiveCooldown * passiveCooldownReductionMultiplier));
        passiveDuration = basePassiveDuration + passiveDurationAdder + (basePassiveDuration * passiveDurationMultiplier);
        skillCooldown = Mathf.Max(baseSkillCooldown * 0.2f, baseSkillCooldown - skillCooldownReductionAdder - (baseSkillCooldown * skillCooldownReductionMultiplier));
        skillChargeRate = Mathf.Max(0f, baseSkillChargeRate + skillChargeRateAdder);
        skillRadius = baseSkillRadius + skillRadiusAdder + (baseSkillRadius * skillRadiusMultiplier);
        skillDamageMultiplier = baseSkillDamageMultiplier + skillDamageMultiplierAdder;
        skillDamage += baseSkillDamage * skillDamageMultiplier;
        float plantSpriteRadius = _circleCollider != null ? _circleCollider.radius * 2 : 0f;

        if (ShowRangeCircle && circleRadius != null)
        {
            // circleRadius.localScale = new Vector3((attackRange * 2f)  + plantSpriteRadius, (attackRange * 2f) + plantSpriteRadius, 1f); // INCLUDES SPRITE
            circleRadius.localScale = new Vector3(attackRange * 2f, attackRange * 2f, 1f);
        }
        if (darkCircleRadius != null)
            darkCircleRadius.localScale = new Vector3(attackRange, attackRange, 1f);

        if (lightEmissionRange > 0 && _light2D == null)
        {
            GameObject lightObj = new GameObject("PlantLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;

            _light2D = lightObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            _light2D.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            _light2D.intensity = 0f;
            _light2D.falloffIntensity = lightFalloffStrength;
            _light2D.targetSortingLayers = GetAllSortingLayerIDs();

            _lightFader = lightObj.AddComponent<LightFader>();
            _lightFader.Setup(_light2D, LightIntensity);
        }

        if (_light2D != null)
            _light2D.enabled = ShowLight;

        // weather bonuses are now applied via SunlightExposedEffect, RainExposedEffect, SnowExposedEffect
        // which modify the adder fields directly, so nothing extra needed here
    }
    public TileType[] allowedTiles;
    public TARGETING targeting = TARGETING.First;
    public virtual bool UsesTargeting => false;

    // secondary "flying first" targeting toggle (Cattail). when on, the plant prefers flying targets
    public bool prioritizeFlying = false;
    public virtual bool UsesFlyingToggle => false;

    // fixed orbit radius toggle: available on every plant as long as Tansy is part of the
    // player's selected roster for this level (her orbit projectiles read the specific orbited
    // plant's own toggle, not hers), not just on Tansy herself. cycles 1..MaxOrbitRadius,
    // wrapping back to 1
    public int orbitRadius = 1;
    public bool UsesRadiusToggle => SaveManager.instance != null && SaveManager.instance.selectedLoadout.Contains("Tansy");
    public virtual int MaxOrbitRadius => Mathf.Max(1, Mathf.FloorToInt(attackRange));

    // the attack range this plant will have the moment it is placed, for the placement preview.
    // reads only data and the managers, never this component's own stat fields, so it is safe to
    // call on the prefab. same formula as Entity.UpdateStats: (base + flat + base * percent) * total,
    // where base already includes the path levels the skill tree's effective point nodes give
    public float PreviewAttackRange(bool onHighground)
    {
        if (data == null) return 0f;
        int p1 = Mathf.RoundToInt(SkillTreeManager.SumStat(data, StatType.Path1LevelAdder));
        int p2 = Mathf.RoundToInt(SkillTreeManager.SumStat(data, StatType.Path2LevelAdder));
        int p3 = Mathf.RoundToInt(SkillTreeManager.SumStat(data, StatType.Path3LevelAdder));
        // same as GetWeatherPath2Bonus: fire plants in sunny weather get one extra passive level
        if (data.elementalType == ElementalType.Fire && IsWeatherActive(WeatherType.Sunny)) p2 += 1;

        float baseRange = data.baseAttackRange
            + RangePerLevel(data, 1) * p1 + RangePerLevel(data, 2) * p2 + RangePerLevel(data, 3) * p3;
        float flat = SkillTreeManager.SumStat(data, StatType.AttackRangeFlat);
        float percent = SkillTreeManager.SumStat(data, StatType.AttackRange)
            + (onHighground ? HighgroundRangeBonus : 0f);

        float range = baseRange + flat + baseRange * percent;
        // melee attackers never reach past their own base range, same clamp as UpdateStats
        if (IsMeleeAttacker) range = Mathf.Min(range, baseRange);
        return range;
    }

    private static bool IsWeatherActive(WeatherType type)
    {
        if (WeatherManager.instance == null) return false;
        foreach (WeatherEntry entry in WeatherManager.instance.GetActiveWeather())
            if (entry.type == type) return true;
        return false;
    }

    // every plant names its per level range field pathNAttackRangePerLevel on its data (most use
    // path 1, Calendula and Carrot path 2, Tansy both), 0 when that path doesn't add range
    private static readonly System.Collections.Generic.Dictionary<(System.Type, int), System.Reflection.FieldInfo> _rangePerLevelFields
        = new System.Collections.Generic.Dictionary<(System.Type, int), System.Reflection.FieldInfo>();

    private static float RangePerLevel(PlantData data, int path)
    {
        var key = (data.GetType(), path);
        if (!_rangePerLevelFields.TryGetValue(key, out System.Reflection.FieldInfo field))
        {
            field = data.GetType().GetField($"path{path}AttackRangePerLevel");
            if (field != null && field.FieldType != typeof(float)) field = null;
            _rangePerLevelFields[key] = field;
        }
        return field != null ? (float)field.GetValue(data) : 0f;
    }

    // Burgeon plants that command minions: the info panel shows a "relocate formation" button,
    // and clicking it (after aiming a point) calls RelocateMinionFormation
    public virtual bool CommandsMinions => false;
    public virtual void RelocateMinionFormation(Vector3 worldPoint) { }

    // plants whose skill can be locked onto a plant target and recast automatically
    // whenever it comes off cooldown, until toggled off (Calendula)
    public virtual bool UsesAutoCast => false;
    public virtual bool IsAutoCasting => false;
    public virtual void ToggleAutoCast() { }
    // overridable label for the auto-cast button, since it isn't always about "casting" a
    // skill on a timer (e.g. Carrot's is a one-time, permanent link, so it reads better as "Bond")
    public virtual string AutoCastLabel => "Auto Cast";
    // sunCost is the final, actually-charged placement cost: baseSunCost minus any
    // sunCostReductionAdder from skill tree nodes or fertilizers (StatType.SunCostReduction),
    // computed once in LoadData after those effects are applied
    public int sunCost, baseSunCost, sunCostReductionAdder, totalSunSpent = 0;
    [System.NonSerialized] public ElementalType elementalType;
    [System.NonSerialized] public DamageType damageType;
    public int exp = 0;
    public float expBoost;
    public float activeCooldown;

    [Header("Body Animation")]
    // the Animator on this plant's Visual (its drawn body). optional: a plant without animations
    // yet leaves it empty, and every body helper below quietly does nothing
    [SerializeField] protected Animator bodyAnimator;
    // Animator.speed is one multiplier shared by every state, so after a clip is sped up or slowed
    // down it is put back to 1 when that clip would have finished, or Idle keeps the leftover speed
    private Coroutine _bodySpeedResetRoutine;
    // parameter names of the controller currently on bodyAnimator, so asking for one it doesn't
    // have (an empty or half built controller) is skipped instead of warning every frame
    private RuntimeAnimatorController _bodyParametersFor;
    private readonly HashSet<string> _bodyParameters = new HashSet<string>();
    // a skill's body animation owns the body until this time, see PlayBodyAnimation
    private float _bodyInterruptUntil;

    [Header("Temperature")]
    public float temperature = 10f;
    public float baseComfortMin = 0f,    comfortMinAdder,    comfortMin;
    public float baseComfortMax = 20f,   comfortMaxAdder,    comfortMax;
    public float baseTemperatureMin = -10f, temperatureMinAdder, temperatureMin;
    public float baseTemperatureMax = 30f,  temperatureMaxAdder, temperatureMax;
    // percentage: slows how fast temperature drifts from Hot/Cold weather, not the damage it deals
    public float baseHeatResistance, heatResistanceAdder, heatResistanceMultiplier, heatResistance;
    public float baseColdResistance, coldResistanceAdder, coldResistanceMultiplier, coldResistance;

    [Header("Air")]
    // 0-100 resource that depletes while Submerged (Underwater weather), mirrors Temperature
    public float air = 100f;
    // percentage: slows how fast Air depletes while Submerged, same shape as heat/cold resistance
    public float baseRespiration, respirationAdder, respiration;

    [Header("Passive")]
    public float basePassiveCooldown, passiveCooldown, passiveCooldownAdder, passiveCooldownReductionMultiplier, passiveCooldownMultiplier;
    public float passiveCooldownTimer;
    public float basePassiveDuration, passiveDuration, passiveDurationAdder, passiveDurationMultiplier;


    [Header("Skill")]
    public float baseSkillCooldown, skillCooldown, skillCooldownReductionAdder, skillCooldownReductionMultiplier;
    public float skillCooldownTimer;
    // multiplier on how fast the skill charges, base 1 is normal speed, 2 charges twice as fast
    public float baseSkillChargeRate = 1f, skillChargeRate = 1f, skillChargeRateAdder;
    public float baseSkillRadius, skillRadius, skillRadiusAdder, skillRadiusMultiplier;
    public float baseSkillDamageMultiplier, skillDamageMultiplier, skillDamageMultiplierAdder;
    public float baseSkillHealth, skillHealth;
    public virtual bool SkillReady => path3Unlocked && skillCooldownTimer <= 0 && !IsSilenced && !IsChanneling;

    // call whenever something reduces this plant's Skill Cooldown by a fixed amount outside of
    // its normal per-frame tick (Dandelion's passive pulse and skill, the Symbiosis/Wither family
    // passive, Hellebore/Cattail/Sunflower/BogIris's Tidal Surge, etc.) - centralizes the floor-at-
    // zero clamp and shows the shared wind-colored "-N" indicator so every source reads the same
    public void ReduceSkillCooldown(float amount)
    {
        if (amount <= 0f || skillCooldownTimer <= 0f) return;
        skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - amount);
        SkillCooldownIndicator.Spawn(transform.position + Vector3.up * 0.25f, amount);
    }


    // Transplant fertilizer only - counts down from TransplantFertilizerEffect.CooldownSeconds after
    // each use, same shape as skillCooldownTimer but independent of it (and of skillChargeRate -
    // this is a utility cooldown, not a skill). stays at 0 (always ready) while the fertilizer
    // isn't active, since the button itself is hidden in that case anyway
    [Header("Transplant")]
    public float transplantCooldownTimer;
    public bool TransplantReady => transplantCooldownTimer <= 0f;

    [Header("Paths")]
    public int path1Level, path2Level, path3Level, path1LevelAdder, path2LevelAdder, path3LevelAdder, effectivePath1Level, effectivePath2Level, effectivePath3Level;
    public bool path3Unlocked;


    protected override void Awake()
    {
        _circleCollider = GetComponent<CircleCollider2D>();
        baseMaxHealth = 200;
        healthBarOffset = new Vector3(0, 0.7f, 0);
        base.Awake();
        _widget = PlantWidget.Create(transform, healthBarOffset.y);
        baseCriticalChance = 0.05f;
        baseCriticalDamage = 1.5f;
        allPlants.Add(this);
    }

    protected void LoadData()
    {
        if (data == null) return;
        baseTauntValue         = data.tauntValue;
        baseMaxHealth          = data.baseMaxHealth;
        baseAttackDamage       = data.baseAttackDamage;
        baseMagicPower        = data.baseMagicPower;
        baseAttackSpeed        = data.baseAttackSpeed;
        baseAttackRange        = data.baseAttackRange;
        baseHealingBonus       = data.baseHealingBonus;
        baseHealingReceived    = data.baseHealingReceived;
        baseTenacity           = data.baseTenacity;
        baseArmor      = data.baseArmor;
        baseMagicArmor = data.baseMagicArmor;
        baseFireResistance     = data.baseFireResistance;
        baseWaterResistance    = data.baseWaterResistance;
        basePoisonResistance   = data.basePoisonResistance;
        baseIceResistance      = data.baseIceResistance;
        baseGrassResistance   = data.baseGrassResistance;
        baseWindResistance     = data.baseWindResistance;
        baseDotResistance      = data.baseDotResistance;
        baseHeatResistance     = data.baseHeatResistance;
        baseColdResistance     = data.baseColdResistance;
        baseRespiration        = data.baseRespiration;
        basePhysicalDamage     = data.basePhysicalDamage;
        baseMagicDamage        = data.baseMagicDamage;
        baseArmorPenFlat       = data.baseArmorPenFlat;
        baseArmorPenPercent    = data.baseArmorPenPercent;
        baseMagicPenFlat       = data.baseMagicPenFlat;
        baseMagicPenPercent    = data.baseMagicPenPercent;
        baseLifesteal          = data.baseLifesteal;
        baseVisibility         = data.baseVisibility;
        baseOnHitEffectiveness = data.baseOnHitEffectiveness;
        baseFireDamage         = data.baseFireDamage;
        baseWaterDamage        = data.baseWaterDamage;
        baseGrassDamage       = data.baseGrassDamage;
        baseWindDamage         = data.baseWindDamage;
        basePoisonDamage       = data.basePoisonDamage;
        baseIceDamage          = data.baseIceDamage;
        baseCriticalChance     = data.baseCriticalChance;
        baseCriticalDamage     = data.baseCriticalDamage;
        baseDotDamage          = data.baseDotDamage;
        baseelementalAffinity     = data.baseelementalAffinity;
        basePassiveDamage      = data.basePassiveDamage;
        baseSkillDamage        = data.baseSkillDamage;
        baseCoordinatedDamage  = data.baseCoordinatedDamage;
        baseLightEmissionRange        = data.baseLightEmissionRange;
        baseMinionDamage              = data.baseMinionDamage;
        baseCounterDamage             = data.baseCounterDamage;
        startingShield                = data.startingShield;
        baseSunCost                   = data.sunCost;
        basePassiveCooldown           = data.basePassiveCooldown;
        basePassiveDuration           = data.basePassiveDuration;
        baseSkillCooldown             = data.baseSkillCooldown;
        baseSkillDuration             = data.baseSkillDuration;
        baseSkillRadius               = data.baseSkillRadius;
        baseSkillDamageMultiplier     = data.baseSkillDamageMultiplier;
        baseSkillHealth               = data.baseSkillHealth;
        elementalType                 = data.elementalType;
        damageType                    = data.damageType;
        if (elementalType == ElementalType.Fire) heatResistanceAdder += 0.5f; // 50% resistance to heat-driven warming, not full immunity
        if (elementalType == ElementalType.Ice)  coldResistanceAdder += 0.5f; // 50% resistance to cold-driven cooling, not full immunity
        SkillTreeManager.ApplyTo(this);
        if (!BuildingPreview) FertilizerManager.instance?.NotifyPlantPlaced(this);
        // baseSunCost is fixed above; sunCostReductionAdder only ever comes from the skill tree
        // call just above (StatType.SunCostReduction), so this always reflects it once it's run
        sunCost = Mathf.Max(0, baseSunCost - sunCostReductionAdder);
        RecomputeEffectivePathLevels();
        UpdateStats();
        health = maxHealth;
        UpdateHealthBar();
        if (!BuildingPreview) OnPlantPlaced?.Invoke(this);
    }

    // true while PlantPreview sets up a copy of a plant only to read its texts: the copy is never
    // placed, so nothing is told it was placed or removed (events, fertilizer, mulches, aims)
    public static bool BuildingPreview;

    // fires once a plant's stats are actually valid (end of LoadData, not Awake — subclasses
    // call LoadData from their own Awake, after their data-driven fields are set up). lets
    // stationary aura plants (Hellebore, Begonia, Zinnia, Calendula) react when a new plant
    // appears nearby, instead of re-scanning the whole field every tick
    public static event System.Action<Plant> OnPlantPlaced;

    // fires from OnDestroy, for ANY removal cause (death, uproot/sell, scene unload) - lets a
    // plant holding a reference to another specific plant (e.g. Carrot's Psionic Bond) clean
    // that reference up no matter how the bonded plant leaves the field
    public static event System.Action<Plant> OnPlantRemoved;

    protected int[] GetAllSortingLayerIDs()
    {
        var layers = UnityEngine.SortingLayer.layers;
        int[] ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            ids[i] = layers[i].id;
        return ids;
    }

    protected virtual void OnDestroy()
    {
        WeatherManager.OnWeatherAdded   -= HandleWeatherAdded;
        WeatherManager.OnWeatherRemoved -= HandleWeatherRemoved;
        // a tooltip's copy (PlantPreview) was never placed: it only leaves the plant list
        if (BuildingPreview)
        {
            allPlants.Remove(this);
            ReleaseHitFlashMaterial();
            return;
        }
        ClearTargetHighlight();
        MulchManager.OnPlantRemoved(this);
        allPlants.Remove(this);
        // an aim this plant started (a skill target, a plant to bond, a dead tile to revive) can't be
        // confirmed for a plant that's gone: a dying or uprooted plant cancels it. aims are started
        // from the selected plant's panel, so the selected plant going cancels any aim too
        SkillTargetingManager targeting = SkillTargetingManager.instance;
        if (targeting != null)
        {
            bool selected = PlantUpgradeUI.instance != null && PlantUpgradeUI.instance.GetSelectedPlant() == this;
            if (selected) targeting.CancelAll();
            else targeting.CancelFor(this);
        }
        if (PlantUpgradeUI.instance != null && PlantUpgradeUI.instance.GetSelectedPlant() == this)
            PlantUpgradeUI.instance.HidePanel();
        ReleaseHitFlashMaterial();
        OnPlantRemoved?.Invoke(this);
    }

    // the widget over the plant. the attack and passive cooldown bars show only while it's hovered
    // or selected, like the old ones did; the widget itself shows while hovered, selected, hurt or
    // shielded, or once the skill is unlocked (PlantWidgetStyle decides the last one)
    private void UpdateWidget()
    {
        if (_widget == null) return;
        bool focused = _isSelected || _hoverHighlighted;

        // bars stack in the order they're first added
        if (PassiveBarFirst) { UpdatePassiveBar(focused); UpdateAttackBar(focused); }
        else { UpdateAttackBar(focused); UpdatePassiveBar(focused); }

        float charge = skillCooldown > 0f ? Mathf.Clamp01(1f - skillCooldownTimer / skillCooldown) : 1f;
        _widget.SetSkill(path3Unlocked, charge, skillCooldownTimer <= 0f);

        PlantWidgetStyle style = PlantWidget.Style;
        bool hurt = health < maxHealth || TotalShield > 0f;
        bool skill = path3Unlocked && style != null && style.showWhileSkillUnlocked;
        bool always = style != null && style.alwaysVisible;
        // a plant with mulches always shows its widget, so its rarity squares stay visible like
        // items in teamfight tactics (proposal: the vault only says they sit above the plant)
        bool mulched = mulches.Count > 0;
        _widget.SetVisible(IsAlive && (focused || hurt || skill || always || mulched));
        UpdateMulchMarkers();
    }

    // the rarity squares by the health bar, rebuilt only when what's equipped changes
    private int _mulchMarkerKey = -1;
    private readonly List<Color> _mulchMarkerColors = new List<Color>();

    private void UpdateMulchMarkers()
    {
        int key = mulches.Count;
        foreach (MulchEffect effect in mulches)
            key = key * 4 + (effect != null ? (int)effect.Rarity + 1 : 0);
        if (key == _mulchMarkerKey) return;
        _mulchMarkerKey = key;
        _mulchMarkerColors.Clear();
        foreach (MulchEffect effect in mulches)
            if (effect != null) _mulchMarkerColors.Add(MulchTierColors.Get(effect.Rarity));
        _widget.SetMulchMarkers(_mulchMarkerColors);
    }

    private void UpdateAttackBar(bool focused)
    {
        if (focused && GetAttackBarVisible()) _widget.SetExtraBar("attack", GetAttackBarFill(), AttackBarColor);
        else _widget.RemoveExtraBar("attack");
    }

    private void UpdatePassiveBar(bool focused)
    {
        if (focused && GetPassiveBarVisible()) _widget.SetExtraBar("passive", GetPassiveBarFill(), PassiveBarColor);
        else _widget.RemoveExtraBar("passive");
    }

    protected override void TickHealthWidget(bool shieldJustExpired)
    {
        if (_widget != null) _widget.SetHealth(health, maxHealth, TotalShield, shieldJustExpired);
    }

    [SerializeField] private SpriteRenderer mainRenderer;
    private bool _hoverHighlighted;

    protected override SpriteRenderer GetMainRenderer() => mainRenderer != null ? mainRenderer : base.GetMainRenderer();

    public override void SetHighlight(Color color)
    {
        bool targeting = SkillTargetingManager.instance != null && SkillTargetingManager.instance.IsPlantTargeting;
        if (_isSelected || (_hoverHighlighted && !targeting))
            color = Color.white;
        else if (_hoverHighlighted && targeting)
            color = Color.yellow;
        base.SetHighlight(color);
    }

    public override void ClearHighlight()
    {
        if (_isSelected || _hoverHighlighted) return;
        base.ClearHighlight();
    }

    // every targeting plant (UsesTargeting == true) overrides this with whatever it would
    // currently pick as its target, purely for display - a read-only query, no side effects, so
    // it's safe to poll every frame while this plant is selected. returning null (the default)
    // simply shows no target highlight, so a plant with no meaningful "current single target"
    // concept (e.g. an AoE-only attacker) doesn't need to override it at all
    protected virtual GameObject GetHighlightTarget() => null;

    // > 0 shows a copy of this plant's own range-circle indicator stuck to its currently
    // highlighted target instead of (or alongside) one centered on the plant itself - e.g.
    // LeafRanger sticks his Passive's bounce radius onto whichever insect is currently
    // highlighted/targeted. most plants don't need this (their own range circle already covers
    // what they need to show), so the default is "don't show one". takes the target itself since
    // a flat/ground-plane radius may need to depend on how high that specific target is flying
    protected virtual float GetTargetRangeIndicatorRadius(Insect target) => -1f;

    private Insect _highlightedTarget;
    private Transform _targetRangeIndicator;

    private void UpdateTargetHighlight()
    {
        if (!UsesTargeting) return;

        Insect desired = null;
        if (_isSelected)
        {
            GameObject targetObj = GetHighlightTarget();
            Insect candidate = targetObj != null ? targetObj.GetComponent<Insect>() : null;
            if (candidate != null && candidate.IsAlive) desired = candidate;
        }

        if (_highlightedTarget != desired)
        {
            if (_highlightedTarget != null) _highlightedTarget.ClearHighlight();
            if (desired != null) desired.SetHighlight(Color.red);
            _highlightedTarget = desired;

            if (_targetRangeIndicator != null)
            {
                Destroy(_targetRangeIndicator.gameObject);
                _targetRangeIndicator = null;
            }
            if (desired != null && circleRadius != null && GetTargetRangeIndicatorRadius(desired) > 0f)
            {
                _targetRangeIndicator = Instantiate(circleRadius, desired.transform);
                _targetRangeIndicator.name = "TargetRangeIndicator";
                // the copy would carry this plant's red line of sight area along (a still picture of it)
                Transform copiedShade = _targetRangeIndicator.Find("SightShade");
                if (copiedShade != null) Destroy(copiedShade.gameObject);
                _targetRangeIndicator.localPosition = Vector3.zero;
                SpriteRenderer sr = _targetRangeIndicator.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0f, 1f, 0f, sr.color.a * 0.6f);
                _targetRangeIndicator.gameObject.SetActive(true);
            }
        }

        // keeps the indicator's size current even while the same target stays highlighted - e.g.
        // a Passive level bought mid-selection changes LeafRanger's bounce radius immediately
        if (_targetRangeIndicator != null && _highlightedTarget != null)
        {
            float radius = GetTargetRangeIndicatorRadius(_highlightedTarget);
            _targetRangeIndicator.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        }
    }

    private void ClearTargetHighlight()
    {
        if (_highlightedTarget != null)
        {
            _highlightedTarget.ClearHighlight();
            _highlightedTarget = null;
        }
        if (_targetRangeIndicator != null)
        {
            Destroy(_targetRangeIndicator.gameObject);
            _targetRangeIndicator = null;
        }
    }

    // generic left/right facing for every targeting plant: mirrors GetMainRenderer()'s sprite
    // via flipX toward whatever GetHighlightTarget() currently resolves to, so no separate
    // left/right art is needed. convention: a plant's unflipped art faces LEFT by default, and
    // flipX mirrors it to face right - holds its last facing (no snap to default) when there's
    // no current target, so it doesn't flicker when insects momentarily clear out of range
    // dead zone: the target has to be at least this far to one side before the plant turns, so an
    // insect almost straight above or below it (a melee plant's attacker) can wobble across its
    // middle without flipping it left and right every few frames. 0.15 units is about 5 tile pixels
    private const float FacingFlipThreshold = 0.15f;

    // idle look around: a plant with nothing to face and nothing going on (not stunned,
    // channeling, winding up an attack or playing a skill animation) turns the other way every
    // 4 to 6 seconds, picked at random each time, so it reads as alive. the clock restarts
    // whenever it has a target again
    private const float IdleTurnMinInterval = 4f;
    private const float IdleTurnMaxInterval = 6f;
    private float _idleTurnTimer = -1f;

    // the target it faces is picked again every FacingRetargetInterval, or at once when that target
    // dies or goes: the pick runs the plant's whole target search (every insect, with its range,
    // night and flight checks), which ran every frame on every plant just to turn the sprite. the
    // sprite still follows the picked target every frame, and attacks do their own fresh search
    private const float FacingRetargetInterval = 0.15f;
    private GameObject _facingTarget;
    private Insect _facingInsect;
    private float _facingRetargetTimer;

    private GameObject FacingTarget()
    {
        if (!UsesTargeting) return null;
        _facingRetargetTimer -= Time.deltaTime;
        bool lost = !ReferenceEquals(_facingTarget, null)
            && (_facingTarget == null || !_facingTarget.activeInHierarchy || (_facingInsect != null && !_facingInsect.IsAlive));
        if (lost || _facingRetargetTimer <= 0f)
        {
            _facingRetargetTimer = FacingRetargetInterval;
            _facingTarget = GetHighlightTarget();
            _facingInsect = _facingTarget != null ? _facingTarget.GetComponent<Insect>() : null;
        }
        return _facingTarget;
    }

    private void UpdateFacingFlip()
    {
        GameObject targetObj = FacingTarget();
        if (targetObj == null)
        {
            UpdateIdleTurn();
            return;
        }
        _idleTurnTimer = -1f;
        TurnToward(targetObj, _facingInsect);
    }

    private void TurnToward(GameObject targetObj, Insect insect)
    {
        Vector2 targetPos = insect != null ? insect.GetApproachPoint(transform.position) : (Vector2)targetObj.transform.position;
        float dx = targetPos.x - transform.position.x;
        if (Mathf.Abs(dx) < FacingFlipThreshold) return;

        SpriteRenderer sr = GetMainRenderer();
        if (sr != null) sr.flipX = dx > 0f;
    }

    // the safety net for the facing delay (Tommy, 2026-11-23): whatever a plant actually attacks, it
    // faces at once and keeps facing until its next pick, even when the last 0.15 s pick chose
    // something else or nothing. every attack path calls this with its target
    protected void FaceAttackTarget(GameObject target)
    {
        if (target == null || !UsesTargeting) return;
        _facingTarget = target;
        _facingInsect = target.GetComponent<Insect>();
        _facingRetargetTimer = FacingRetargetInterval;
        _idleTurnTimer = -1f;
        TurnToward(target, _facingInsect);
    }

    private void UpdateIdleTurn()
    {
        bool busy = IsStunned || IsChanneling || _attackWindupRoutine != null || Time.time < _bodyInterruptUntil;
        if (busy) return;

        if (_idleTurnTimer < 0f) _idleTurnTimer = Random.Range(IdleTurnMinInterval, IdleTurnMaxInterval);
        _idleTurnTimer -= Time.deltaTime;
        if (_idleTurnTimer > 0f) return;

        SpriteRenderer sr = GetMainRenderer();
        if (sr != null) sr.flipX = !sr.flipX;
        _idleTurnTimer = Random.Range(IdleTurnMinInterval, IdleTurnMaxInterval);
    }

    // lets another plant show/hide THIS plant's own range circle (e.g. Carrot highlighting its
    // Soil Bond target while Carrot itself is selected) without marking this plant as selected -
    // Select()/Deselect() are reserved for this plant's own actual selection state
    public void ShowExternalRangeCircle(bool show)
    {
        if (ShowRangeCircle && circleRadius != null)
            circleRadius.gameObject.SetActive(show);
    }

    protected override void Start()
    {
        base.Start();
        GetMainRenderer();
        // the widget measures this plant's own sprite to sit just above it, not at one fixed height
        if (_widget != null)
            _widget.FollowSpriteTop(GetMainRenderer(), data != null ? data.widgetHeightOffset : 0);

        // subscribe to weather changes and apply all currently active weather effects
        WeatherManager.OnWeatherAdded   += HandleWeatherAdded;
        WeatherManager.OnWeatherRemoved += HandleWeatherRemoved;
        if (WeatherManager.instance != null)
            foreach (WeatherEntry entry in WeatherManager.instance.GetActiveWeather())
                ApplyWeatherEffect(entry.type, entry.intensity);

        if (ShowRangeCircle && circleRadius != null)
        {
            circleRadius.gameObject.SetActive(false);
            if (!ShowDarkCircle) return;
            darkCircleRadius = Instantiate(circleRadius, circleRadius.parent);
            darkCircleRadius.name = "DarkCircleRadius";
            SpriteRenderer sr = darkCircleRadius.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.85f, 0f, sr.color.a);
            darkCircleRadius.gameObject.SetActive(false);
        }
    }

    private void HandleWeatherAdded(WeatherType type, int intensity)
    {
        // remove any existing effect for this type first (intensity may have changed)
        RemoveWeatherEffect(type);
        ApplyWeatherEffect(type, intensity);
    }

    private void HandleWeatherRemoved(WeatherType type)
    {
        RemoveWeatherEffect(type);
    }

    private void ApplyWeatherEffect(WeatherType w, int intensity)
    {
        switch (w)
        {
            case WeatherType.Sunny:      ApplyEffect(new SunlightExposedEffect(this, this, intensity)); break;
            case WeatherType.Rain:       ApplyEffect(new RainExposedEffect(this, this, intensity));     break;
            case WeatherType.Snow:       ApplyEffect(new SnowExposedEffect(this, this, intensity));    break;
            case WeatherType.Underwater: ApplyEffect(new SubmergedEffect(this, this, intensity));      break;
            case WeatherType.Sandstorm:  ApplyEffect(new SandstormExposedEffect(this, this, intensity)); break;
        }
    }

    private void RemoveWeatherEffect(WeatherType w)
    {
        switch (w)
        {
            case WeatherType.Sunny:      RemoveEffect<SunlightExposedEffect>(); break;
            case WeatherType.Rain:       RemoveEffect<RainExposedEffect>();     break;
            case WeatherType.Snow:       RemoveEffect<SnowExposedEffect>();    break;
            case WeatherType.Underwater: RemoveEffect<SubmergedEffect>();      break;
            case WeatherType.Sandstorm:  RemoveEffect<SandstormExposedEffect>(); break;
        }
    }

    protected override void OnHover()
    {
        if (ShowRangeCircle && circleRadius != null)
            circleRadius.gameObject.SetActive(true);
        if (darkCircleRadius != null && DarknessManager.instance != null && DarknessManager.instance.isDark)
            darkCircleRadius.gameObject.SetActive(true);
        _hoverHighlighted = true;
        SetHighlight(Color.white);
    }

    protected override void OnHoverExit()
    {
        _hoverHighlighted = false;
        ClearHighlight();
        if (_isSelected) return;
        if (ShowRangeCircle && circleRadius != null)
            circleRadius.gameObject.SetActive(false);
        if (darkCircleRadius != null)
            darkCircleRadius.gameObject.SetActive(false);
        RefreshHealthBarVisibility();
    }

    public void Select()
    {
        _isSelected = true;
        SetHighlight(Color.white);
        if (ShowRangeCircle && circleRadius != null)
            circleRadius.gameObject.SetActive(true);
        if (darkCircleRadius != null && DarknessManager.instance != null && DarknessManager.instance.isDark)
            darkCircleRadius.gameObject.SetActive(true);
    }

    public void Deselect()
    {
        _isSelected = false;
        ClearHighlight();
        ClearTargetHighlight();
        if (ShowRangeCircle && circleRadius != null)
            circleRadius.gameObject.SetActive(false);
        if (darkCircleRadius != null)
            darkCircleRadius.gameObject.SetActive(false);
        RefreshHealthBarVisibility();
    }

    protected override void Update()
    {
        base.Update();
        RecomputeEffectivePathLevels();
        UpdateTargetHighlight();
        UpdateFacingFlip();
        UpdateSightShade();

        if (_light2D != null)
        {
            if (lightEmissionRange != _lastLightEmissionRange)
            {
                if (lightEmissionRange > 0f && _lastLightEmissionRange <= 0f)
                {
                    // turning on from fully off: pop the radius out, then fade intensity up from black
                    _light2D.pointLightOuterRadius = lightEmissionRange;
                    _light2D.pointLightInnerRadius = ComputeLightInnerRadius(lightEmissionRange);
                    _lightFader?.FadeIn(1f);
                }
                else if (lightEmissionRange <= 0f)
                {
                    // turning off: fade the light out in place, radius stays as-is
                    _lightFader?.FadeOut(1f);
                }
                else
                {
                    // already lit and resizing (e.g. Floral Glow changing an already-illuminated
                    // plant's range): fade the old radius out, swap it, fade the new one back in
                    _lightFader?.CrossFadeTo(lightEmissionRange, ComputeLightInnerRadius(lightEmissionRange));
                }

                _lastLightEmissionRange = lightEmissionRange;
            }
        }

        if (passiveCooldownTimer > 0)
            passiveCooldownTimer -= Time.deltaTime;
        if (skillCooldownTimer > 0)
            skillCooldownTimer -= Time.deltaTime * skillChargeRate;
        if (transplantCooldownTimer > 0)
            transplantCooldownTimer -= Time.deltaTime;

        UpdateWidget();

        if (_hoverHighlighted)
            SetHighlight(Color.white);

        UpdateTemperature();
        UpdateAir();
    }

    private float airDamageTimer;
    private int airDamageTicks;   // consecutive ticks spent at 0 Air, resets on recovery

    // Oxygen equivalent of Heal(): scales by this plant's own Respiration internally, and
    // returns the (Respiration-scaled) amount actually provided, so callers that track
    // cumulative Oxygen handed out (e.g. Kelp's Sun production) can accumulate the real total
    public float ReplenishOxygen(float amount)
    {
        float scaled = amount * (1f + respiration);
        air = Mathf.Min(air + scaled, 100f);
        return scaled;
    }

    // mirrors UpdateTemperature: only relevant while actually Submerged (Underwater weather).
    // an active IRespirationEffect flips depletion into regeneration at that effect's own rate,
    // instead of just slowing the drain the way Respiration (the resistance stat) does
    private void UpdateAir()
    {
        if (!HasEffect<SubmergedEffect>()) return;

        IRespirationEffect bestRespiration = null;
        foreach (StatusEffect e in activeEffects)
        {
            if (e is IRespirationEffect r && (bestRespiration == null || r.RespirationRegenPerSecond > bestRespiration.RespirationRegenPerSecond))
                bestRespiration = r;
        }

        bool inAirBubble = occupiedTile != null && occupiedTile.isAirBubble;

        if (bestRespiration != null)
        {
            // the Respiration stat also boosts regen while an IRespirationEffect is active,
            // not just the depletion mitigation
            float regen = bestRespiration.RespirationRegenPerSecond * (1f + respiration) * Time.deltaTime;
            air = Mathf.Min(air + regen, 100f);
            bestRespiration.OnOxygenGranted(regen);
        }
        else if (!inAirBubble)
            air = Mathf.Max(air - 10f * Mathf.Max(0f, 1f - respiration) * Time.deltaTime, 0f);

        // same ramping rate/formula as running out of comfort on temperature, just Wind True damage
        if (air <= 0f)
        {
            airDamageTimer += Time.deltaTime;
            if (airDamageTimer >= 2f)
            {
                airDamageTimer = 0f;
                airDamageTicks++;
                float dmg = maxHealth * 0.03f * 2f + maxHealth * TemperatureDamageRampPerTick * (airDamageTicks - 1);
                Damage(dmg, DamageType.True, ElementalType.Wind, temperatureDamageTags);
                DamageIndicator.Spawn(GetIndicatorPosition(), dmg, ElementalType.Wind, false);
            }
        }
        else
        {
            airDamageTimer = 0f;
            airDamageTicks = 0;
        }
    }

    private static readonly DamageTag[] temperatureDamageTags = { DamageTag.Weather };
    private float temperatureDamageTimer;
    private int temperatureDamageTicks;   // consecutive ticks spent out of comfort, resets on return
    private const float TemperatureDamageRampPerTick = 0.02f;  // +2% max health damage per tick

    private void UpdateTemperature()
    {
        if (WeatherManager.instance != null)
        {
            switch (WeatherManager.instance.temperature)
            {
                case TemperatureType.Hot:
                    bool rainCoolsHeat = WeatherManager.instance.GetIntensity(WeatherType.Rain) >= 5
                                      || WeatherManager.instance.GetIntensity(WeatherType.Snow) >= 5;
                    if (rainCoolsHeat)
                        temperature = Mathf.Max(temperature - 1f * Time.deltaTime, comfortMin);
                    else
                        // Heat Resistance only slows the heat-driven rise, not the recovery above
                        temperature += 1f * Mathf.Max(0f, 1f - heatResistance) * Time.deltaTime;
                    break;
                case TemperatureType.Cold:
                    bool sunWarmsFreeze = WeatherManager.instance.GetIntensity(WeatherType.Sunny) >= 5;
                    if (sunWarmsFreeze)
                        temperature = Mathf.Min(temperature + 1f * Time.deltaTime, comfortMax);
                    else
                        // Cold Resistance only slows the cold-driven drop, not the recovery above
                        temperature -= 1f * Mathf.Max(0f, 1f - coldResistance) * Time.deltaTime;
                    break;
                default:
                    // Warm/Normal/Chill aren't extreme enough to drive Temperature at all: force it
                    // back into Comfort instead of leaving it stuck wherever a prior Hot/Cold left it
                    temperature = Mathf.Clamp(temperature, comfortMin, comfortMax);
                    break;
            }
        }
        temperature = Mathf.Clamp(temperature, temperatureMin, temperatureMax);

        if (WeatherManager.instance != null && WeatherManager.instance.temperature == TemperatureType.Hot
            && occupiedTile != null && (occupiedTile.tileType == TileType.Water || occupiedTile.isWaterAdjacent))
            temperature = Mathf.Min(temperature, 10f);

        if (WeatherManager.instance != null && WeatherManager.instance.temperature == TemperatureType.Cold
            && occupiedTile != null && (occupiedTile.isHeatSource || occupiedTile.isHeatAdjacent))
            temperature = Mathf.Max(temperature, 10f);

        bool tooCold = temperature < comfortMin;
        bool tooHot  = temperature > comfortMax;

        if (tooCold || tooHot)
        {
            temperatureDamageTimer += Time.deltaTime;
            if (temperatureDamageTimer >= 2f)
            {
                temperatureDamageTimer = 0f;
                temperatureDamageTicks++;
                // first tick is the base rate, each tick after ramps up another 2% max health,
                // rewarding regulating plants that return the target to comfort before it stacks
                float dmg = maxHealth * 0.03f * 2f + maxHealth * TemperatureDamageRampPerTick * (temperatureDamageTicks - 1);
                ElementalType dmgElement = tooCold ? ElementalType.Ice : ElementalType.Fire;
                Damage(dmg, DamageType.True, dmgElement, temperatureDamageTags);
                DamageIndicator.Spawn(GetIndicatorPosition(), dmg, dmgElement, false);
            }
        }
        else
        {
            temperatureDamageTimer = 0f;
            temperatureDamageTicks = 0;
        }
    }

    public virtual void ActivateSkill() {}

    // an echo seed repeating one of this plant's attacks: the on hit effects that aren't debuffs
    // (waterlily's splash) happen again. the mulch repeats the debuffs itself. target can be dead or
    // gone by now, groundPoint and aimPoint are where it was when the attack hit
    public virtual void RepeatOnHitEffects(Insect target, Vector3 groundPoint, Vector3 aimPoint) {}

    // every caller (UI button/hotkey, auto-cast plants) should call this instead of ActivateSkill()
    // directly, so the skill sound plays uniformly regardless of which plant/path triggers it -
    // ActivateSkill() itself stays the per-plant override point for the actual skill behaviour
    public void TriggerSkill()
    {
        if (data != null) SfxPlayer.Play(data.skillSound, transform.position);
        ActivateSkill();
    }

    private void RecomputeEffectivePathLevels()
    {
        effectivePath1Level = path1Level + path1LevelAdder;
        effectivePath2Level = path2Level + path2LevelAdder + GetWeatherPath2Bonus();
        effectivePath3Level = path3Level + path3LevelAdder;
    }

    private int GetWeatherPath2Bonus()
    {
        if (elementalType == ElementalType.Fire && HasEffect<SunlightExposedEffect>())
            return 1;

        if (elementalType == ElementalType.Water)
        {
            bool onWater = occupiedTile != null && (occupiedTile.tileType == TileType.Water || occupiedTile.isWaterAdjacent);
            if (onWater || HasEffect<RainExposedEffect>() || HasEffect<SubmergedEffect>()) return 1;
        }

        if (elementalType == ElementalType.Grass && occupiedTile != null && occupiedTile.tileType == TileType.Grass)
            return 1;

        if (elementalType == ElementalType.Wind && occupiedTile != null && occupiedTile.isHighground)
            return 1;

        if (elementalType == ElementalType.Ice && HasEffect<SnowExposedEffect>())
            return 1;

        // moonlight: dark enough for night to matter, but not the deeper pitch-black state -
        // rain clouds block the moon out, so this doesn't count while it's raining
        if (elementalType == ElementalType.Poison && DarknessManager.instance != null
            && DarknessManager.instance.isDark && !DarknessManager.instance.pitchBlack
            && (WeatherManager.instance == null || !WeatherManager.instance.HasWeather(WeatherType.Rain)))
            return 1;

        return 0;
    }

    public void Uproot()
    {
        float refundRate = GameManager.instance.currentWave == 0 ? 1f : 0.5f;
        GameManager.instance.SunCount += (int)(totalSunSpent * refundRate);
        Debug.Log("Uprooted " + GetName() + " and refunded " + (int)(totalSunSpent * refundRate));
        GameManager.instance.UpdateSun();
        BankExp();
        if (data.plantName == "AcornKnight" && occupiedTile.tileType == TileType.Dirt)
            TutorialManager.instance?.NotifyEvent(TutorialEvents.UprootAcornKnightFromDirt);
        // need some sound effects eventually
        occupiedTile.isOccupied = false;
        occupiedTile.GetComponent<Collider2D>().enabled = true;
        PlantSelector.instance.SetUprootMode(false);
        PlantUpgradeUI.instance.HidePanel();
        DetachAndFadeLight();
        Destroy(gameObject);
    }

    // Transplant mulch: moves this exact instance (same stats/levels/active effects) onto a new
    // tile, free of charge - unlike Uproot, nothing is destroyed or refunded. Tile.OnMouseDown
    // already validated destination.tileType against allowedTiles and that it's unoccupied before
    // calling this, same checks placement itself uses
    public void TriggerTransplant(Tile destination)
    {
        if (!TransplantReady || occupiedTile == null || destination == null || destination == occupiedTile) return;

        occupiedTile.isOccupied = false;
        occupiedTile.GetComponent<Collider2D>().enabled = true;

        // same as a fresh placement onto a tile that held a ghost - it can no longer be revived
        destination.deadPlant = null;
        destination.isOccupied = true;
        destination.GetComponent<Collider2D>().enabled = false;

        transform.position = destination.transform.position;
        occupiedTile = destination;
        ApplyHighgroundBonus(destination.isHighground);

        transplantCooldownTimer = TransplantFertilizerEffect.CooldownSeconds;
    }

    public void GainExp(int amount)
    {
        int gained = (int)(amount * (1 + expBoost));
        exp += gained;
    }

    // exp banked mid-level (via Uproot, or a level ending) but not yet committed to the
    // persistent save - only CommitPendingExpToSave() (called on an actual level win) writes this
    // into saveData, so a game over losing mid-level simply discards whatever's sitting here
    private static readonly Dictionary<string, int> pendingLevelExp = new Dictionary<string, int>();

    // clears any exp left over from a previous attempt at this level (e.g. after a game over and
    // retry) - called once per level load, see ProceduralLevel.Start
    public static void ResetPendingExpForNewLevel() => pendingLevelExp.Clear();

    // the exp a plant type has earned this level so far, banked or still on its plants (wisdom
    // leaf's tooltip). wisdom leaf itself is only added at level end
    public static int LevelExpSoFar(string plantName)
    {
        pendingLevelExp.TryGetValue(plantName, out int total);
        foreach (Plant plant in allPlants)
            if (plant != null && plant.data != null && plant.data.plantName == plantName) total += plant.exp;
        return total;
    }

    // banks this plant's session exp into pendingLevelExp, staged until the level is actually won
    private void BankExp()
    {
        if (exp <= 0 || data == null) return;
        pendingLevelExp.TryGetValue(data.plantName, out int current);
        pendingLevelExp[data.plantName] = current + exp;
        exp = 0;
    }

    // exp only ever becomes real (persistent) here, and this is only ever called on a completed
    // level (see ProceduralLevel.RunWaves) - never on a game over, so exp earned in a lost level
    // (including any banked mid-level via Uproot) never reaches the save file
    public static void CommitPendingExpToSave()
    {
        Debug.Log($"[Exp] Level complete - total exp gained per plant this level ({pendingLevelExp.Count} plant type(s)):");
        foreach (var kvp in pendingLevelExp)
            Debug.Log($"[Exp]   {kvp.Key}: {kvp.Value} exp");

        // wisdom leaf: a plant type's whole exp for the level grows by every wisdom leaf equipped on a
        // living plant of that type, added together. applied once here, at level end, since exp is
        // a whole number and a small percent per kill would round away
        foreach (string plantName in new List<string>(pendingLevelExp.Keys))
        {
            float wisdom = MulchManager.WisdomBonusFor(plantName);
            if (wisdom <= 0f) continue;
            int boosted = Mathf.RoundToInt(pendingLevelExp[plantName] * (1f + wisdom));
            Debug.Log($"[Exp]   {plantName}: wisdom leaf +{wisdom * 100f:F0}% ({pendingLevelExp[plantName]} to {boosted})");
            pendingLevelExp[plantName] = boosted;
        }

        if (SaveManager.instance != null)
            foreach (var kvp in pendingLevelExp)
                SaveManager.instance.saveData.AddPlantExp(kvp.Key, kvp.Value);
        pendingLevelExp.Clear();
    }

    // called once from ProceduralLevel when a level completes, so plants that survive to the
    // end still bank their exp (Uproot banks it individually, see Uproot() above). also sweeps
    // dead-but-not-yet-revived ghosts' stored exp, since Kill() deliberately leaves it on the
    // DeadPlantRecord (in case of revival) rather than banking it immediately
    public static void BankAllExpForLevelEnd()
    {
        foreach (Plant plant in new List<Plant>(allPlants))
            plant?.BankExp();

        foreach (var kvp in Tile.allTiles)
        {
            DeadPlantRecord record = kvp.Value.deadPlant;
            if (record == null || record.exp <= 0 || record.prefab == null) continue;
            PlantData recordData = record.prefab.GetComponent<Plant>()?.data;
            if (recordData == null) continue;
            pendingLevelExp.TryGetValue(recordData.plantName, out int current);
            pendingLevelExp[recordData.plantName] = current + record.exp;
            record.exp = 0;
        }
    }

    public virtual void OnPath1Upgrade(int level) {}
    public virtual void OnPath2Upgrade(int level) {}
    public virtual void OnPath3Unlock() {}
    public virtual void OnPath3Upgrade(int level) {}

    // UPGRADE COSTS
    // per level cap set by ProceduralLevel from LevelConfig, default 5
    public static int pathLevelCap = 5;
    // absolute cap, max level bonuses always require this regardless of the level cap
    public const int absoluteLevelCap = 5;

    public bool IsPath1Maxed => path1Level >= absoluteLevelCap;
    public bool IsPath2Maxed => path2Level >= absoluteLevelCap;
    public bool IsPath3Maxed => path3Level >= absoluteLevelCap;

    // upgrade cost scales with the plant's own suncost so premium plants cost
    // proportionally more to scale. base is the first level price as a fraction
    // of buy cost, step is the exponential multiplier/Lvl.
    const float upgradeBaseFactor = 0.25f;
    const float upgradeStepFactor = 2.1f;
    const float skillBaseFactor   = 0.5f;

    // FertilizerManager.UpgradeCostMultiplier is market day's discount, so the shown and charged cost
    // both include it
    int PathCost(int level, float baseFactor) =>
        Mathf.RoundToInt(sunCost * baseFactor * Mathf.Pow(upgradeStepFactor, level) * Mathf.Max(0f, FertilizerManager.UpgradeCostMultiplier));

    public int GetPath1Cost() => PathCost(path1Level, upgradeBaseFactor);
    public int GetPath2Cost() => PathCost(path2Level, upgradeBaseFactor);
    public int GetPath3Cost() => PathCost(path3Level, skillBaseFactor);

    public bool UpgradePath1()
    {
        if (path1Level >= pathLevelCap) return false;
        if (!GameManager.instance.SpendSun(GetPath1Cost())) return false;
        totalSunSpent += GetPath1Cost();
        path1Level++;
        effectivePath1Level = path1Level + path1LevelAdder;
        OnPath1Upgrade(effectivePath1Level);
        return true;
    }

    public bool UpgradePath2()
    {
        if (path2Level >= pathLevelCap) return false;
        if (!GameManager.instance.SpendSun(GetPath2Cost())) return false;
        totalSunSpent += GetPath2Cost();
        path2Level++;
        effectivePath2Level = path2Level + path2LevelAdder + GetWeatherPath2Bonus();
        OnPath2Upgrade(effectivePath2Level);
        return true;
    }

    public bool UnlockPath3()
    {
        if (path3Unlocked) return false;
        if (!GameManager.instance.SpendSun(GetPath3Cost())) return false;
        totalSunSpent += GetPath3Cost();
        path3Unlocked = true;
        OnPath3Unlock();
        return true;
    }

    public bool UpgradePath3()
    {
        if (!path3Unlocked) return false;
        if (path3Level >= pathLevelCap) return false;
        if (!GameManager.instance.SpendSun(GetPath3Cost())) return false;
        totalSunSpent += GetPath3Cost();
        path3Level++;
        effectivePath3Level = path3Level + path3LevelAdder;
        OnPath3Upgrade(effectivePath3Level);
        return true;
    }

    // grants one free level of every path (unlocking Path3 if needed) without spending sun or
    // touching totalSunSpent - used by skill tree "instant" capstone nodes. call at most once per
    // plant instance (from Awake, before UpdateStats), since it directly increments the level
    // counters rather than going through the sun-spending Upgrade methods above
    public void GrantFreePathLevels()
    {
        if (path1Level < pathLevelCap)
        {
            path1Level++;
            effectivePath1Level = path1Level + path1LevelAdder;
            OnPath1Upgrade(effectivePath1Level);
        }
        if (path2Level < pathLevelCap)
        {
            path2Level++;
            effectivePath2Level = path2Level + path2LevelAdder + GetWeatherPath2Bonus();
            OnPath2Upgrade(effectivePath2Level);
        }
        if (!path3Unlocked)
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }
        if (path3Level < pathLevelCap)
        {
            path3Level++;
            effectivePath3Level = path3Level + path3LevelAdder;
            OnPath3Upgrade(effectivePath3Level);
        }
    }

    // the growth mulches: levels added to one path's effective level (1 attack, 2 passive, 3 skill),
    // not its point level, so the cost of the next point and the uproot refund don't change. the
    // path's hook runs again at the new effective level, the same way LoadData's skill tree adders
    // are baked in. a locked skill path gets the levels too, and they show once it's unlocked
    public void AddPathLevelBonus(int path, int delta)
    {
        if (delta == 0) return;
        switch (path)
        {
            case 1: path1LevelAdder += delta; break;
            case 2: path2LevelAdder += delta; break;
            case 3: path3LevelAdder += delta; break;
            default: return;
        }
        RecomputeEffectivePathLevels();
        switch (path)
        {
            case 1: OnPath1Upgrade(effectivePath1Level); break;
            case 2: OnPath2Upgrade(effectivePath2Level); break;
            case 3: OnPath3Upgrade(effectivePath3Level); break;
        }
    }

    // FIELD SELECTION
    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (SkillTargetingManager.instance.WasPlantCancelledThisFrame) return;
        if (SkillTargetingManager.instance.IsPlantTargeting) { SkillTargetingManager.instance.ConfirmPlantTarget(this); return; }
        if (SkillTargetingManager.instance.IsTargeting) return;
        if (PlantSelector.instance.uprootMode)
        {
            Uproot();
            return;
        }
        if (data.plantName == "AcornKnight")
            TutorialManager.instance?.NotifyEvent(TutorialEvents.ClickPlacedAcornKnight);
        PlantUpgradeUI.instance.ShowPanel(this);
    }

    public float ScaleCC(float baseDuration) => baseDuration + immobilizeDurationAdder + baseDuration * immobilizeDurationMultiplier;

    // PATH NAMES & HOVER DESCRIPTIONS
    public virtual PlantBaseStats GetBaseStats() => data == null ? default : new PlantBaseStats
    {
        attackDamage    = data.baseAttackDamage,
        attackSpeed     = data.baseAttackSpeed,
        attackRange     = data.baseAttackRange,
        skillCooldown   = data.baseSkillCooldown,
        passiveCooldown = data.basePassiveCooldown,
        piercing        = data.basePiercing,
    };

    public virtual string GetPath1Name() => "Attack";
    public virtual string GetPath2Name() => "Passive";
    public virtual string GetPath3Name() => "Skill";
    public virtual string GetPath1Description(bool details = false) => "";
    public virtual string GetPath2Description(bool details = false) => "";
    public virtual string GetPath3Description(bool details = false) => "";

    protected static string ShiftHint(bool details) =>
        details ? "<color=grey>[Release SHIFT for overview]</color>"
                : "<color=grey>[Hold SHIFT for details]</color>";

    protected string SkillCooldownLine() => $"Cooldown: <b>{Mathf.RoundToInt(skillCooldown)}s</b>";

    // where an Attack, Passive or Skill text's sections start (Tommy, 2026-11-26): the upgrades (each plant puts
    // UpgradesBreak after its description) and the max level bonus (Level5Section starts with MaxLevelBreak).
    // the plant info panel cuts the text there into framed boxes (PlantUpgradeUI.ShowTooltip); both are zero
    // width characters, so a text shown whole reads as before
    public const string UpgradesBreak = "\u200B";
    public const string MaxLevelBreak = "\u2060";

    protected string Level5Section(int pathLevel, string bonusText = null)
    {
        bool unlocked = pathLevel >= absoluteLevelCap;
        string header = $"{MaxLevelBreak}<color={(unlocked ? "green" : "grey")}><b>[Max Level Bonus]</b></color>";
        if (bonusText == null) return header;
        string body = unlocked
            ? bonusText
            : $"<color=grey>{System.Text.RegularExpressions.Regex.Replace(bonusText, "</?color[^>]*>", "")}</color>";
        return $"{header}\n\n{body}";
    }
    public virtual string GetElement() => PlantData.ElementalTag(data != null ? data.elementalType : elementalType);

    public virtual string GetElementDescription()
    {
        switch (data != null ? data.elementalType : elementalType)
        {
            case ElementalType.Fire:
            return $"Increase Passive tree level by <color=green>1</color> when exposed to sunlight\n50% Heat Resistance";

            case ElementalType.Grass:
            return $"Can be placed on <color=green>Grass</color>.\nIncrease Passive tree level by <color=green>1</color> when placed on Grass";

            case ElementalType.Water:
            return $"Increase Passive tree level by <color=green>1</color> when near natural water";

            case ElementalType.Poison:
            return $"Increase Passive tree level by <color=green>1</color> when exposed to moonlight (dark, but not pitch black, and not raining)";

            case ElementalType.Ice:
            return $"Increase Passive tree level by <color=green>1</color> when exposed to snow\n50% Cold Resistance";

            case ElementalType.Wind:
            return $"Increase Passive tree level by <color=green>1</color> when in high altitude";

            default:
            return "";
        }
    }

    // DESCRIPTIONS
    public virtual string GetName() => DisplayName;

    // the plant's name as players see it (Tommy, 2026-11-26): PlantData.displayName. every plant's title
    // and every description that names a plant read it, so renaming a plant in its data renames it
    // everywhere (Rhodiola is Rhodiola Rosea). plantName stays the save key
    public string DisplayName => data != null && !string.IsNullOrEmpty(data.displayName) ? data.displayName : GetType().Name;

    // the title the info panel and tooltips show: the display name, bold, in the plant's colour
    protected string TitleName(string color) => $"<b><color={color}>{DisplayName}</color></b>";

    // another plant's display name by its save key (plantName), from Resources/PlantRegistry, for a
    // description that mentions a plant it doesn't hold. falls back to the key
    private static PlantRegistry _nameRegistry;
    public static string DisplayNameOf(string plantName)
    {
        if (_nameRegistry == null) _nameRegistry = Resources.Load<PlantRegistry>("PlantRegistry");
        if (_nameRegistry != null && _nameRegistry.plants != null)
            foreach (PlantData d in _nameRegistry.plants)
                if (d != null && d.plantName == plantName && !string.IsNullOrEmpty(d.displayName)) return d.displayName;
        return plantName;
    }

    public virtual string GetDescription()
    {
        return "";
    }

    public virtual string GetAttackDescription()
    {
        return "";
    }

    public virtual string GetSkillDesription()
    {
        return "";
    }

    public virtual string GetPassiveDescription()
    {
        return "";
    }

    private void DetachAndFadeLight()
    {
        if (_light2D == null) return;
        GameObject lightObj = _light2D.gameObject;
        // the light may already be disabled (see Update()'s own FadeOut when lightEmissionRange
        // drops to 0, which leaves the object inactive rather than destroyed so it can fade back
        // in later) - a coroutine can't start on an inactive object, so it has to be reactivated
        // here before handing it a fresh FadeOut, or this whole detach-and-destroy silently no-ops
        lightObj.SetActive(true);
        lightObj.transform.SetParent(null);
        LightFader fader = _lightFader ?? lightObj.AddComponent<LightFader>();
        fader.SetupForFadeOut(_light2D);
        fader.FadeOut(0.3f, destroyOnComplete: true);
        _light2D = null;
        _lightFader = null;
    }

    public override void Kill()
    {
        // exp is NOT banked here - StoreDeadRecord keeps it on the DeadPlantRecord in case this
        // plant is revived, and a revived plant needs its exp intact rather than zeroed out
        StoreDeadRecord();
        MulchManager.instance?.OnPlantDied(this, occupiedTile != null ? occupiedTile.deadPlant : null);
        FreeTile();
        DetachAndFadeLight();
        base.Kill();
    }

    public override void Kill(Entity source)
    {
        StoreDeadRecord();
        MulchManager.instance?.OnPlantDied(this, occupiedTile != null ? occupiedTile.deadPlant : null);
        FreeTile();
        DetachAndFadeLight();
        base.Kill(source);
    }

    private void FreeTile()
    {
        if (occupiedTile == null) return;
        occupiedTile.isOccupied = false;
        Collider2D tileCol = occupiedTile.GetComponent<Collider2D>();
        if (tileCol != null) tileCol.enabled = true;
    }

    // IAttackable
    // a distance the approach point can't be closer than (its collider's box around the center), so a
    // range check can rule a plant out before GetApproachPoint's physics query (Insect's plant pick)
    public float ApproachDistanceAtLeast(Vector3 from)
    {
        if (_circleCollider == null) return Vector2.Distance(from, transform.position);
        Bounds box = _circleCollider.bounds;
        return Mathf.Max(0f, Vector2.Distance(from, box.center) - box.extents.magnitude);
    }

    public Vector3 GetApproachPoint(Vector3 from)
    {
        if (_circleCollider != null)
            return Physics2D.ClosestPoint(from, _circleCollider);
        return transform.position;
    }

    private static readonly DamageTag[] MeleeHitTags = { DamageTag.Melee, DamageTag.Attack };

    public bool ReceiveAttack(float damage, Insect attacker)
    {
        float missChance = Mathf.Clamp01(evasion - attacker.accuracy);
        if (UnityEngine.Random.value < missChance)
        {
            StatusIndicator.Spawn(GetIndicatorPosition(), "Miss", new Color(0.55f, 0.6f, 0.75f));
            return false;
        }
        Damage(damage, attacker.attackDamageType, attacker.attackElementalType, attacker, false, MeleeHitTags);
        OnHitByInsect(attacker);
        return true;
    }

    protected virtual void OnHitByInsect(Insect attacker) {}
    public bool IsAlive => health > 0;
    public Vector3 Position => transform.position;

    // skill-tree assist credit for buffs/shields/heals, pulled in by Insect.RegisterAttacker
    // whenever this plant lands an attack or debuff. buffs/shields are NOT stored here at all -
    // they only count if still active at the moment of attack, so they're checked live against
    // activeEffects (see BuffSupporters below). heals are different: the heal itself is instant,
    // so instead we remember who healed us and for how long the assist window stays open
    private const float HealAssistWindow = 8f;
    private readonly List<HealRecord> recentHeals = new List<HealRecord>();

    private struct HealRecord
    {
        public Plant healer;
        public float time;
    }

    public void RegisterHeal(Plant healer)
    {
        if (healer == null || healer == this) return;
        recentHeals.RemoveAll(h => Time.time - h.time > HealAssistWindow);
        recentHeals.Add(new HealRecord { healer = healer, time = Time.time });
    }

    // every plant that counts as an assist on this plant's hit (buffing or shielding it right now, or
    // healed it within HealAssistWindow), added straight into the set: Insect.RegisterAttacker runs on
    // every hit, and walking the two properties below made two new enumerators each time
    public void AddAssistersTo(HashSet<Plant> set)
    {
        foreach (StatusEffect e in activeEffects)
            if (e.effectType == StatusEffect.Type.positive && e.source is Plant p && p != this)
                set.Add(p);
        foreach (HealRecord h in recentHeals)
            if (Time.time - h.time <= HealAssistWindow)
                set.Add(h.healer);
    }

    public IEnumerable<Plant> RecentHealers
    {
        get
        {
            foreach (HealRecord h in recentHeals)
                if (Time.time - h.time <= HealAssistWindow)
                    yield return h.healer;
        }
    }

    // buffs/shields (StatusEffect.Type.positive) currently active on this plant, sourced from
    // other plants - only counts as an assist while the effect is still up, unlike heals above
    public IEnumerable<Plant> BuffSupporters
    {
        get
        {
            foreach (StatusEffect e in activeEffects)
                if (e.effectType == StatusEffect.Type.positive && e.source is Plant p && p != this)
                    yield return p;
        }
    }

    // generates sun scaled by this plant's sunYieldMultiplier, rounded up (sun can't be decimal),
    // and pops the +Sun indicator at indicatorPosition (defaults to this plant's own position,
    // e.g. Aeonium granting sun via a nearby plant's Blessing passes that plant's position instead)
    // fromKill marks sun paid for a kill (glowshroom) rather than this plant's own production, for
    // mulches that only care about one of the two. mulchSunProductionBonus is this plant's drought
    // mulch, its pause and its boost; nothing is banked or shown while it sits at zero. public for
    // the sun sap mulch, which makes any plant a small sun producer
    public int GenerateSun(int amount, Vector3? indicatorPosition = null, bool fromKill = false)
    {
        int total = Mathf.CeilToInt(amount * (1f + sunYieldMultiplier) * Mathf.Max(0f, 1f + mulchSunProductionBonus));
        if (total <= 0) return 0;
        GameManager.instance?.AddSun(total);
        SunIndicator.Spawn((indicatorPosition ?? transform.position) + new Vector3(0.25f, 0.5f, 0f), total);
        MulchManager.NotifySunGenerated(this, total, fromKill);
        return total;
    }

    // hook for plants that can validly target insects beyond their own circle (e.g. Carrot's
    // Soil Bond, which shares its bonded ally's range - a second circle centered on the ally,
    // not just a bigger one centered on Carrot). default: the standard single-circle check
    protected virtual bool IsWithinAttackRange(Insect insect, float distance) => distance <= attackRange;

    // forcedPriorityTarget short-circuit shared by every Find* method below - null for virtually
    // every plant, so this is a cheap no-op in the common case
    private GameObject ForcedPriorityTargetOrNull()
    {
        if (forcedPriorityTarget == null || !forcedPriorityTarget.IsAlive || forcedPriorityTarget.team == Team.Friendly) return null;
        return CanReachInsect(forcedPriorityTarget) ? forcedPriorityTarget.gameObject : null;
    }

    protected GameObject FindNearest(System.Collections.Generic.List<Insect> insects)
    {
        if (cannotTargetInsects) return null;
        GameObject forced = ForcedPriorityTargetOrNull();
        if (forced != null) return forced;

        GameObject nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive || insect.carriedBy != null) continue;
            // ground position, not GetAimPoint() - a knockup only bounces the insect's visual
            // sprite upward (never its actual root position), and that visual offset shares the
            // same world axis this 2D range check reads, so using the aim point here would let a
            // plant "see" an insect that only looks airborne, never actually moved
            // the range checks use how far it looks (Visibility), nearest is still the real distance
            float real = Vector3.Distance(transform.position, insect.transform.position);
            float dist = insect.SeenDistance(real);
            if (IsWithinAttackRange(insect, dist) && real < nearestDist && IsValidNightTarget(insect, dist) && IsValidFlightTarget(insect) && InSight(insect))
            {
                nearestDist = real;
                nearest = insect.gameObject;
            }
        }
        return nearest;
    }

    protected GameObject FindStrongest(System.Collections.Generic.List<Insect> insects)
    {
        if (cannotTargetInsects) return null;
        GameObject forced = ForcedPriorityTargetOrNull();
        if (forced != null) return forced;

        GameObject strongest = null;
        float highestMaxHealth = -1f;
        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive || insect.carriedBy != null) continue;
            float dist = insect.SeenDistance(Vector3.Distance(transform.position, insect.transform.position)); // see FindNearest
            if (!IsWithinAttackRange(insect, dist) || !IsValidNightTarget(insect, dist) || !IsValidFlightTarget(insect) || !InSight(insect)) continue;
            if (insect.maxHealth > highestMaxHealth)
            {
                highestMaxHealth = insect.maxHealth;
                strongest = insect.gameObject;
            }
        }
        return strongest;
    }

    // fastest or slowest insect in range by its current movement speed (slows count), ties going
    // to whichever is furthest along the path, same as First
    protected GameObject FindFastest(System.Collections.Generic.List<Insect> insects) => FindBySpeed(insects, true);
    protected GameObject FindSlowest(System.Collections.Generic.List<Insect> insects) => FindBySpeed(insects, false);

    private GameObject FindBySpeed(System.Collections.Generic.List<Insect> insects, bool fastest)
    {
        if (cannotTargetInsects) return null;
        GameObject forced = ForcedPriorityTargetOrNull();
        if (forced != null) return forced;

        // a reused list (it was a new one on every call, and Fastest and Slowest plants call this
        // every frame while waiting): filled and read here only, so nothing keeps it between calls
        System.Collections.Generic.List<Insect> inRange = _speedCandidates;
        inRange.Clear();
        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive || insect.carriedBy != null) continue;
            float dist = insect.SeenDistance(Vector3.Distance(transform.position, insect.transform.position)); // see FindNearest
            if (!IsWithinAttackRange(insect, dist) || !IsValidNightTarget(insect, dist) || !IsValidFlightTarget(insect) || !InSight(insect)) continue;
            inRange.Add(insect);
        }
        Insect best = PickBySpeed(inRange, fastest);
        inRange.Clear();
        return best != null ? best.gameObject : null;
    }

    private readonly System.Collections.Generic.List<Insect> _speedCandidates = new System.Collections.Generic.List<Insect>();

    // picks among insects that already passed every range check, so plants with their own
    // candidate lists (e.g. Nerium Oleander's chain) can use the same rule
    // takes the list itself (it took any sequence, which made a new enumerator on every call)
    protected static Insect PickBySpeed(System.Collections.Generic.List<Insect> candidates, bool fastest)
    {
        const float SpeedTieTolerance = 0.01f;
        Insect best = null;
        float bestSpeed = 0f, bestDistToNext = Mathf.Infinity;
        int bestWaypoint = -1;
        for (int c = 0; c < candidates.Count; c++)
        {
            Insect insect = candidates[c];
            if (insect == null) continue;
            float speed = insect.CurrentMoveSpeed;
            Transform waypoint = insect.GetCurrentWaypoint();
            float distToNext = waypoint != null ? Vector3.Distance(insect.transform.position, waypoint.position) : Mathf.Infinity;
            bool better;
            if (best == null) better = true;
            else if (Mathf.Abs(speed - bestSpeed) > SpeedTieTolerance) better = fastest ? speed > bestSpeed : speed < bestSpeed;
            else better = insect.currentWaypointIndex > bestWaypoint
                || (insect.currentWaypointIndex == bestWaypoint && distToNext < bestDistToNext);
            if (!better) continue;
            best = insect; bestSpeed = speed; bestWaypoint = insect.currentWaypointIndex; bestDistToNext = distToNext;
        }
        return best;
    }

    protected GameObject FindFirst(System.Collections.Generic.List<Insect> insects)
    {
        if (cannotTargetInsects) return null;
        GameObject forced = ForcedPriorityTargetOrNull();
        if (forced != null) return forced;

        GameObject furthest = null;
        int highestWaypointIndex = -1;
        float closestDistToNext = Mathf.Infinity;
        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive || insect.carriedBy != null) continue;
            float dist = insect.SeenDistance(Vector3.Distance(transform.position, insect.transform.position)); // see FindNearest
            if (!IsWithinAttackRange(insect, dist) || !IsValidNightTarget(insect, dist) || !IsValidFlightTarget(insect) || !InSight(insect)) continue;
            Transform waypoint = insect.GetCurrentWaypoint();
            if (waypoint == null) continue;
            if (insect.currentWaypointIndex > highestWaypointIndex)
            {
                highestWaypointIndex = insect.currentWaypointIndex;
                closestDistToNext = Vector3.Distance(insect.transform.position, waypoint.position);
                furthest = insect.gameObject;
            }
            else if (insect.currentWaypointIndex == highestWaypointIndex)
            {
                float d = Vector3.Distance(insect.transform.position, waypoint.position);
                if (d < closestDistToNext) { closestDistToNext = d; furthest = insect.gameObject; }
            }
        }
        return furthest;
    }

    protected GameObject FindLast(System.Collections.Generic.List<Insect> insects)
    {
        if (cannotTargetInsects) return null;
        GameObject forced = ForcedPriorityTargetOrNull();
        if (forced != null) return forced;

        GameObject last = null;
        int lowestWaypointIndex = int.MaxValue;
        float furthestDistToNext = -1f;
        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive || insect.carriedBy != null) continue;
            float dist = insect.SeenDistance(Vector3.Distance(transform.position, insect.transform.position)); // see FindNearest
            if (!IsWithinAttackRange(insect, dist) || !IsValidNightTarget(insect, dist) || !IsValidFlightTarget(insect) || !InSight(insect)) continue;
            Transform waypoint = insect.GetCurrentWaypoint();
            if (waypoint == null) continue;
            if (insect.currentWaypointIndex < lowestWaypointIndex)
            {
                lowestWaypointIndex = insect.currentWaypointIndex;
                furthestDistToNext = Vector3.Distance(insect.transform.position, waypoint.position);
                last = insect.gameObject;
            }
            else if (insect.currentWaypointIndex == lowestWaypointIndex)
            {
                float d = Vector3.Distance(insect.transform.position, waypoint.position);
                if (d > furthestDistToNext) { furthestDistToNext = d; last = insect.gameObject; }
            }
        }
        return last;
    }

    // can this plant, using its own complete targeting logic, actually reach this insect right
    // now? default: the standard single circle plus this plant's own night-visibility rule.
    // public (not protected) so a DIFFERENT plant can ask "would you be able to see this" without
    // needing to know how targeting actually works for that specific plant - e.g. Carrot's Soil
    // Bond calls this on its bonded ally rather than re-deriving a plain circle from its
    // attackRange, so a plant with exotic reach (like Nerium Oleander's sprout-chain targeting,
    // which bypasses the normal range circle entirely) is still seen correctly through the bond
    public virtual bool CanReachInsect(Insect insect)
    {
        float dist = insect.SeenDistance(Vector3.Distance(transform.position, insect.transform.position)); // see FindNearest
        return IsWithinAttackRange(insect, dist) && IsValidNightTarget(insect, dist) && IsValidFlightTarget(insect) && InSight(insect);
    }

    // ---------- line of sight (Tommy, 2026-11-27) ----------
    // a plant that needs a clear line (straight and homing shooters, Stargazer, Kelp, Rhodiola: NeedsLineOfSight)
    // can't target a ground insect or an ally it can't see. from low ground, obstacle tiles and cliff edges
    // block the line; from high ground only trees, bushes and boulders standing on high ground do (it sees
    // down past cliffs and over everything on low ground). flying insects are never hidden. what it sees is
    // cast once from where it stands (LineOfSight) and kept until it moves or its ground changes. the part
    // of its range it can't see shows red on its range circle (SightShade)
    // melee plants need one too (Tommy, 2026-11-27: Acorn Knight, Bird of Paradise, Cactus)
    public virtual bool NeedsLineOfSight => IsMeleeAttacker;

    // the rays from where it stands, null when it sees everything (it needs no clear line)
    private float[] _sightRays;
    private bool _sightCast;
    private Vector3 _sightOrigin;
    private bool _sightFromHigh;
    private int _sightVersion;

    public bool OnHighground => occupiedTile != null && occupiedTile.isHighground;

    private void EnsureSight()
    {
        bool high = OnHighground;
        Vector3 here = transform.position;
        if (_sightCast && here == _sightOrigin && high == _sightFromHigh) return;
        _sightCast = true;
        _sightOrigin = here;
        _sightFromHigh = high;
        _sightVersion++;
        if (!NeedsLineOfSight) { _sightRays = null; return; }
        if (_sightRays == null) _sightRays = new float[LineOfSight.Rays];
        LineOfSight.Cast(here, _sightRays, high);
    }

    // can this plant see a point on the ground (an ally, or a ground insect's spot)
    public bool CanSee(Vector3 point)
    {
        if (!NeedsLineOfSight) return true;
        EnsureSight();
        return _sightRays == null || LineOfSight.Sees(_sightRays, _sightOrigin, point);
    }

    // a flying insect is never hidden
    public bool InSight(Insect insect) => insect.isFlying || CanSee(insect.transform.position);

    // the red part of its range circle, kept up to date while the circle shows
    private SightShade _sightShade;
    private void UpdateSightShade()
    {
#if UNITY_EDITOR
        // F8 on a selected plant: its line of sight readout in the console (LineOfSight.LogReadout)
        if (_isSelected && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame)
        {
            if (NeedsLineOfSight) EnsureSight();
            LineOfSight.LogReadout($"{name} (needs line of sight: {NeedsLineOfSight})", transform.position, OnHighground, attackRange, _sightRays, occupiedTile);
            Debug.Log($"[LineOfSight] {name}: sight version {_sightVersion}; " + (_sightShade != null ? _sightShade.Describe() : "no red area made yet") +
                      $"; circleRadius '{(circleRadius != null ? circleRadius.name : "none")}' active {(circleRadius != null && circleRadius.gameObject.activeInHierarchy)}");
        }
#endif
        if (!NeedsLineOfSight || circleRadius == null || !circleRadius.gameObject.activeInHierarchy) return;
        EnsureSight();
        if (_sightShade == null) _sightShade = SightShade.On(circleRadius);
        _sightShade.Show(_sightRays, _sightVersion, attackRange);
    }

    // a melee plant (see IsMeleeAttacker) can't normally pick a flying insect as a target - it
    // has no way to reach one in melee. the one exception: an insect that has already chosen to
    // attack THIS plant (see Insect.target) stays targetable, so the plant can fight back instead
    // of standing there defenseless while something flying whittles it down
    public virtual bool IsValidFlightTarget(Insect insect)
    {
        if (!IsMeleeAttacker || !insect.isFlying) return true;
        return ReferenceEquals(insect.target, this);
    }

    public virtual bool IsValidNightTarget(Insect insect, float distance)
    {
        if (cannotTargetInsects) return false;
        if (insect.isBurrowed && !DetectsBurrowed) return false;
        if (DarknessManager.instance == null || !DarknessManager.instance.isDark) return true;
        // melee plants fight at point-blank range regardless of visibility - darkness's halved
        // sight range only matters for plants that actually need to see farther than arm's reach
        if (IsMeleeAttacker) return true;
        if (distance <= attackRange * 0.5f) return true;
        return DarknessManager.instance.IsIlluminated(insect.transform.position);
    }

    // override on a plant that can specifically detect burrowed insects (ground-level
    // detection). false by default: a burrowed insect just can't be picked as anyone's primary
    // target. this only stops it from being SELECTED - incidental splash/AoE that overlaps its
    // position is separately blocked by Entity.Damage's CanHitBurrowed tag requirement, since
    // that isn't gated by target selection at all
    public virtual bool DetectsBurrowed => false;

    // generic windup/resolve split for attacks. call this from Attack()/Shoot() instead of
    // dealing damage or spawning a projectile immediately, so a plant can have a charge-up before
    // the actual hit lands. today, with no animations wired up on any plant yet, windupSeconds is
    // just a plain timer read from data. once a plant has a real Animator, the same onResolve
    // callback can instead be invoked from an Animation Event placed on the clip's hit/fire
    // frame, so the calling code in Attack()/Shoot() never needs to change, only what drives the
    // resolve does. onResolve should re-validate anything it closed over (is the target still
    // alive, still in range) rather than trusting a snapshot taken back when the windup started
    private Coroutine _attackWindupRoutine;

    protected void BeginAttackWindup(float windupSeconds, System.Action onResolve)
    {
        if (_attackWindupRoutine != null) return; // already mid windup, refuse a second one
        _attackWindupRoutine = StartCoroutine(AttackWindupRoutine(windupSeconds, onResolve));
    }

    private IEnumerator AttackWindupRoutine(float windupSeconds, System.Action onResolve)
    {
        if (windupSeconds > 0f)
            yield return new WaitForSeconds(windupSeconds);
        _attackWindupRoutine = null;
        onResolve?.Invoke();
    }

    // seconds of windup before an attack fires, at the CURRENT attack speed - data.baseAttackChargeTime
    // is authored against the plant's own base Attack Speed, same convention as Sunflower's bolt
    // delay: scales inversely with attack speed, same 1/attackSpeed relationship attackCooldown uses
    public float AttackChargeTime
    {
        get
        {
            if (data == null || data.baseAttackChargeTime <= 0f) return 0f;
            float baseSpeed = data.baseAttackSpeed > 0f ? data.baseAttackSpeed : attackSpeed;
            return data.baseAttackChargeTime * (baseSpeed / Mathf.Max(0.01f, attackSpeed));
        }
    }

    // animation-driven counterpart to BeginAttackWindup, for a plant with a real attack charge
    // animation (drawing a bow, swinging a sword, summoning a fireball): instead of a plain
    // timer, this plays animator's clip at whatever speed makes its full clipLength fit exactly
    // into AttackChargeTime, so the whole animation visibly speeds up or slows down with attack
    // speed while an Animation Event placed on the exact frame the attack should fire (calling
    // OnAttackChargeFireFrame()) still lands at a fixed fraction of the clip either way.
    // clipLength is the clip's own authored Length in seconds (shown in the Animation window) -
    // Unity doesn't expose this before the state is actually entered, so it's passed in rather
    // than queried. falls back to firing immediately if no animator is wired up yet.
    //
    // keyed per-Animator rather than a single shared field: a plant can have multiple charge
    // animations genuinely overlapping in time (e.g. Sunflower's three max-level circles, each
    // starting on its own staggered timer rather than waiting for the previous one to finish) - a
    // single shared "pending callback" would get clobbered by the second BeginAnimatedAttackCharge
    // call before the first one's Animation Event ever fires, since each Animator is its own
    // stable, never-reused-concurrently identity, this isolates them correctly
    private readonly Dictionary<Animator, System.Action> _pendingChargeFireCallbacks = new Dictionary<Animator, System.Action>();

    // fires a trigger on the body animator, stretched or squeezed so a clip of clipLength seconds
    // fits exactly inside targetDuration. interrupt is for skills: it clears any trigger still
    // waiting and keeps every other body animation off the body until this one is done (its state
    // needs an Any State transition, or one from every state, with no exit time). it only cuts
    // the animation short, never the logic behind it, so an attack in progress still lands
    protected void PlayBodyAnimation(string trigger, float clipLength, float targetDuration, bool interrupt = false)
    {
        if (!BodyHasParameter(trigger)) return;
        if (!interrupt && Time.time < _bodyInterruptUntil) return;

        if (interrupt)
        {
            foreach (AnimatorControllerParameter p in bodyAnimator.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger) bodyAnimator.ResetTrigger(p.name);
            _bodyInterruptUntil = Time.time + Mathf.Max(0f, targetDuration);
        }

        if (_bodySpeedResetRoutine != null) StopCoroutine(_bodySpeedResetRoutine);
        bodyAnimator.speed = targetDuration > 0f && clipLength > 0f ? Mathf.Max(0.01f, clipLength / targetDuration) : 1f;
        bodyAnimator.SetTrigger(trigger);
        _bodySpeedResetRoutine = StartCoroutine(ResetBodySpeedAfter(targetDuration));
        StartCoroutine(DropUnusedBodyTrigger(trigger));
    }

    // a trigger the current state has no transition for would sit there and play its animation
    // late, the next time the body reaches a state that takes it, out of step with the logic. so
    // once the animator has had a real frame to use it (and finished any transition in progress)
    // whatever is left of it is dropped
    private IEnumerator DropUnusedBodyTrigger(string trigger)
    {
        do { yield return null; } while (Time.deltaTime <= 0f);
        while (bodyAnimator != null && bodyAnimator.IsInTransition(0)) yield return null;
        yield return null;
        if (bodyAnimator != null) bodyAnimator.ResetTrigger(trigger);
    }

    private IEnumerator ResetBodySpeedAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bodyAnimator != null) bodyAnimator.speed = 1f;
        _bodySpeedResetRoutine = null;
    }

    protected void SetBodyBool(string parameter, bool value)
    {
        if (BodyHasParameter(parameter)) bodyAnimator.SetBool(parameter, value);
    }

    private bool BodyHasParameter(string parameter)
    {
        if (bodyAnimator == null || bodyAnimator.runtimeAnimatorController == null || !bodyAnimator.isInitialized) return false;
        if (_bodyParametersFor != bodyAnimator.runtimeAnimatorController)
        {
            _bodyParametersFor = bodyAnimator.runtimeAnimatorController;
            _bodyParameters.Clear();
            foreach (AnimatorControllerParameter p in bodyAnimator.parameters) _bodyParameters.Add(p.name);
        }
        return _bodyParameters.Contains(parameter);
    }

    protected void BeginAnimatedAttackCharge(Animator animator, string triggerName, float clipLength, System.Action onFireFrame)
    {
        if (animator == null) { onFireFrame?.Invoke(); return; }
        _pendingChargeFireCallbacks[animator] = onFireFrame;
        float chargeTime = AttackChargeTime;
        animator.speed = chargeTime > 0f && clipLength > 0f ? Mathf.Max(0.01f, clipLength / chargeTime) : 1f;
        animator.SetTrigger(triggerName);
    }

    // hook this up (indirectly, via AttackChargeAnimationRelay on the same GameObject as the
    // Animator) to an Animation Event on the attack charge clip's fire frame. sourceAnimator
    // identifies which pending charge this is, so overlapping charges on different Animators
    // never cross-resolve each other
    public void OnAttackChargeFireFrame(Animator sourceAnimator)
    {
        if (sourceAnimator == null) return;
        if (!_pendingChargeFireCallbacks.TryGetValue(sourceAnimator, out System.Action callback)) return;
        _pendingChargeFireCallbacks.Remove(sourceAnimator);
        callback?.Invoke();
    }
}
