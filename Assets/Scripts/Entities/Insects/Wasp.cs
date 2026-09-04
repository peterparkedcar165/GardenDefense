using UnityEngine;

public class Wasp : FlyingInsect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    // only ever substitutes in a Photosynthesis target when the base targeting logic already
    // decided to attack a plant at all (so taunt/oblivious/engagement/aggressivity gating from
    // the base property is fully preserved) - otherwise falls back to the normal nearest pick
    public override IAttackable target
    {
        get
        {
            IAttackable baseTarget = base.target;
            if (baseTarget is Plant)
            {
                Plant photosynthesis = FindNearestPhotosynthesisPlantInRange();
                if (photosynthesis != null) return photosynthesis;
            }
            return baseTarget;
        }
    }

    private Plant FindNearestPhotosynthesisPlantInRange()
    {
        Plant nearest = null;
        float nearestDist = float.MaxValue;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (plant.data == null || plant.data.family != PlantFamily.Photosynthesis) continue;
            if (!CanReachPlant(plant)) continue;
            float dist = Vector3.Distance(transform.position, plant.GetApproachPoint(transform.position));
            if (dist > targetingRange || dist >= nearestDist) continue;
            nearestDist = dist;
            nearest = plant;
        }
        return nearest;
    }

    public override string GetDescription() =>
        "Mildly aggressive insect. Stronger aggressivity towards Photosynthesis plants." + FlyingLine() + AggressivityLine();
}
