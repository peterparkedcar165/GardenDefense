using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DarknessManager : MonoBehaviour
{
    public static DarknessManager instance;
    public bool isDark = false;

    private static readonly List<(Transform t, float radius)> _dynamicSources = new List<(Transform, float)>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        _dynamicSources.Clear();
        SceneManager.sceneLoaded += (_, __) => _dynamicSources.Clear();
    }

    void Awake() { instance = this; }

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
