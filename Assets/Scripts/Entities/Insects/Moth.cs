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
            return CurrentLightTarget();
        }
    }

    // the light plant it goes for, worked out once a frame: target is read several times a frame
    // and every read scanned every plant. worked out again within the frame if that plant dies
    // or is removed
    private Plant _lightTarget;
    private int _lightTargetFrame = -1;

    private Plant CurrentLightTarget()
    {
        bool gone = !ReferenceEquals(_lightTarget, null) && (_lightTarget == null || !_lightTarget.IsAlive);
        if (_lightTargetFrame != Time.frameCount || gone)
        {
            _lightTarget = FindNearestLightPlantInRange();
            _lightTargetFrame = Time.frameCount;
        }
        return _lightTarget;
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

    // its own position read once, and squared distances (the same test without square roots)
    private bool IsNearLight()
    {
        Vector3 here = transform.position;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant.lightEmissionRange <= 0f) continue;
            float range = plant.lightEmissionRange;
            if ((plant.transform.position - here).sqrMagnitude <= range * range)
                return true;
        }
        foreach (Insect insect in allInsects)
        {
            if (insect == null || insect == this || !insect.IsAlive || insect.lightEmissionRange <= 0f) continue;
            float range = insect.lightEmissionRange;
            if ((insect.transform.position - here).sqrMagnitude <= range * range)
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
            if (plant.SeenDistance(dist) <= targetingRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = plant;
            }
        }
        return nearest;
    }

    // WinterMoth adds its own lines after this one
    public override string GetAbilityDescription() =>
        $"Increase <color=green><b>Movement Speed</b></color> by <color=green><b>{IlluminativeHasteEffect.speedBonus * 100f:F0}%</b></color> when illuminated.\n" +
        "Aggressive against plants that emit light.";

    // its data says passive, but it goes after plants that give off light (the target override above)
    public override string AggressivityNote() =>
        $"Except: a Moth goes after the nearest plant that gives off light within its targeting range ({(data != null ? data.baseTargetingRange : targetingRange):0.#}).";
}
