using UnityEngine;

public class Moth : FlyingInsect
{
    private bool isExposedToLight;

    protected override void Awake()
    {
        base.Awake();
        baseMaxHealth     = 400f;
        baseAttackDamage  = 10f;
        baseAttackSpeed   = 1.3f;
        baseAttackRange   = 0.5f;
        baseMovementSpeed = 1.1f;
        targetingRange    = 2.5f;
        sunDrop           = 8;

    }

    public override IAttackable target
    {
        get
        {
            IAttackable taunted = GetEffect<TauntEffect>()?.taunter;
            if (taunted != null) return taunted;
            return FindNearestLightPlantInRange();
        }
    }

    private Plant FindNearestLightPlantInRange()
    {
        Plant nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (plant.lightEmissionRange <= 0f) continue;
            float dist = Vector3.Distance(transform.position, plant.Position);
            if (dist <= targetingRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = plant;
            }
        }
        return nearest;
    }

    public override void UpdateStats()
    {
        base.UpdateStats();

        isExposedToLight = false;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (plant.lightEmissionRange <= 0f) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) <= plant.lightEmissionRange)
            {
                isExposedToLight = true;
                break;
            }
        }
        if (!isExposedToLight)
        {
            foreach (Insect insect in allInsects)
            {
                if (insect == null || insect == this || !insect.IsAlive) continue;
                if (insect.lightEmissionRange <= 0f) continue;
                if (Vector3.Distance(transform.position, insect.transform.position) <= insect.lightEmissionRange)
                {
                    isExposedToLight = true;
                    break;
                }
            }
        }

        if (isExposedToLight)
        {
            movementSpeed += 0.4f;
            flightSpeed = 2f * movementSpeed;
        }
    }

    public override string GetName() => "<b><color=#C8A2C8>Moth</color></b>";

    public override string GetDescription()
        => $"The {GetName()} is passive and docile — until it spots a light source. It will then relentlessly pursue the nearest illuminated plant.";

    public override string GetPassiveDescription()
        => $"The {GetName()} is drawn to light. Gains <color=green><b>+0.4</b></color> movement speed while inside any plant's light radius. Provoked by any plant emitting light within range.";
}
