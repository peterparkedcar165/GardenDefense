using UnityEngine;

// sizes a one shot burst so its particles travel out to a given radius and stop there: fixed
// lifetime, start speed 70 to 100% of radius over lifetime, speed limited and eased down to 0 over
// each particle's life, alpha holding until halfway then fading out. shared so Calendula's fire
// burst (her attack range) and Waterlily's bubble hit (her splash radius) reach exactly as far as
// their own radius
public static class ParticleRadiusFit
{
    // built once and shared, the particle system copies both when they are assigned, so nothing
    // here is changed afterwards
    private static readonly AnimationCurve LimitCurve = new AnimationCurve(
        new Keyframe(0f,   1f, 0f, 0f),
        new Keyframe(0.5f, 1f, 0f, 0f),
        new Keyframe(1f,   0f, 0f, 0f)
    );
    private static readonly Gradient FadeGradient = BuildFadeGradient();

    private static Gradient BuildFadeGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        return gradient;
    }

    public static void FitBurstToRadius(ParticleSystem ps, float radius, float lifetime)
    {
        if (ps == null || lifetime <= 0f) return;

        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            radius * 0.7f / lifetime,
            radius        / lifetime);

        var lvol = ps.limitVelocityOverLifetime;
        lvol.enabled = true;
        lvol.separateAxes = false;
        lvol.dampen = 0.4f;
        lvol.limit = new ParticleSystem.MinMaxCurve(radius / lifetime, LimitCurve);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(FadeGradient);
    }
}
