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

public abstract class Plant : Entity, IAttackable
{
    public static List<Plant> allPlants = new List<Plant>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        allPlants = new List<Plant>();
        SceneManager.sceneLoaded += (scene, mode) => allPlants.Clear();
    }

    public PlantData data;
    public Tile occupiedTile;

    [SerializeField] private Transform circleRadius;
    private Transform darkCircleRadius;
    private CircleCollider2D _circleCollider;
    private bool _isSelected = false;
    private UnityEngine.Rendering.Universal.Light2D _light2D;
    private const float lightIntensity = 0.65f;
    [SerializeField] private float lightInnerRadius = 1.2f;
    [SerializeField] private float lightFalloffStrength = 0.2f;
    protected virtual bool ShowLight => DarknessManager.instance != null;
    protected virtual bool ShowDarkCircle => true;

    public override void UpdateStats()
    {
        base.UpdateStats();
        comfortMin = baseComfortMin + comfortMinAdder;
        comfortMax = baseComfortMax + comfortMaxAdder;
        temperatureMin = baseTemperatureMin + temperatureMinAdder;
        temperatureMax = baseTemperatureMax + temperatureMaxAdder;
        passiveCooldown = basePassiveCooldown + passiveCooldownAdder + (basePassiveCooldown * passiveCooldownMultiplier) - (basePassiveCooldown * passiveCooldownReductionMultiplier);
        skillCooldown = baseSkillCooldown - skillCooldownReductionAdder - (baseSkillCooldown * skillCooldownReductionMultiplier);
        skillDamageMultiplier = baseSkillDamageMultiplier + skillDamageMultiplierAdder;
        skillDamage += baseSkillDamage * skillDamageMultiplier;
        float plantSpriteRadius = _circleCollider != null ? _circleCollider.radius * 2 : 0f;

        if (circleRadius != null)
        {
            // circleRadius.localScale = new Vector3((attackRange * 2f)  + plantSpriteRadius, (attackRange * 2f) + plantSpriteRadius, 1f); // INCLUDES SPRITE
            circleRadius.localScale = new Vector3(attackRange * 2f, attackRange * 2f, 1f);
        }
        if (darkCircleRadius != null)
            darkCircleRadius.localScale = new Vector3(attackRange, attackRange, 1f);

        if (lightEmissionRange > 0 && _light2D == null)
        {
            _light2D = gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            _light2D.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            _light2D.intensity = lightIntensity;
            _light2D.falloffIntensity = lightFalloffStrength;
            _light2D.targetSortingLayers = GetAllSortingLayerIDs();
        }

        if (_light2D != null)
        {
            _light2D.enabled = ShowLight;
            _light2D.intensity = lightIntensity;
            _light2D.pointLightOuterRadius = lightEmissionRange;
            _light2D.pointLightInnerRadius = Mathf.Min(lightInnerRadius, lightEmissionRange);
        }

        if (WeatherManager.instance != null)
        {
            switch (WeatherManager.instance.weather)
            {
                case WeatherType.Sunny: fireDamage += 0.12f; break;
                case WeatherType.Rain:  waterDamage += 0.12f; break;
                case WeatherType.Snow:  iceDamage += 0.12f; break;
            }
        }
    }
    public TileType[] allowedTiles;
    public TARGETING targeting = TARGETING.First;
    public virtual bool UsesTargeting => false;
    public int sunCost, totalSunSpent = 0;
    public ElementalType elementalType;
    public DamageType damageType;
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
    public float basePassiveDuration, passiveDuration;

    [Header("Skill")]
    public float baseSkillCooldown, skillCooldown, skillCooldownReductionAdder, skillCooldownReductionMultiplier;
    public float skillCooldownTimer;
    public float baseSkillRadius, skillRadius;
    public float baseSkillDamageMultiplier, skillDamageMultiplier, skillDamageMultiplierAdder;
    public float baseSkillHealth, skillHealth;
    public bool SkillReady => path3Unlocked && skillCooldownTimer <= 0;


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
        basePhysicalResistance = data.basePhysicalResistance;
        baseMagicResistance    = data.baseMagicResistance;
        baseFireResistance     = data.baseFireResistance;
        baseWaterResistance    = data.baseWaterResistance;
        basePoisonResistance   = data.basePoisonResistance;
        baseIceResistance      = data.baseIceResistance;
        baseNatureResistance   = data.baseNatureResistance;
        baseWindResistance     = data.baseWindResistance;
        baseDotResistance      = data.baseDotResistance;
        basePhysicalShred      = data.basePhysicalShred;
        baseMagicShred         = data.baseMagicShred;
        baseLifesteal          = data.baseLifesteal;
        baseBonusEffectChance  = data.baseBonusEffectChance;
        baseFireDamage         = data.baseFireDamage;
        baseWaterDamage        = data.baseWaterDamage;
        baseNatureDamage       = data.baseNatureDamage;
        baseWindDamage         = data.baseWindDamage;
        basePoisonDamage       = data.basePoisonDamage;
        baseIceDamage          = data.baseIceDamage;
        baseCriticalChance     = data.baseCriticalChance;
        baseCriticalDamage     = data.baseCriticalDamage;
        baseDotDamage          = data.baseDotDamage;
        baseElementalPower     = data.baseElementalPower;
        basePassiveDamage      = data.basePassiveDamage;
        baseSkillDamage        = data.baseSkillDamage;
        baseCoordinatedDamage  = data.baseCoordinatedDamage;
        baseLightEmissionRange        = data.baseLightEmissionRange;
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
        if (elementalType == ElementalType.Ice)  comfortMinAdder = -5f;
        if (elementalType == ElementalType.Fire) comfortMaxAdder =  5f;
        FertilizerManager.instance?.ApplyTo(this);
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
        allPlants.Remove(this);
        if (PlantUpgradeUI.instance != null && PlantUpgradeUI.instance.GetSelectedPlant() == this)
            PlantUpgradeUI.instance.HidePanel();
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

    private SpriteRenderer GetMainRenderer()
    {
        if (_cachedRenderer != null) return _cachedRenderer;
        _cachedRenderer = mainRenderer ?? GetComponentInChildren<SpriteRenderer>();
        return _cachedRenderer;
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
        foreach (SpriteRenderer outline in _outlineRenderers)
        {
            if (sr != null) outline.sprite = sr.sprite;
            outline.color = color;
            outline.enabled = true;
        }
        _isHighlighted = true;
    }

    public void ClearHighlight()
    {
        if (!_isHighlighted || _outlineRenderers == null) return;
        foreach (SpriteRenderer outline in _outlineRenderers)
            outline.enabled = false;
        _isHighlighted = false;
    }

    protected override void Start()
    {
        base.Start();
        GetMainRenderer();
        if (circleRadius != null)
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

    protected override void OnHover()
    {
        if (circleRadius != null)
            circleRadius.gameObject.SetActive(true);
        if (darkCircleRadius != null && DarknessManager.instance != null && DarknessManager.instance.isDark)
            darkCircleRadius.gameObject.SetActive(true);
        if (SkillTargetingManager.instance != null && SkillTargetingManager.instance.IsPlantTargeting)
        {
            SetHighlight(Color.yellow);
            _hoverHighlighted = true;
        }
    }

    protected override void OnHoverExit()
    {
        ClearHighlight();
        _hoverHighlighted = false;
        if (_isSelected) return;
        if (circleRadius != null)
            circleRadius.gameObject.SetActive(false);
        if (darkCircleRadius != null)
            darkCircleRadius.gameObject.SetActive(false);
        RefreshHealthBarVisibility();
    }

    public void Select()
    {
        _isSelected = true;
        if (circleRadius != null)
            circleRadius.gameObject.SetActive(true);
        if (darkCircleRadius != null && DarknessManager.instance != null && DarknessManager.instance.isDark)
            darkCircleRadius.gameObject.SetActive(true);
    }

    public void Deselect()
    {
        _isSelected = false;
        if (circleRadius != null)
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

        if (passiveCooldownTimer > 0)
            passiveCooldownTimer -= Time.deltaTime;
        if (skillCooldownTimer > 0)
            skillCooldownTimer -= Time.deltaTime;

        if (_hoverHighlighted && (SkillTargetingManager.instance == null || !SkillTargetingManager.instance.IsPlantTargeting))
        {
            ClearHighlight();
            _hoverHighlighted = false;
        }

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
                case TemperatureType.Hot:  temperature += 1f * Time.deltaTime; break;
                case TemperatureType.Cold: temperature -= 1f * Time.deltaTime; break;
            }
        }
        temperature = Mathf.Clamp(temperature, temperatureMin, temperatureMax);

        if (WeatherManager.instance != null && WeatherManager.instance.temperature == TemperatureType.Hot
            && occupiedTile != null && (occupiedTile.tileType == TileType.Water || occupiedTile.isWaterAdjacent))
            temperature = Mathf.Min(temperature, comfortMax);

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
                if (damageIndicatorPrefab != null)
                {
                    GameObject indicator = Instantiate(damageIndicatorPrefab, GetIndicatorPosition(), Quaternion.identity);
                    indicator.GetComponent<DamageIndicator>().Initialize(dmg, dmgElement, false);
                }
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
        if (elementalType == ElementalType.Fire)
        {
            if (WeatherManager.instance != null && WeatherManager.instance.weather == WeatherType.Sunny)
                return 1;
        }

        if (elementalType == ElementalType.Water)
        {
            bool onWater = occupiedTile != null && (occupiedTile.tileType == TileType.Water || occupiedTile.isWaterAdjacent);
            bool isRaining = WeatherManager.instance != null && WeatherManager.instance.weather == WeatherType.Rain;
            if (onWater || isRaining) return 1;
        }

        if (elementalType == ElementalType.Wind)
        {
            if (occupiedTile != null && occupiedTile.isHighground) return 1;
        }

        return 0;
    }

    public void Uproot()
    {
        GameManager.instance.SunCount += (int)(totalSunSpent * 0.5);
        Debug.Log("Uprooted " + GetName() + " and refunded " + (int)(totalSunSpent * 0.5));
        GameManager.instance.UpdateSun();
        // need some sound effects eventually
        occupiedTile.isOccupied = false;
        occupiedTile.GetComponent<Collider2D>().enabled = true;
        PlantSelector.instance.uprootMode = false;
        PlantUpgradeUI.instance.HidePanel();
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

    public int GetPath1Cost() => Mathf.RoundToInt((sunCost*0.25f) + (44 * path1Level));
    public int GetPath2Cost() => Mathf.RoundToInt((sunCost*0.25f) + (44 * path2Level));
    public int GetPath3Cost() => Mathf.RoundToInt((sunCost * 0.25f) + (31 + (32 * path3Level)));

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
    public virtual string GetPath1Description() => "";
    public virtual string GetPath2Description() => "";
    public virtual string GetPath3Description() => "";
    public virtual string GetElement()
    {
        switch (elementalType)
        {
            case ElementalType.Fire:
            return $"<color=orange>Fire</color>";

            case ElementalType.Nature:
            return $"<color=green>Nature</color>";

            case ElementalType.Water:
            return $"<color=#4FC3F7>Water</color>";

            case ElementalType.Poison:
            return $"<color=purple>Poison</color>";

            case ElementalType.Ice:
            return $"<color=#00FFFF>Ice</color>";

            case ElementalType.Wind:
            return $"<color=#B2EBF2>Wind</color>";

            default:
            return "";
        }
    }

    public virtual string GetElementDescription()
    {
        switch (elementalType)
        {
            case ElementalType.Fire:
            return $"Increase Passive tree level by <color=green>1</color> when exposed to sunlight\nIncrease comfort in <color=orange>hot</color> weather";

            case ElementalType.Nature:
            return $"Can be placed on <color=green>Grass</color>.";

            case ElementalType.Water:
            return $"Increase Passive tree level by <color=green>1</color> when near water";

            case ElementalType.Poison:
            return $"Taking damage returns <color=purple>Poison</color> damage equal to <color=purple><b>200%</b></color> of the hit to the attacker.";

            case ElementalType.Ice:
            return $"Increase Passive tree level by <color=green>1</color> when in cold weather\nIncrease comfort in <color=#00FFFF>cold</color> weather";

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

    public override void Kill()
    {
        if (PlantUpgradeUI.instance?.GetSelectedPlant() == this) PlantUpgradeUI.instance.HidePanel();
        FreeTile();
        base.Kill();
    }

    public override void Kill(Entity source)
    {
        if (PlantUpgradeUI.instance?.GetSelectedPlant() == this) PlantUpgradeUI.instance.HidePanel();
        FreeTile();
        base.Kill(source);
    }

    private void FreeTile()
    {
        if (occupiedTile == null) return;
        occupiedTile.isOccupied = false;
        occupiedTile.GetComponent<Collider2D>().enabled = true;
    }

    // IAttackable
    public void ReceiveAttack(float damage, Insect attacker)
    {
        Damage(damage, DamageType.Physical, ElementalType.Neutral, attacker, false, new DamageTag[] { DamageTag.Melee, DamageTag.Attack });
        OnHitByInsect(attacker);
    }

    protected virtual void OnHitByInsect(Insect attacker) {}
    public bool IsAlive => health > 0;
    public Vector3 Position => transform.position;

    protected GameObject FindNearest(System.Collections.Generic.List<Insect> insects)
    {
        GameObject nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (Insect insect in insects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, insect.transform.position);
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
            float dist = Vector3.Distance(transform.position, insect.transform.position);
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
            float dist = Vector3.Distance(transform.position, insect.transform.position);
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

    public bool IsValidNightTarget(Insect insect, float distance)
    {
        if (DarknessManager.instance == null || !DarknessManager.instance.isDark) return true;
        if (distance <= attackRange * 0.5f) return true;
        return DarknessManager.instance.IsIlluminated(insect.transform.position);
    }
}
