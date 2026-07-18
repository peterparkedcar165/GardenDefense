using UnityEngine;

// emits a flat 2d particle cone along a direction, shared by stargazer and rhodiola
// each particle gets a velocity at a random angle within the cone around the aim direction,
// sidestepping the 3d shape module so the fan is always flat, symmetric and evenly spread.
// the prefab particle system should have Emission Rate over Time 0, all emission happens here
public class ConeParticleEmitter
{
    private readonly ParticleSystem particles;
    private readonly float rate;      // particles per second while active
    private readonly float reachMin;  // each particle overshoots the range by a random
    private readonly float reachMax;  // factor in this interval for a ragged edge
    private float emitAccumulator;

    public ConeParticleEmitter(ParticleSystem particles, float rate = 250f, float reachMin = 1.1f, float reachMax = 1.2f)
    {
        this.particles = particles;
        this.rate      = rate;
        this.reachMin  = reachMin;
        this.reachMax  = reachMax;
    }

    public void Update(bool active, Vector2 direction, float coneAngle, float range, float travelTime)
    {
        if (particles == null) return;

        // disable automatic emission so the shape never spawns stationary particles on the plant
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        // reach: a particle crosses the range plus a random overshoot in travelTime seconds.
        // lifetime is fixed, speed is randomized per particle below
        float travel = travelTime > 0f ? travelTime : 0.25f;
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = travel;
        main.startSpeed    = 0f;

        if (!active) { emitAccumulator = 0f; return; }

        // accumulate fractional emissions so the rate is smooth across frames
        emitAccumulator += rate * Time.deltaTime;
        int count = Mathf.FloorToInt(emitAccumulator);
        emitAccumulator -= count;

        float baseAngle = Mathf.Atan2(direction.y, direction.x);
        float half      = coneAngle * 0.5f * Mathf.Deg2Rad;

        for (int i = 0; i < count; i++)
        {
            float a = baseAngle + Random.Range(-half, half);
            float speed = range * Random.Range(reachMin, reachMax) / travel;
            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
            {
                velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * speed
            };
            particles.Emit(ep, 1);
        }
    }
}
