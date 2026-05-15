using UnityEngine;

public class DarknessManager : MonoBehaviour
{
    public static DarknessManager instance;

    public bool isDark = false;

    void Awake()
    {
        instance = this;
    }

    public bool IsIlluminated(Vector3 position)
    {
        if (!isDark) return true;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant.lightEmissionRange <= 0) continue;
            if (Vector3.Distance(position, plant.transform.position) <= plant.lightEmissionRange)
                return true;
        }
        return false;
    }
}
