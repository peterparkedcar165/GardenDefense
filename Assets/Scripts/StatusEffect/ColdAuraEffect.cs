using UnityEngine;

// lives permanently on the insect that radiates it (e.g. Winter Moth) rather than on each
// plant it chills. the insect moves constantly, so tracking a per-plant instance would mean
// adding/removing it every time a plant enters or leaves range; instead this ticks once per
// frame on its owner and directly cools whatever plants are currently within range.
public class ColdAuraEffect : StatusEffect
{
    private readonly float radius;
    private readonly float coldPerSecond;

    public ColdAuraEffect(Entity target, int level, Entity source, float radius, float coldPerSecond)
        : base(target, float.MaxValue, level, source)
    {
        this.radius = radius;
        this.coldPerSecond = coldPerSecond;
        effectType = Type.positive;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Cold Aura</color>";
    public override string GetDescription() => $"Chills nearby plants, reducing their temperature by <b>{coldPerSecond:F1}</b> per second.";

    public override void OnApply() { }

    public override void OnTick(float deltaTime)
    {
        // the cold is added per deltaTime, so this still runs every frame; the aura position is read once
        Vector3 center = target.transform.position;
        float radiusSqr = radius * radius;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if ((plant.transform.position - center).sqrMagnitude > radiusSqr) continue;
            plant.temperature = Mathf.Max(plant.temperature - coldPerSecond * deltaTime, plant.temperatureMin);
        }
    }

    public override void OnExpire() { }
}
