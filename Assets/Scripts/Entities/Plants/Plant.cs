using UnityEngine;

public enum PlantType
{
    Neutral,
    Diurnal,
    Nocturnal,
    Arid,
    Aquatic,
    Lush
}
public abstract class Plant : Entity
{
    public PlantData data;
    public Tile occupiedTile;

    [SerializeField] private Transform circleRadius;
    protected override void UpdateStats()
    {
        base.UpdateStats();
        float plantSpriteRadius = GetComponent<CircleCollider2D>().radius*2;

        if (circleRadius != null)
        {
            circleRadius.localScale = new Vector3((attackRange * 2f)  + plantSpriteRadius, (attackRange * 2f) + plantSpriteRadius, 1f);
        }
    }
    public TileType[] allowedTiles;
    public int sunCost, totalSunSpent = 0;
    public int exp = 0;
    public float expBoost;
    public float activeCooldown; // most of these are public for debugging purposes

    [Header("Paths")]
    public int path1Level, path2Level, path3Level, path1LevelAdder, path2LevelAdder, path3LevelAdder, effectivePath1Level, effectivePath2Level, effectivePath3Level;
    public bool path3Unlocked;


    protected override void Awake()
    {
        baseMaxHealth = 100;
        base.Awake();
        baseCriticalChance = 0.05f;
        baseCriticalDamage = 1.75f;
    }

    protected virtual void Start()
    {
        if (circleRadius != null)
            circleRadius.gameObject.SetActive(false);
    }

    protected override void OnHover()
    {
        Debug.Log("OnHover called, circleRadius is: " + circleRadius);
        if (circleRadius != null)
            circleRadius.gameObject.SetActive(true);
    }

    protected override void OnHoverExit()
    {
        Debug.Log("OnHoverExit called");
        if (circleRadius != null)
            circleRadius.gameObject.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();
        effectivePath1Level = path1Level + path1LevelAdder;
        effectivePath2Level = path2Level + path2LevelAdder;
        effectivePath3Level = path3Level + path3LevelAdder;
    }

    public void Uproot()
    {
        GameManager.instance.SunCount += (int)(totalSunSpent * 0.5);
        Debug.Log("Uprooted " + GetName() + " and refunded " + (int)(totalSunSpent * 0.5));
        GameManager.instance.UpdateSun();
        // need some sound effects eventually
        occupiedTile.isOccupied = false;
        PlantSelector.instance.uprootMode = false;
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

    public int GetPath1Cost() => sunCost / 2 + 28 * path1Level;
    public int GetPath2Cost() => sunCost / 2 + 28 * path2Level;
    public int GetPath3Cost() => Mathf.RoundToInt(sunCost * 0.75f + 28 * path3Level);

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
        effectivePath2Level = path2Level + path2LevelAdder;
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
        if (PlantSelector.instance.uprootMode)
        {
            Uproot();
            return;
        }
        PlantUpgradeUI.instance.ShowPanel(this);
    }

    // PATH NAMES & HOVER DESCRIPTIONS
    public virtual string GetPath1Name() => "Path 1";
    public virtual string GetPath2Name() => "Path 2";
    public virtual string GetPath3Name() => "Path 3";
    public virtual string GetPath1Description() => "";
    public virtual string GetPath2Description() => "";
    public virtual string GetPath3Description() => "";

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
}
