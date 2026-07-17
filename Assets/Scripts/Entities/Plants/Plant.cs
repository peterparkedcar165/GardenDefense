using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public struct PlantBaseStats
{
    // Common
    public float attackDamage, attackSpeed, attackRange, skillCooldown, passiveCooldown, skillDuration;
    public int piercing;
    // AcornSprout
    public float stunChance, stunDuration, skillDamageMultiplier;
    // Sunflower / BogIris
    public float sunGenerated, sunInterval, openDuration;
    // BogIris skill
    public float geyserDamage, knockUpHeight;
    // Dandelion
    public int seedCount;
    public float beamWidth;
    // LeafRanger
    public float skillAttackSpeedBonus;
    // Waterlily
    public float splashRadius, bubbleDamage;
    // PoisonShroom
    public float poisonDuration, poisonDamagePerSecond;
    // Snowdrop
    public float slowPercent, blizzardDamage;
    // Calendula
    public float floralGlowHeal;
}

public enum TARGETING { Nearest, First, Last }

// a plant's cultivar defines its archetype/role in the garden
public enum PlantCultivar
{
    Chlorophyll,    // sun generator
    Verdance,       // healer
    Symbiosis,      // buffer
    Shelter,        // tank/shield
    Thorn,          // single target dps
    Wither,         // debuff/nihility
    Burgeon,        // summoning
    Kinship,        // coordinated attacks (e.g. Calendula's skill)
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

    public virtual bool ShowRangeCircle => true;
    protected virtual bool GetPassiveBarVisible() => passiveCooldown > 0f && passiveCooldownTimer > 0f;
    protected virtual float GetPassiveBarFill() => passiveCooldown > 0f ? Mathf.Clamp01(1f - passiveCooldownTimer / passiveCooldown) : 1f;
    protected virtual bool GetAttackBarVisible() => false;
    protected virtual float GetAttackBarFill() => attackCooldown > 0f ? Mathf.Clamp01(attackCooldownTimer / attackCooldown) : 0f;
    [SerializeField] private Transform circleRadius;
    private Transform darkCircleRadius;
    private CircleCollider2D _circleCollider;
    private bool _isSelected = false;
    private UnityEngine.Rendering.Universal.Light2D _light2D;
    private float _lastLightEmissionRange;
    protected virtual float LightIntensity => 0.65f;
    private const float lightFadeSpeed = 2f;
    private GameObject _skillBarInstance;
    private Transform _skillBarFill;
    private GameObject _passiveBarInstance;
    private Transform _passiveBarFill;
    private GameObject _attackBarInstance;
    private Transform _attackBarFill;
    [SerializeField] private float lightInnerRadius = 1.2f;
    [SerializeField] private float lightFalloffStrength = 0.2f;
    protected virtual bool ShowLight => DarknessManager.instance != null;
    protected virtual bool ShowDarkCircle => true;

    // stunned plants (e.g. Webbed by a Cave Spider) cannot attack
    public bool IsStunned => HasEffect<HardCrowdControl>();

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

    private void StoreDeadRecord()
    {
        if (occupiedTile == null || selfPrefab == null) return;
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        occupiedTile.deadPlant = new DeadPlantRecord
        {
            prefab              = selfPrefab,
            previewSprite       = sr != null ? sr.sprite                  : null,
            spriteLocalPosition = sr != null ? sr.transform.localPosition : Vector3.zero,
            sortingLayerID      = sr != null ? sr.sortingLayerID          : 0,
            sortingOrder        = sr != null ? sr.sortingOrder            : 0,
            path1Level   = path1Level,
            path2Level   = path2Level,
            path3Level   = path3Level,
            path3Unlocked = path3Unlocked,
            totalSunSpent = totalSunSpent,
            exp          = exp,
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
            sr.color = new Color(1f, 1f, 1f, 0.6f);
            _ghosts.Add(ghost);
        }
    }

    public static void HideDeadPlantGhosts()
    {
        foreach (GameObject g in _ghosts)
            if (g != null) Destroy(g);
        _ghosts.Clear();
    }

    public static Plant RevivePlant(Tile tile)
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
        plant.skillCooldownTimer = plant.skillCooldown * 0.1f;

        tile.isOccupied = true;
        Collider2D tileCol = tile.GetComponent<Collider2D>();
        if (tileCol != null) tileCol.enabled = false;
        return plant;
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        comfortMin = baseComfortMin + comfortMinAdder;
        comfortMax = baseComfortMax + comfortMaxAdder;
        temperatureMin = baseTemperatureMin + temperatureMinAdder;
        temperatureMax = baseTemperatureMax + temperatureMaxAdder;
        passiveCooldown = Mathf.Max(basePassiveCooldown * 0.2f, basePassiveCooldown + passiveCooldownAdder + (basePassiveCooldown * passiveCooldownMultiplier) - (basePassiveCooldown * passiveCooldownReductionMultiplier));
        passiveDuration = basePassiveDuration + passiveDurationAdder + (basePassiveDuration * passiveDurationMultiplier);
        skillCooldown = Mathf.Max(baseSkillCooldown * 0.2f, baseSkillCooldown - skillCooldownReductionAdder - (baseSkillCooldown * skillCooldownReductionMultiplier));
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

    // Burgeon plants that command minions: the info panel shows a "relocate formation" button,
    // and clicking it (after aiming a point) calls RelocateMinionFormation
    public virtual bool CommandsMinions => false;
    public virtual void RelocateMinionFormation(Vector3 worldPoint) { }
    public int sunCost, totalSunSpent = 0;
    [System.NonSerialized] public ElementalType elementalType;
    [System.NonSerialized] public DamageType damageType;
    public int exp = 0;
    public float expBoost;
    public float activeCooldown;

    [Header("Temperature")]
    public float temperature = 10f;
    public float baseComfortMin = 0f,    comfortMinAdder,    comfortMin;
    public float baseComfortMax = 20f,   comfortMaxAdder,    comfortMax;
    public float baseTemperatureMin = -10f, temperatureMinAdder, temperatureMin;
    public float baseTemperatureMax = 30f,  temperatureMaxAdder, temperatureMax;

    [Header("Passive")]
    public float basePassiveCooldown, passiveCooldown, passiveCooldownAdder, passiveCooldownReductionMultiplier, passiveCooldownMultiplier;
    public float passiveCooldownTimer;
    public float basePassiveDuration, passiveDuration, passiveDurationAdder, passiveDurationMultiplier;


    [Header("Skill")]
    public float baseSkillCooldown, skillCooldown, skillCooldownReductionAdder, skillCooldownReductionMultiplier;
    public float skillCooldownTimer;
    public float baseSkillRadius, skillRadius, skillRadiusAdder, skillRadiusMultiplier;
    public float baseSkillDamageMultiplier, skillDamageMultiplier, skillDamageMultiplierAdder;
    public float baseSkillHealth, skillHealth;
    public bool SkillReady => path3Unlocked && skillCooldownTimer <= 0 && !IsSilenced && !IsChanneling;


    [Header("Paths")]
    public int path1Level, path2Level, path3Level, path1LevelAdder, path2LevelAdder, path3LevelAdder, effectivePath1Level, effectivePath2Level, effectivePath3Level;
    public bool path3Unlocked;


    protected override void Awake()
    {
        _circleCollider = GetComponent<CircleCollider2D>();
        baseMaxHealth = 200;
        healthBarOffset = new Vector3(0, 0.7f, 0);
        base.Awake();
        SpawnHealthBar();
        SpawnAttackBar();
        SpawnSkillBar();
        SpawnPassiveBar();
        baseCriticalChance = 0.05f;
        baseCriticalDamage = 1.75f;
        allPlants.Add(this);
    }

    protected void LoadData()
    {
        if (data == null) return;
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
        baseGroundResistance   = data.baseGroundResistance;
        baseDotResistance      = data.baseDotResistance;
        basePhysicalDamage     = data.basePhysicalDamage;
        baseMagicDamage        = data.baseMagicDamage;
        baseArmorPenFlat       = data.baseArmorPenFlat;
        baseArmorPenPercent    = data.baseArmorPenPercent;
        baseMagicPenFlat       = data.baseMagicPenFlat;
        baseMagicPenPercent    = data.baseMagicPenPercent;
        baseLifesteal          = data.baseLifesteal;
        baseBonusEffectChance  = data.baseBonusEffectChance;
        baseFireDamage         = data.baseFireDamage;
        baseWaterDamage        = data.baseWaterDamage;
        baseGrassDamage       = data.baseGrassDamage;
        baseWindDamage         = data.baseWindDamage;
        basePoisonDamage       = data.basePoisonDamage;
        baseIceDamage          = data.baseIceDamage;
        baseGroundDamage       = data.baseGroundDamage;
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
        sunCost                       = data.sunCost;
        basePassiveCooldown           = data.basePassiveCooldown;
        basePassiveDuration           = data.basePassiveDuration;
        baseSkillCooldown             = data.baseSkillCooldown;
        baseSkillDuration             = data.baseSkillDuration;
        baseSkillRadius               = data.baseSkillRadius;
        baseSkillDamageMultiplier     = data.baseSkillDamageMultiplier;
        baseSkillHealth               = data.baseSkillHealth;
        elementalType                 = data.elementalType;
        damageType                    = data.damageType;
        if (elementalType == ElementalType.Ice)
        {
            temperatureMaxAdder = 10f - baseTemperatureMax;             // cap at 10, immune to heat
            comfortMinAdder     = baseTemperatureMin - baseComfortMin;  // lower comfort floor to temperature min, immune to cold
        }
        if (elementalType == ElementalType.Fire) temperatureMinAdder = 10f - baseTemperatureMin; // floor temperature at 10
        FertilizerManager.instance?.ApplyTo(this);
        SkillTreeManager.ApplyTo(this);
        UpdateStats();
        health = maxHealth;
        UpdateHealthBar();
    }

    private int[] GetAllSortingLayerIDs()
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
        allPlants.Remove(this);
        if (PlantUpgradeUI.instance != null && PlantUpgradeUI.instance.GetSelectedPlant() == this)
            PlantUpgradeUI.instance.HidePanel();
    }

    private void SpawnAttackBar()
    {
        GameObject prefab = Resources.Load<GameObject>("HealthBar");
        if (prefab == null) return;

        _attackBarInstance = Instantiate(prefab, transform);

        Vector3 pos = healthBarOffset;
        pos.x -= 0.35625f;
        pos.y -= 0.084375f;
        _attackBarInstance.transform.localPosition = pos;

        Vector3 scale = _attackBarInstance.transform.localScale;
        scale.x *= 0.75f;
        scale.y *= 0.35f;
        _attackBarInstance.transform.localScale = scale;

        _attackBarFill = _attackBarInstance.transform.Find("Fill");
        if (_attackBarFill != null)
        {
            SpriteRenderer sr = _attackBarFill.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.cyan;
        }

        Transform shieldChild = _attackBarInstance.transform.Find("ShieldFill");
        if (shieldChild != null) Destroy(shieldChild.gameObject);

        _attackBarInstance.SetActive(false);
    }

    private void SpawnPassiveBar()
    {
        GameObject prefab = Resources.Load<GameObject>("HealthBar");
        if (prefab == null) return;

        _passiveBarInstance = Instantiate(prefab, transform);

        Vector3 pos = healthBarOffset;
        pos.x -= 0.35625f;
        pos.y -= 0.128125f;
        _passiveBarInstance.transform.localPosition = pos;

        Vector3 scale = _passiveBarInstance.transform.localScale;
        scale.x *= 0.75f;
        scale.y *= 0.35f;
        _passiveBarInstance.transform.localScale = scale;

        _passiveBarFill = _passiveBarInstance.transform.Find("Fill");
        if (_passiveBarFill != null)
        {
            SpriteRenderer sr = _passiveBarFill.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.85f, 0f);
        }

        Transform shieldChild = _passiveBarInstance.transform.Find("ShieldFill");
        if (shieldChild != null) Destroy(shieldChild.gameObject);

        _passiveBarInstance.SetActive(false);
    }

    private void SpawnSkillBar()
    {
        GameObject prefab = Resources.Load<GameObject>("HealthBar");
        if (prefab == null) return;

        _skillBarInstance = Instantiate(prefab, transform);

        Vector3 pos = healthBarOffset;
        pos.x -= 0.35625f;
        pos.y -= 0.171875f;
        _skillBarInstance.transform.localPosition = pos;

        Vector3 scale = _skillBarInstance.transform.localScale;
        scale.x *= 0.75f;
        scale.y *= 0.35f;
        _skillBarInstance.transform.localScale = scale;

        _skillBarFill = _skillBarInstance.transform.Find("Fill");
        if (_skillBarFill != null)
        {
            SpriteRenderer sr = _skillBarFill.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.8f, 0.8f, 0.8f);
        }

        Transform shieldChild = _skillBarInstance.transform.Find("ShieldFill");
        if (shieldChild != null) Destroy(shieldChild.gameObject);

        _skillBarInstance.SetActive(false);
    }

    [SerializeField] private SpriteRenderer mainRenderer;
    private SpriteRenderer _cachedRenderer;
    private SpriteRenderer[] _outlineRenderers;
    private bool _isHighlighted;
    private bool _hoverHighlighted;
    private const int OutlineCount = 8;
    private const float OutlineWidth = 0.05f;

    private static Material _outlineMaterial;
    private static Material GetOutlineMaterial()
    {
        if (_outlineMaterial != null) return _outlineMaterial;
        Shader shader = Shader.Find("Custom/SpriteSilhouette");
        if (shader != null) _outlineMaterial = new Material(shader);
        return _outlineMaterial;
    }

    protected virtual SpriteRenderer GetMainRenderer()
    {
        if (_cachedRenderer != null) return _cachedRenderer;
        _cachedRenderer = mainRenderer ?? GetComponentInChildren<SpriteRenderer>();
        return _cachedRenderer;
    }

    protected void ResetOutlineRenderers()
    {
        if (_outlineRenderers != null)
        {
            foreach (var r in _outlineRenderers)
                if (r != null) Destroy(r.gameObject);
            _outlineRenderers = null;
        }
        _cachedRenderer = null;
        _isHighlighted = false;
    }

    private void EnsureOutlineRenderers()
    {
        if (_outlineRenderers != null) return;
        SpriteRenderer sr = GetMainRenderer();
        if (sr == null) return;

        _outlineRenderers = new SpriteRenderer[OutlineCount];
        for (int i = 0; i < OutlineCount; i++)
        {
            float angle = i * (360f / OutlineCount) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * OutlineWidth;

            GameObject obj = new GameObject("Outline");
            obj.transform.SetParent(sr.transform);
            obj.transform.localPosition = offset;
            obj.transform.localScale = Vector3.one;
            obj.transform.localRotation = Quaternion.identity;
            obj.layer = gameObject.layer;

            SpriteRenderer outlineSR = obj.AddComponent<SpriteRenderer>();
            outlineSR.sortingLayerID = sr.sortingLayerID;
            outlineSR.sortingOrder = sr.sortingOrder - 1;
            outlineSR.enabled = false;
            Material mat = GetOutlineMaterial();
            if (mat != null) outlineSR.material = mat;
            _outlineRenderers[i] = outlineSR;
        }
    }

    public void SetHighlight(Color color)
    {
        EnsureOutlineRenderers();
        if (_outlineRenderers == null) return;
        SpriteRenderer sr = GetMainRenderer();
        bool targeting = SkillTargetingManager.instance != null && SkillTargetingManager.instance.IsPlantTargeting;
        if (_isSelected || (_hoverHighlighted && !targeting))
            color = Color.white;
        else if (_hoverHighlighted && targeting)
            color = Color.yellow;
        foreach (SpriteRenderer outline in _outlineRenderers)
        {
            if (outline == null) continue;
            if (sr != null) outline.sprite = sr.sprite;
            outline.color = color;
            outline.enabled = true;
        }
        _isHighlighted = true;
    }

    public void ClearHighlight()
    {
        if (_isSelected || _hoverHighlighted) return;
        if (!_isHighlighted || _outlineRenderers == null) return;
        foreach (SpriteRenderer outline in _outlineRenderers)
            outline.enabled = false;
        _isHighlighted = false;
    }

    protected override void Start()
    {
        base.Start();
        GetMainRenderer();

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
            case WeatherType.Sunny: ApplyEffect(new SunlightExposedEffect(this, this, intensity)); break;
            case WeatherType.Rain:  ApplyEffect(new RainExposedEffect(this, this, intensity));     break;
            case WeatherType.Snow:  ApplyEffect(new SnowExposedEffect(this, this, intensity));    break;
        }
    }

    private void RemoveWeatherEffect(WeatherType w)
    {
        switch (w)
        {
            case WeatherType.Sunny: RemoveEffect<SunlightExposedEffect>(); break;
            case WeatherType.Rain:  RemoveEffect<RainExposedEffect>();     break;
            case WeatherType.Snow:  RemoveEffect<SnowExposedEffect>();    break;
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
        if (ShowRangeCircle && circleRadius != null)
            circleRadius.gameObject.SetActive(false);
        if (darkCircleRadius != null)
            darkCircleRadius.gameObject.SetActive(false);
        RefreshHealthBarVisibility();
    }

    protected override void Update()
    {
        base.Update();
        effectivePath1Level = path1Level + path1LevelAdder;
        effectivePath2Level = path2Level + path2LevelAdder + GetWeatherPath2Bonus();
        effectivePath3Level = path3Level + path3LevelAdder;

        if (_light2D != null)
        {
            if (lightEmissionRange != _lastLightEmissionRange)
            {
                if (lightEmissionRange > 0f)
                {
                    _light2D.pointLightOuterRadius = lightEmissionRange;
                    _light2D.pointLightInnerRadius = Mathf.Min(lightInnerRadius, lightEmissionRange);
                }
                if (lightEmissionRange > _lastLightEmissionRange)
                    _light2D.intensity = 0f;
                _lastLightEmissionRange = lightEmissionRange;
            }
            float targetIntensity = lightEmissionRange > 0f ? LightIntensity : 0f;
            _light2D.intensity = Mathf.Lerp(_light2D.intensity, targetIntensity, Time.unscaledDeltaTime * lightFadeSpeed);
        }

        if (passiveCooldownTimer > 0)
            passiveCooldownTimer -= Time.deltaTime;
        if (skillCooldownTimer > 0)
            skillCooldownTimer -= Time.deltaTime;

        {
            float stackY = healthBarOffset.y - 0.046875f;
            const float barHalfH = 0.021875f;

            bool attackShow = GetAttackBarVisible() && (_isSelected || _hoverHighlighted);
            if (_attackBarInstance != null)
            {
                _attackBarInstance.SetActive(attackShow);
                if (attackShow)
                {
                    Vector3 p = _attackBarInstance.transform.localPosition;
                    p.y = stackY - barHalfH;
                    _attackBarInstance.transform.localPosition = p;
                    stackY -= barHalfH * 2f;
                    if (_attackBarFill != null)
                    {
                        Vector3 s = _attackBarFill.localScale;
                        s.x = GetAttackBarFill();
                        _attackBarFill.localScale = s;
                    }
                }
            }

            bool passiveShow = GetPassiveBarVisible() && (_isSelected || _hoverHighlighted);
            if (_passiveBarInstance != null)
            {
                _passiveBarInstance.SetActive(passiveShow);
                if (passiveShow)
                {
                    Vector3 p = _passiveBarInstance.transform.localPosition;
                    p.y = stackY - barHalfH;
                    _passiveBarInstance.transform.localPosition = p;
                    stackY -= barHalfH * 2f;
                    if (_passiveBarFill != null)
                    {
                        Vector3 s = _passiveBarFill.localScale;
                        s.x = GetPassiveBarFill();
                        _passiveBarFill.localScale = s;
                    }
                }
            }

            bool skillShow = path3Unlocked;
            if (_skillBarInstance != null)
            {
                _skillBarInstance.SetActive(skillShow);
                if (skillShow)
                {
                    Vector3 p = _skillBarInstance.transform.localPosition;
                    p.y = stackY - barHalfH;
                    _skillBarInstance.transform.localPosition = p;
                    if (_skillBarFill != null)
                    {
                        float fill = skillCooldown > 0f ? Mathf.Clamp01(1f - skillCooldownTimer / skillCooldown) : 1f;
                        Vector3 s = _skillBarFill.localScale;
                        s.x = fill;
                        _skillBarFill.localScale = s;
                    }
                }
            }
        }

        if (_hoverHighlighted)
            SetHighlight(Color.white);

        UpdateTemperature();
    }

    private static readonly DamageTag[] temperatureDamageTags = { DamageTag.Weather };
    private float temperatureDamageTimer;

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
                        temperature += 1f * Time.deltaTime;
                    break;
                case TemperatureType.Cold:
                    bool sunWarmsFreeze = WeatherManager.instance.GetIntensity(WeatherType.Sunny) >= 5;
                    if (sunWarmsFreeze)
                        temperature = Mathf.Min(temperature + 1f * Time.deltaTime, comfortMax);
                    else
                        temperature -= 1f * Time.deltaTime;
                    break;
            }
        }
        temperature = Mathf.Clamp(temperature, temperatureMin, temperatureMax);

        if (WeatherManager.instance != null && WeatherManager.instance.temperature == TemperatureType.Hot
            && occupiedTile != null && (occupiedTile.tileType == TileType.Water || occupiedTile.isWaterAdjacent))
            temperature = Mathf.Min(temperature, 10f);

        if (WeatherManager.instance != null && WeatherManager.instance.temperature == TemperatureType.Cold
            && occupiedTile != null && (occupiedTile.tileType == TileType.Heat || occupiedTile.isHeatAdjacent))
            temperature = Mathf.Max(temperature, 10f);

        bool tooCold = temperature < comfortMin;
        bool tooHot  = temperature > comfortMax;

        if (tooCold || tooHot)
        {
            temperatureDamageTimer += Time.deltaTime;
            if (temperatureDamageTimer >= 2f)
            {
                temperatureDamageTimer = 0f;
                float dmg = maxHealth * 0.03f * 2f;
                ElementalType dmgElement = tooCold ? ElementalType.Ice : ElementalType.Fire;
                Damage(dmg, DamageType.True, dmgElement, temperatureDamageTags);
                DamageIndicator.Spawn(GetIndicatorPosition(), dmg, dmgElement, false);
            }
        }
        else
        {
            temperatureDamageTimer = 0f;
        }
    }

    public virtual void ActivateSkill() {}

    private int GetWeatherPath2Bonus()
    {
        if (elementalType == ElementalType.Fire && HasEffect<SunlightExposedEffect>())
            return 1;

        if (elementalType == ElementalType.Water)
        {
            bool onWater = occupiedTile != null && (occupiedTile.tileType == TileType.Water || occupiedTile.isWaterAdjacent);
            if (onWater || HasEffect<RainExposedEffect>()) return 1;
        }

        if (elementalType == ElementalType.Grass && occupiedTile != null && occupiedTile.tileType == TileType.Grass)
            return 1;

        if (elementalType == ElementalType.Wind && occupiedTile != null && occupiedTile.isHighground)
            return 1;

        if (elementalType == ElementalType.Ice && WeatherManager.instance?.temperature == TemperatureType.Cold)
            return 1;

        return 0;
    }

    public void Uproot()
    {
        float refundRate = GameManager.instance.currentWave == 0 ? 1f : 0.5f;
        GameManager.instance.SunCount += (int)(totalSunSpent * refundRate);
        Debug.Log("Uprooted " + GetName() + " and refunded " + (int)(totalSunSpent * refundRate));
        GameManager.instance.UpdateSun();
        // need some sound effects eventually
        occupiedTile.isOccupied = false;
        occupiedTile.GetComponent<Collider2D>().enabled = true;
        PlantSelector.instance.uprootMode = false;
        PlantUpgradeUI.instance.HidePanel();
        DetachAndFadeLight();
        Destroy(gameObject);
    }

    public void GainExp(float amount)
    {
        exp += (int)(amount * (1 + expBoost));
    }

    public virtual void OnPath1Upgrade(int level) {}
    public virtual void OnPath2Upgrade(int level) {}
    public virtual void OnPath3Unlock() {}
    public virtual void OnPath3Upgrade(int level) {}

    // UPGRADE COSTS
    public const int pathLevelCap = 5;

    public bool IsPath1Maxed => path1Level >= pathLevelCap;
    public bool IsPath2Maxed => path2Level >= pathLevelCap;
    public bool IsPath3Maxed => path3Level >= pathLevelCap;

    // upgrade cost scales with the plant's own suncost so premium plants cost
    // proportionally more to scale. base is the first level price as a fraction
    // of buy cost, step is the exponential multiplier/Lvl.
    const float upgradeBaseFactor = 0.2f;
    const float upgradeStepFactor = 2.5f;
    const float skillBaseFactor   = 0.25f;

    int PathCost(int level, float baseFactor) => Mathf.RoundToInt(sunCost * baseFactor * Mathf.Pow(upgradeStepFactor, level));

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

    protected string Level5Section(int effectiveLevel, string bonusText = null)
    {
        bool unlocked = effectiveLevel >= pathLevelCap;
        string header = $"<color={(unlocked ? "green" : "grey")}><b>[Max Level Bonus]</b></color>";
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
            return $"Increase Passive tree level by <color=green>1</color> when exposed to sunlight\nImmune to cold temperatures";

            case ElementalType.Grass:
            return $"Can be placed on <color=green>Grass</color>.\nIncrease Passive tree level by <color=green>1</color> when placed on Grass";

            case ElementalType.Water:
            return $"Increase Passive tree level by <color=green>1</color> when near natural water";

            case ElementalType.Poison:
            return $"Taking damage returns <color=purple>Poison</color> damage equal to <color=purple><b>200%</b></color> of the hit to the attacker.";

            case ElementalType.Ice:
            return $"Increase Passive tree level by <color=green>1</color> when in cold weather\nImmune to hot temperatures";

            case ElementalType.Wind:
            return $"Increase Passive tree level by <color=green>1</color> when in high altitude";

            default:
            return "";
        }
    }

    // DESCRIPTIONS
    public virtual string GetName() => data != null ? data.displayName : "";

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
        lightObj.transform.SetParent(null);
        var fader = lightObj.AddComponent<LightFader>();
        fader.SetupForFadeOut(_light2D);
        fader.FadeOut(0.3f, destroyOnComplete: true);
        _light2D = null;
    }

    public override void Kill()
    {
        StoreDeadRecord();
        FreeTile();
        DetachAndFadeLight();
        base.Kill();
    }

    public override void Kill(Entity source)
    {
        StoreDeadRecord();
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
    public Vector3 GetApproachPoint(Vector3 from)
    {
        if (_circleCollider != null)
            return Physics2D.ClosestPoint(from, _circleCollider);
        return transform.position;
    }

    public bool ReceiveAttack(float damage, Insect attacker)
    {
        float missChance = Mathf.Clamp01(evasion - attacker.accuracy);
        if (UnityEngine.Random.value < missChance)
        {
            StatusIndicator.Spawn(GetIndicatorPosition(), "Miss", new Color(0.55f, 0.6f, 0.75f));
            return false;
        }
        Damage(damage, attacker.attackDamageType, attacker.attackElementalType, attacker, false, new DamageTag[] { DamageTag.Melee, DamageTag.Attack });
        OnHitByInsect(attacker);
        return true;
    }

    protected virtual void OnHitByInsect(Insect attacker) {}
    public bool IsAlive => health > 0;
    public Vector3 Position => transform.position;

    // generates sun scaled by this plant's sunYieldMultiplier, rounded up (sun can't be decimal).
    // returns the total granted so callers can show the right indicator amount
    protected int GenerateSun(int amount)
    {
        int total = Mathf.CeilToInt(amount * (1f + sunYieldMultiplier));
        GameManager.instance?.AddSun(total);
        return total;
    }

    protected GameObject FindNearest(System.Collections.Generic.List<Insect> insects)
    {
        GameObject nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, insect.GetAimPoint());
            if (dist <= attackRange && dist < nearestDist && IsValidNightTarget(insect, dist))
            {
                nearestDist = dist;
                nearest = insect.gameObject;
            }
        }
        return nearest;
    }

    protected GameObject FindFirst(System.Collections.Generic.List<Insect> insects)
    {
        GameObject furthest = null;
        int highestWaypointIndex = -1;
        float closestDistToNext = Mathf.Infinity;
        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, insect.GetAimPoint());
            if (dist > attackRange || !IsValidNightTarget(insect, dist)) continue;
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
        GameObject last = null;
        int lowestWaypointIndex = int.MaxValue;
        float furthestDistToNext = -1f;
        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, insect.GetAimPoint());
            if (dist > attackRange || !IsValidNightTarget(insect, dist)) continue;
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

    public virtual bool IsValidNightTarget(Insect insect, float distance)
    {
        if (DarknessManager.instance == null || !DarknessManager.instance.isDark) return true;
        if (distance <= attackRange * 0.5f) return true;
        return DarknessManager.instance.IsIlluminated(insect.transform.position);
    }
}
