using UnityEngine;

// on dark levels, brings a weather effect's particles back up toward their daytime brightness
// where a plant, insect or other light shines (DarknessManager.LightAmount). WeatherManager dims
// the whole effect on night levels (WeatherVisualEntry.nightDimming), which suits the dark ground,
// but a light brightens the ground under it and the dimmed rain gets lost there.
// sits on the effect's root and covers every particle system under it (rain: drops and splashes).
// does nothing on day levels. unlike a lit shader, the effect keeps its own unlit additive look
public class ParticleLightResponse : MonoBehaviour
{
    [Tooltip("brightness of a particle well inside a light, as a share of its daytime brightness. outside any light it keeps the night dimmed brightness")]
    [SerializeField, Range(0f, 1f)] private float litBrightness = 1f;

    private ParticleSystem[] _systems;
    private ParticleSystem.Particle[] _buffer;

    void Start()
    {
        _systems = GetComponentsInChildren<ParticleSystem>(true);
        int size = 0;
        foreach (ParticleSystem ps in _systems)
            size = Mathf.Max(size, ps.main.maxParticles);
        _buffer = new ParticleSystem.Particle[size];
    }

    void LateUpdate()
    {
        DarknessManager dm = DarknessManager.instance;
        if (dm == null || !dm.isDark || _systems == null) return;

        foreach (ParticleSystem ps in _systems)
        {
            ParticleSystem.MainModule main = ps.main;
            // the start colour already carries the night dimming (set when the effect was made)
            Color dark = main.startColor.color;
            Color lit = new Color(litBrightness, litBrightness, litBrightness, dark.a);
            if (dark.r >= lit.r && dark.g >= lit.g && dark.b >= lit.b) continue;

            int count = ps.GetParticles(_buffer);
            if (count == 0) continue;
            bool local = main.simulationSpace == ParticleSystemSimulationSpace.Local;
            Transform t = ps.transform;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = local ? t.TransformPoint(_buffer[i].position) : _buffer[i].position;
                _buffer[i].startColor = Color.Lerp(dark, lit, dm.LightAmount(pos));
            }
            ps.SetParticles(_buffer, count);
        }
    }
}
