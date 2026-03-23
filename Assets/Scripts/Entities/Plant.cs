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
    public float physicalShred, magicShred, bonusEffectChance;

    public int sunCost;

    protected override void Awake()
    {
        base.Awake();
        maxHealth = 20;
    }

    protected void Start()
    {
        
    }

    protected override void Update()
    {
        base.Update();
    }

}
