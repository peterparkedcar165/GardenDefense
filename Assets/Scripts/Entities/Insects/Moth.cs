using UnityEngine;

public class Moth : FlyingInsect
{
    private float scanTimer = 0f;
    private const float scanInterval = 0.1f;
    private const float buffDuration = 0.15f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        targetingRange = 2.5f;
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

    protected override void Update()
    {
        base.Update();
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            UpdateLightCheck();
        }
    }

    private void UpdateLightCheck()
    {
        if (IsNearLight())
        {
            IlluminativeHasteEffect existing = GetEffect<IlluminativeHasteEffect>();
            if (existing != null)
                existing.duration = buffDuration;
            else
                ApplyEffect(new IlluminativeHasteEffect(this, buffDuration, 1, this));
        }
    }

    private bool IsNearLight()
    {
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant.lightEmissionRange <= 0f) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) <= plant.lightEmissionRange)
                return true;
        }
        foreach (Insect insect in allInsects)
        {
            if (insect == null || insect == this || !insect.IsAlive || insect.lightEmissionRange <= 0f) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= insect.lightEmissionRange)
                return true;
        }
        return false;
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
}
