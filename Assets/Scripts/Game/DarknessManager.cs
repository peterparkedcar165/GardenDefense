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
    [Tooltip("if true, the level is fully lit during wave 0 (setup) and goes dark when wave 1 begins")]
    [SerializeField] private bool litDuringSetup = false;
    [SerializeField] private Light2D globalLight;       // the scene's global light, faded in setup
    [SerializeField] private float lightFadeSpeed = 1.5f;

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

        // wave 0 = setup: keep the level bright; from wave 1 on it goes dark
        bool inSetup = GameManager.instance == null || GameManager.instance.currentWave == 0;
        isDark = !inSetup;
        if (globalLight != null)
        {
            float target = inSetup ? 1f : 0f;
            globalLight.intensity = Mathf.MoveTowards(globalLight.intensity, target, lightFadeSpeed * Time.deltaTime);
        }
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
