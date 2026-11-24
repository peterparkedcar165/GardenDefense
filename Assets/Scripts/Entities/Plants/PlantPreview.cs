using System.Collections.Generic;
using UnityEngine;

// the in-game texts of a plant that isn't on the field (the loadout screen's plant tooltip, the
// encyclopedia), with the numbers of a plant just placed: every path at level 0 (Tommy, 2026-11-24).
// those numbers live in the plant's own stats, which only exist once it's set up, so a copy of its
// prefab is made far off the map, set up like a placed plant (Awake), read, and destroyed before it
// runs a single frame. Plant.BuildingPreview keeps the copy from telling anything it was placed or
// removed. one copy per plant, kept until ClearCache (the loadout screen clears it when it opens,
// since the skill tree can change between levels)
public static class PlantPreview
{
    public struct Texts
    {
        public string name;
        public string description;
        public string attack;
        public string passive;
        public string skill;
    }

    private static readonly Dictionary<PlantData, Texts> cache = new Dictionary<PlantData, Texts>();

    public static void ClearCache() => cache.Clear();

    public static Texts For(PlantData data)
    {
        if (data == null) return default;
        if (cache.TryGetValue(data, out Texts texts)) return texts;
        texts = Build(data);
        cache[data] = texts;
        return texts;
    }

    private static Texts Build(PlantData data)
    {
        Plant prefab = data.plantPrefab;
        if (prefab == null) return new Texts { name = data.displayName, description = "", attack = "", passive = "", skill = "" };

        Plant copy = null;
        Plant.BuildingPreview = true;
        try
        {
            copy = Object.Instantiate(prefab, new Vector3(100000f, 100000f, 0f), Quaternion.identity);
            return Read(copy);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PlantPreview: {data.displayName} couldn't be set up for its tooltip ({e.Message}); its numbers come from the prefab");
            try { return Read(prefab); }
            catch (System.Exception) { return new Texts { name = data.displayName, description = "", attack = "", passive = "", skill = "" }; }
        }
        finally
        {
            if (copy != null) Object.DestroyImmediate(copy.gameObject);
            Plant.BuildingPreview = false;
        }
    }

    private static Texts Read(Plant plant) => new Texts
    {
        name = plant.GetName(),
        description = WithoutScaling(plant.GetDescription()),
        attack = WithoutScaling(plant.GetAttackDescription()),
        passive = WithoutScaling(plant.GetPassiveDescription()),
        skill = WithoutScaling(plant.GetSkillDesription()),
    };

    // a bracketed, coloured bonus after a number: magic power [+12], a share of attack damage [33%],
    // armor scaling [+12]. the level texts show them; off the field they're left out (Tommy, 2026-11-24).
    // a plain bracketed word, like [Punctured], stays
    private static readonly System.Text.RegularExpressions.Regex Scaling = new System.Text.RegularExpressions.Regex(
        @" ?\[<color=[^>]+><b>[^\[\]<]*</b></color>\]", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string WithoutScaling(string text) => string.IsNullOrEmpty(text) ? text : Scaling.Replace(text, "");
}
