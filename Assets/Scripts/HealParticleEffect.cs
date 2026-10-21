using UnityEngine;

// one-shot front/back particle burst spawned wherever a heal lands (see Entity.Heal) - at the
// healed entity's visual center (Entity.GetVisualCenter), not the floating-text indicator spot.
// both burst count and particle size scale with how much was healed: floor at 10 healed, ceiling
// at 100 healed, linear between. burst floors at 3 (each of Front and Back) and ceilings at 5;
// size floors at 0.1x and ceilings at 0.5x of the prefab's own authored Start Size, via
// startSizeMultiplier, so the prefab's own Start Size curve/range never needs touching. Shape
// radius also varies by the healed entity's own size: Plant=0.5, Insect=0.25
public static class HealParticleEffect
{
    private const float MinHeal = 10f;
    private const float MaxHeal = 100f;
    private const int MinBurst = 3;
    private const int MaxBurst = 5;
    private const float MinSizeMultiplier = 0.1f;
    private const float MaxSizeMultiplier = 0.5f;
    // random jitter applied on top of the heal-based size, so repeated heals of the same amount
    // don't all render at the exact same size
    private const float SizeVariation = 0.1f;
    private const float Lifetime = 2f;

    private const float PlantShapeRadius = 0.5f;
    private const float InsectShapeRadius = 0.25f;

    public static void Spawn(Entity target, Vector3 position, float amount)
    {
        GameObject prefab = Resources.Load<GameObject>("HealParticles");
        if (prefab == null) return;

        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);

        float t = Mathf.InverseLerp(MinHeal, MaxHeal, Mathf.Max(amount, MinHeal));
        int burstCount = Mathf.RoundToInt(Mathf.Lerp(MinBurst, MaxBurst, t));
        float sizeMultiplier = Mathf.Lerp(MinSizeMultiplier, MaxSizeMultiplier, t)
            + Random.Range(-SizeVariation, SizeVariation);
        sizeMultiplier = Mathf.Max(0.05f, sizeMultiplier);

        float shapeRadius = target is Insect ? InsectShapeRadius : PlantShapeRadius;

        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.EmissionModule emission = ps.emission;
            ParticleSystem.Burst burst = emission.GetBurst(0);
            burst.count = burstCount;
            emission.SetBurst(0, burst);

            ParticleSystem.MainModule main = ps.main;
            main.startSizeMultiplier = sizeMultiplier;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.radius = shapeRadius;
        }

        Object.Destroy(go, Lifetime);
    }
}
