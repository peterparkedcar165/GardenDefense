using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class DarknessManager : MonoBehaviour
{
    public static DarknessManager instance;
    public bool isDark = false;
    // true only for cave levels: insects standing outside any light are not rendered.
    // leave false for nighttime/desert-night, where insects stay visible in shadow
    public bool pitchBlack = false;

    [Header("Setup-phase lighting (cave levels)")]
    [Tooltip("if true, the NightLight is lit during wave 0 (setup) and fades to black just before wave 1")]
    [SerializeField] private bool litDuringSetup = false;
    [SerializeField] private Light2D nightLight;          // assign the scene's NightLight
    [Tooltip("NightLight intensity during the setup phase")]
    [SerializeField] private float setupIntensity = 0.4f;
    [Tooltip("the light reaches 0 this many seconds before wave 1 begins")]
    [SerializeField] private float darkByCountdown = 1f;
    [Tooltip("how long (seconds) the fade from the setup intensity down to 0 takes")]
    [SerializeField] private float fadeDuration = 0.4f;

    private static readonly List<(Transform t, float radius)> _dynamicSources = new List<(Transform, float)>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        _dynamicSources.Clear();
        SceneManager.sceneLoaded += (_, __) => _dynamicSources.Clear();
    }

    void Awake() { instance = this; }

    void Update()
    {
        if (!litDuringSetup) return;

        bool inSetup = GameManager.instance == null || GameManager.instance.currentWave == 0;
        float countdown = GameHUD.instance != null ? GameHUD.instance.NextWaveCountdown : float.PositiveInfinity;

        // held at setupIntensity through setup, then fades to 0 over fadeDuration, reaching 0 when
        // darkByCountdown seconds remain before wave 1. stays 0 from then on (pitch black for the wave)
        float target;
        if (!inSetup)
            target = 0f;
        else
            target = setupIntensity * Mathf.Clamp01((countdown - darkByCountdown) / Mathf.Max(fadeDuration, 0.0001f));

        isDark = target <= 0.001f;   // darkness gameplay kicks in once the NightLight is fully out
        if (nightLight != null) nightLight.intensity = target;
    }

    public static void RegisterLightSource(Transform t, float radius) => _dynamicSources.Add((t, radius));

    public static void UnregisterLightSource(Transform t) => _dynamicSources.RemoveAll(s => s.t == t);

    public bool IsIlluminated(Vector3 position)
    {
        if (!isDark) return true;

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant.lightEmissionRange <= 0) continue;
            if (Vector3.Distance(position, plant.transform.position) <= plant.lightEmissionRange)
                return true;
        }

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect.lightEmissionRange <= 0) continue;
            if (Vector3.Distance(position, insect.transform.position) <= insect.lightEmissionRange)
                return true;
        }

        foreach (Insect insect in Insect.friendlyInsects)
        {
            if (insect == null || !insect.IsAlive || insect.lightEmissionRange <= 0) continue;
            if (Vector3.Distance(position, insect.transform.position) <= insect.lightEmissionRange)
                return true;
        }

        for (int i = _dynamicSources.Count - 1; i >= 0; i--)
        {
            var (t, radius) = _dynamicSources[i];
            if (t == null) { _dynamicSources.RemoveAt(i); continue; }
            if (Vector3.Distance(position, t.position) <= radius)
                return true;
        }

        return false;
    }
}
