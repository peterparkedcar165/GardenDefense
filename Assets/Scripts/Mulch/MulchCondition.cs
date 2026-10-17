using System.Collections.Generic;
using UnityEngine;

// eligibility gate for whether a mulch is allowed to appear in a draft at all, checked against
// the player's current loadout and the wave the draft happens on. every populated list below is
// ANDed together - unlike FertilizerData's targetFamilies/targetElementalTypes, which are an OR
// set used for a different purpose entirely (who an already-active fertilizer's bonus applies to)
[System.Serializable]
public class MulchCondition
{
    [Tooltip("every family listed must be present in the loadout. leave empty to not require any")]
    public PlantFamily[] requiredFamilies;
    [Tooltip("every element listed must be present in the loadout. leave empty to not require any")]
    public ElementalType[] requiredElements;
    [Tooltip("none of these families may be present in the loadout")]
    public PlantFamily[] excludedFamilies;
    [Tooltip("0 means no minimum")]
    public int minWave;
    [Tooltip("0 means no maximum")]
    public int maxWave;

    public bool IsEligible(List<PlantData> loadout, int currentWave)
    {
        if (minWave > 0 && currentWave < minWave) return false;
        if (maxWave > 0 && currentWave > maxWave) return false;

        if (requiredFamilies != null)
            foreach (PlantFamily family in requiredFamilies)
                if (!LoadoutHasFamily(loadout, family)) return false;

        if (requiredElements != null)
            foreach (ElementalType element in requiredElements)
                if (!LoadoutHasElement(loadout, element)) return false;

        if (excludedFamilies != null)
            foreach (PlantFamily family in excludedFamilies)
                if (LoadoutHasFamily(loadout, family)) return false;

        return true;
    }

    private static bool LoadoutHasFamily(List<PlantData> loadout, PlantFamily family)
    {
        foreach (PlantData data in loadout)
            if (data != null && data.family == family) return true;
        return false;
    }

    private static bool LoadoutHasElement(List<PlantData> loadout, ElementalType element)
    {
        foreach (PlantData data in loadout)
            if (data != null && data.elementalType == element) return true;
        return false;
    }
}
