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
    }

    public override IAttackable target
    {
        get
        {
            // hypnotized (friendly) moths fight enemy insects like any other friendly
            if (team == Team.Friendly) return FindNearestEnemyInRange();
            IAttackable taunted = GetEffect<TauntEffect>()?.taunter;
            if ((taunted as Object) != null) return taunted;
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

    // split out so WinterMoth can inherit this exact text and append its own extra lines while
    // still letting Moth.GetDescription() control the final Flying/Aggressivity ordering
    protected virtual string DescriptionCore() =>
        "Passive insect. Becomes highly aggressive against plants emitting light. Increase Movement Speed by " +
        $"<color=green><b>{IlluminativeHasteEffect.speedBonus * 100f:F0}%</b></color> when illuminated.";

    public override string GetDescription() => DescriptionCore() + FlyingLine() + AggressivityLine();
}
