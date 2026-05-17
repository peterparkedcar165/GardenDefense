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
    public float fieryInfusionHeal;
}

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
    [SerializeField] private float lightIntensity = 1f;
    [SerializeField] private float lightInnerRadius = 1.2f;
    [SerializeField] private float lightFalloffStrength = 0.2f;
    protected virtual bool ShowLight => true;
    protected virtual bool ShowDarkCircle => true;

    public override void UpdateStats()
    {
        base.UpdateStats();
        passiveCooldown = basePassiveCooldown + passiveCooldownAdder + (basePassiveCooldown * passiveCooldownMultiplier) - (basePassiveCooldown * passiveCooldownReductionMultiplier);
        skillCooldown = baseSkillCooldown - skillCooldownReductionAdder - (baseSkillCooldown * skillCooldownReductionMultiplier);
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
    public int sunCost, totalSunSpent = 0;
    public ElementalType elementalType;
    public DamageType damageType;
    public int exp = 0;
    public float expBoost;
    public float activeCooldown;

    [Header("Passive")]
    public float basePassiveCooldown, passiveCooldown, passiveCooldownAdder, passiveCooldownReductionMultiplier, passiveCooldownMultiplier;
    public float passiveCooldownTimer;

    [Header("Skill")]
    public float baseSkillCooldown, skillCooldown, skillCooldownReductionAdder, skillCooldownReductionMultiplier;
    public float skillCooldownTimer;
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
        FertilizerManager.instance?.ApplyTo(this);
    }

    private int[] GetAllSortingLayerIDs()
    {
        var layers = UnityEngine.SortingLayer.layers;
        int[] ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            ids[i] = layers[i].id;
        return ids;
    }

    void OnDestroy()
    {
        allPlants.Remove(this);
        if (PlantUpgradeUI.instance != null && PlantUpgradeUI.instance.GetSelectedPlant() == this)
            PlantUpgradeUI.instance.HidePanel();
    }

    protected virtual void Start()
    {
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
    }

    protected override void OnHoverExit()
    {
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
    public virtual PlantBaseStats GetBaseStats() => default;

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
            return $"Increase Passive tree level by <color=green>1</color> when exposed to sunlight";

            case ElementalType.Nature:
            return $"Can be placed on <color=green>Grass</color>.";

            case ElementalType.Water:
            return $"Increase Passive tree level by <color=green>1</color> when near water";

            case ElementalType.Poison:
            return $"Deal 25% increased damage to immobilized insects";

            case ElementalType.Ice:
            return $"Increase Passive tree level by <color=green>1</color> when in cold weather";

            case ElementalType.Wind:
            return $"Increase Passive tree level by <color=green>1</color> when in high altitude";

            default:
            return "";
        }
    }

    // DESCRIPTIONS
    public virtual string GetName()
    {
        return "";
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
    }
    public bool IsAlive => health > 0;
    public Vector3 Position => transform.position;

    public bool IsValidNightTarget(Insect insect, float distance)
    {
        if (DarknessManager.instance == null || !DarknessManager.instance.isDark) return true;
        if (distance <= attackRange * 0.5f) return true;
        return DarknessManager.instance.IsIlluminated(insect.transform.position);
    }
}
