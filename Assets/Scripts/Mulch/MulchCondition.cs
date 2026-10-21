using System.Collections.Generic;
using UnityEngine;

// one "at least one of these" requirement - a bare array can't nest in the Inspector (no
// array-of-arrays), so each OR group needs this thin wrapper. a single-entry group behaves as a
// plain AND requirement for that one element/family/plant - see MulchCondition for how groups
// combine. excludedPlants (element/family groups only) lets a specific plant NOT count toward
// satisfying the group even though its element/family matches - e.g. a Grass requirement that
// Rhodiola shouldn't be able to satisfy on her own, since she doesn't deal direct damage
[System.Serializable]
public class ElementGroup
{
    public ElementalType[] anyOf;
    public PlantData[] excludedPlants;
}

[System.Serializable]
public class FamilyGroup
{
    public PlantFamily[] anyOf;
    public PlantData[] excludedPlants;
}

// one "plant" alternative within a PlantGroup - a bare plant presence check by default, but can
// additionally demand that specific plant have a skill tree node purchased and/or a live, placed,
// alive instance with a given upgrade path maxed. e.g. Rejuvenator's Calendula/Tansy entries also
// require their own Radiant Aura unlock, since without it they heal with no Regeneration tag at
// all - a flat "Calendula is in the loadout" check alone would be wrong. leave requiredUnlockId
// blank and requiredMaxedPath at 0 for a plain "just needs to be in the loadout" entry, same as
// before this existed
[System.Serializable]
public class QualifiedPlant
{
    public PlantData plant;
    [Tooltip("leave blank if just being in the loadout is enough")]
    public string requiredUnlockId;
    [Tooltip("0 = no path requirement; else 1/2/3 = that path must be maxed on a currently " +
        "placed, alive instance of this plant (checked live, since path levels reset every level)")]
    [Range(0, 3)]
    public int requiredMaxedPath;

    public bool IsSatisfied(List<PlantData> loadout)
    {
        if (plant == null || !LoadoutHasThisPlant(loadout)) return false;

        if (!string.IsNullOrEmpty(requiredUnlockId) &&
            !SkillTreeManager.HasUnlock(plant.plantName, plant.skillTree, requiredUnlockId))
            return false;

        if (requiredMaxedPath > 0 && !HasMaxedPlacedInstance()) return false;

        return true;
    }

    private bool LoadoutHasThisPlant(List<PlantData> loadout)
    {
        foreach (PlantData data in loadout)
            if (data == plant) return true;
        return false;
    }

    private bool HasMaxedPlacedInstance()
    {
        foreach (Plant placed in Plant.allPlants)
        {
            if (placed == null || !placed.IsAlive || placed.data != plant) continue;
            bool maxed = requiredMaxedPath switch
            {
                1 => placed.IsPath1Maxed,
                2 => placed.IsPath2Maxed,
                3 => placed.IsPath3Maxed,
                _ => false,
            };
            if (maxed) return true;
        }
        return false;
    }
}

// Rejuvenator requiring any ONE of a hand-picked list of "plants with a regeneration effect",
// some of which only qualify once their own gating unlock is bought - see QualifiedPlant
[System.Serializable]
public class PlantGroup
{
    public QualifiedPlant[] anyOf;
}

// eligibility gate for whether a mulch is allowed to appear in a draft at all, checked against
// the player's current loadout and the wave the draft happens on, plus (for
// requiredPlantGroups' QualifiedPlant entries) live field state and persistent skill tree saves -
// so "this plant is in the loadout" and "this plant has actually unlocked the specific mechanic
// the mulch cares about" can be required separately. unlike FertilizerData's
// targetFamilies/targetElementalTypes (an OR set used for a different purpose entirely - who an
// already-active fertilizer's bonus applies to), this is AND-of-ORs: every group listed must have
// at least one match (AND between groups), and within one group only a single
// match is needed (OR within that group). e.g. to require "Fire AND Grass", use two single-entry
// groups ({Fire}, {Grass}); to require "Fire OR Water", use one group with both in it
// ({Fire, Water})
[System.Serializable]
public class MulchCondition
{
    [Tooltip("every group must have at least one match (AND between groups, OR within a group). " +
        "leave empty to not require any")]
    public FamilyGroup[] requiredFamilyGroups;
    [Tooltip("every group must have at least one match (AND between groups, OR within a group). " +
        "leave empty to not require any")]
    public ElementGroup[] requiredElementGroups;
    [Tooltip("every group must have at least one of its specific plants present and satisfying " +
        "its own optional unlock/maxed-path qualifier (AND between groups, OR within a group). " +
        "leave empty to not require any")]
    public PlantGroup[] requiredPlantGroups;
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

        if (requiredFamilyGroups != null)
            foreach (FamilyGroup group in requiredFamilyGroups)
            {
                if (group == null || group.anyOf == null || group.anyOf.Length == 0) continue;
                bool anyMatch = false;
                foreach (PlantFamily family in group.anyOf)
                    if (LoadoutHasFamily(loadout, family, group.excludedPlants)) { anyMatch = true; break; }
                if (!anyMatch) return false;
            }

        if (requiredElementGroups != null)
            foreach (ElementGroup group in requiredElementGroups)
            {
                if (group == null || group.anyOf == null || group.anyOf.Length == 0) continue;
                bool anyMatch = false;
                foreach (ElementalType element in group.anyOf)
                    if (LoadoutHasElement(loadout, element, group.excludedPlants)) { anyMatch = true; break; }
                if (!anyMatch) return false;
            }

        if (requiredPlantGroups != null)
            foreach (PlantGroup group in requiredPlantGroups)
            {
                if (group == null || group.anyOf == null || group.anyOf.Length == 0) continue;
                bool anyMatch = false;
                foreach (QualifiedPlant requirement in group.anyOf)
                    if (requirement != null && requirement.IsSatisfied(loadout)) { anyMatch = true; break; }
                if (!anyMatch) return false;
            }

        if (excludedFamilies != null)
            foreach (PlantFamily family in excludedFamilies)
                if (LoadoutHasFamily(loadout, family, null)) return false;

        return true;
    }

    private static bool LoadoutHasFamily(List<PlantData> loadout, PlantFamily family, PlantData[] excludedPlants)
    {
        foreach (PlantData data in loadout)
        {
            if (data == null || data.family != family) continue;
            if (IsExcluded(data, excludedPlants)) continue;
            return true;
        }
        return false;
    }

    private static bool LoadoutHasElement(List<PlantData> loadout, ElementalType element, PlantData[] excludedPlants)
    {
        foreach (PlantData data in loadout)
        {
            if (data == null || data.elementalType != element) continue;
            if (IsExcluded(data, excludedPlants)) continue;
            return true;
        }
        return false;
    }

    private static bool IsExcluded(PlantData data, PlantData[] excludedPlants)
    {
        if (excludedPlants == null) return false;
        foreach (PlantData excluded in excludedPlants)
            if (excluded == data) return true;
        return false;
    }
}
