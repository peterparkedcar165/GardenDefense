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

    protected override void UpdateStats()
    {
        base.UpdateStats();
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
