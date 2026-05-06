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
            circleRadius.localScale = new Vector3((attackRange * 2f)  + plantSpriteRadius, (attackRange * 2f) + plantSpriteRadius, 1f); // INCLUDES SPRITE
            // circleRadius.localScale = new Vector3(attackRange * 2f, attackRange * 2f, 1f);
        }
    }
    public TileType[] allowedTiles;
    public int sunCost, totalSunSpent = 0;
    public ElementalType elementalType;
    public DamageType damageType;
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
            
        if (elementalType == ElementalType.Water && occupiedTile != null) // WATER BONUS
        {
            if (occupiedTile.tileType == TileType.Water || occupiedTile.isWaterAdjacent){
                path2LevelAdder +=2;
            }
        }
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
        occupiedTile.GetComponent<Collider2D>().enabled = true;
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

    public int GetPath1Cost() => Mathf.RoundToInt((sunCost*0.75f) + (32 * path1Level));
    public int GetPath2Cost() => Mathf.RoundToInt((sunCost*0.75f) + (32 * path2Level));
    public int GetPath3Cost() => Mathf.RoundToInt((sunCost * 1f) + (36 * path3Level));

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
            return $"Increase Passive tree level by <color=green>1</color> when exposed to light";

            case ElementalType.Nature:
            return $"Can be placed on <color=green>Grass</color>.";

            case ElementalType.Water:
            return $"Increase Passive tree level by <color=green>2</color> when near water";

            case ElementalType.Poison:
            return $"Deal 25% increased damage to immobilized insects";

            case ElementalType.Ice:
            return $"Increase Passive tree level by <color=green>1</color> when near cold";

            case ElementalType.Wind:
            return $"Increase Passive tree level by <color=green>1</color> when up high";

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
}
