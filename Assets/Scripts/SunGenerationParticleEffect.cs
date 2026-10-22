using UnityEngine;

// one-shot front/back particle burst spawned wherever a Photosynthesis-family plant generates
// sun from its own passive (Sunflower's book tick, BogIris's sun tick, Kelp's Oxygen threshold -
// NOT one-off/event sun like Glowshroom's on-kill burst or Aeonium's blessing). burst count
// scales with the amount of sun generated: floor at 2 sun (2 particles, each of Front and
// Back), ceiling at 20 sun (4 particles, each), linear between. shapeRadius is passed in by the
// caller since it varies by plant (Sunflower spawns smaller/tighter on its book, 0.25; others
// spawn centered on their own visual at 0.5)
public static class SunGenerationParticleEffect
{
    private const float MinSun = 2f;
    private const float MaxSun = 20f;
    private const int MinBurst = 2;
    private const int MaxBurst = 4;
    private const float Lifetime = 2f;

    public static void Spawn(Vector3 position, int amount, float shapeRadius)
    {
        GameObject prefab = Resources.Load<GameObject>("SunGenerationParticles");
        if (prefab == null) return;

        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);

        float t = Mathf.InverseLerp(MinSun, MaxSun, Mathf.Max(amount, MinSun));
        int burstCount = Mathf.RoundToInt(Mathf.Lerp(MinBurst, MaxBurst, t));

        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.EmissionModule emission = ps.emission;
            ParticleSystem.Burst burst = emission.GetBurst(0);
            burst.count = burstCount;
            emission.SetBurst(0, burst);

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.radius = shapeRadius;
        }

        Object.Destroy(go, Lifetime);
    }
}
