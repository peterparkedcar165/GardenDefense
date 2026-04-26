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

    public int sunCost;
    public int exp = 0, expToNextLevel = 52, level = 1, levelCap = 25;
    public float expBoost;
    public int passiveLevel, activeLevel, passiveLevelCap = 5, activeLevelCap = 5;
    public float passiveCooldown, activeCooldown; // most of these are public for debugging purposes

    protected override void Awake()
    {
        baseMaxHealth = 20;
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
        while(exp >= expToNextLevel && level < levelCap)
        {
            LevelUp();
        }
    }

    public void GainExp(float amount)
    {
        exp += (int)(amount * (1 + expBoost));
    }
    public virtual void LevelUp() // virtual so child classes can use this
    {
        exp -= expToNextLevel;
        if (exp < 0) exp = 0;
        level++;
        expToNextLevel = (int) (52 * (1 + level*0.3f));
        if (level % (3) == 0 && passiveLevel < passiveLevelCap)
        {
            UpgradePassive();
            Debug.Log("Passive increased to: " + passiveLevel);
        }
        if (level % 5 == 0 && activeLevel < activeLevelCap) {
            UpgradeActive();
            Debug.Log("Active increased to: " + activeLevel);
        }
        // stat increases will be Base stats, and implemented differently for each plant
    }

    public virtual void UpgradePassive()
    {
        passiveLevel += 1;
    }

    public virtual void UpgradeActive()
    {
        activeLevel += 1;
    }
}
