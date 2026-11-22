using UnityEngine;
using System.Collections.Generic;

// the level's fertilizer: one is picked before the level from LevelConfig.fertilizers (see
// FertilizerSelectionUI) and stays active until the level ends. session only, reset at every level
// start (ProceduralLevel.Start). the level wide values below are what fertilizer effects nudge;
// the game code they change reads them directly, and ResetForLevel puts them back to neutral
public class FertilizerManager : MonoBehaviour
{
    public static FertilizerManager instance;

    // greedy growth raises it: what a leaked insect's damage is multiplied by
    public static float LeakDamageMultiplier = 1f;
    // rekindling: every burn a plant applies lasts this much of its normal duration (BurnEffect)
    public static float BurnDurationMultiplier = 1f;
    // melt: read by Entity.Damage's freeze and fire branch, swapping the instant shatter for a
    // gradual melt
    public static bool MeltActive = false;
    public static float MeltFreezeDurationReduction = 0f;
    public static float MeltDamageMultiplier = 1f;
    // market day: what placing a plant costs (GetPlacementCost) and what a path upgrade costs
    // (Plant.PathCost), as multipliers
    public static float PlacementCostMultiplier = 1f;
    public static float UpgradeCostMultiplier = 1f;
    // solar loan: sun still owed after the loan came due. every sun gained pays this off first
    // (GameManager.AddSun)
    public static int SunDebt = 0;
    // clean garden: every this many waves, an Epic mulch drops at the end of the wave on top of the
    // guaranteed one (0 = off). read by MulchManager
    public static int EpicMulchEveryWaves = 0;

    private FertilizerData active;
    private FertilizerEffect activeEffect;

    public FertilizerData Active => active;
    public string ActiveStatus => activeEffect?.GetStatus();

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // called once per level start (ProceduralLevel.Start)
    public void ResetForLevel()
    {
        activeEffect?.Deactivate();
        activeEffect = null;
        active = null;

        LeakDamageMultiplier = 1f;
        BurnDurationMultiplier = 1f;
        MeltActive = false;
        MeltFreezeDurationReduction = 0f;
        MeltDamageMultiplier = 1f;
        PlacementCostMultiplier = 1f;
        UpgradeCostMultiplier = 1f;
        SunDebt = 0;
        EpicMulchEveryWaves = 0;
    }

    // the pre level pick. picking again (the dev menu does) swaps the old one out first
    public void Commit(FertilizerData fertilizer)
    {
        activeEffect?.Deactivate();
        activeEffect = null;
        active = fertilizer;
        if (fertilizer == null) return;

        activeEffect = fertilizer.CreateEffect();
        activeEffect?.Activate();
        if (activeEffect != null)
            foreach (Plant plant in new List<Plant>(Plant.allPlants))
                if (plant != null && plant.IsAlive) activeEffect.OnPlantPlaced(plant);
    }

    // a level offers exactly this many, fertile start first (Tommy, 2026-11-21: at most 3;
    // 2026-11-22: always 3)
    public const int MaxOffered = 3;

    // the fertilizers of a level's list the current loadout can use, in list order, at most MaxOffered.
    // when the list comes up short (too short, or melt or rekindling hidden by the loadout) the rest
    // is filled from every fertilizer (MulchRegistry.fertilizers), so the screen always shows 3
    public static List<FertilizerData> GetOffered(FertilizerData[] list)
    {
        List<PlantData> loadout = GetCurrentLoadout();
        List<FertilizerData> result = new List<FertilizerData>();
        if (list != null)
        {
            foreach (FertilizerData data in list)
            {
                if (result.Count >= MaxOffered) break;
                if (data != null && !result.Contains(data) && data.IsOffered(loadout)) result.Add(data);
            }
        }
        if (result.Count < MaxOffered) FillFromRegistry(result, list, loadout);
        return result;
    }

    // the fill walks the registry from a spot set by the level number, so a level always fills the
    // same way for the same loadout. it skips what the level already lists and every weather pick
    // (sunny, rainy, snowy day, greenhouse): a fill shouldn't undo a level's weather or darkness
    private static void FillFromRegistry(List<FertilizerData> result, FertilizerData[] list, List<PlantData> loadout)
    {
        MulchRegistry registry = Resources.Load<MulchRegistry>("MulchRegistry");
        if (registry == null || registry.fertilizers == null || registry.fertilizers.Length == 0) return;
        FertilizerData[] all = registry.fertilizers;
        int level = ProceduralLevel.CurrentConfig != null ? Mathf.Max(0, ProceduralLevel.CurrentConfig.levelNumber) : 0;
        for (int i = 0; i < all.Length && result.Count < MaxOffered; i++)
        {
            FertilizerData data = all[(level + i) % all.Length];
            if (data == null || result.Contains(data) || ChangesWeather(data)) continue;
            if (list != null && System.Array.IndexOf(list, data) >= 0) continue;
            if (data.IsOffered(loadout)) result.Add(data);
        }
    }

    private static bool ChangesWeather(FertilizerData data) =>
        data is WeatherFertilizerData || data is GreenhouseFertilizerData;

    public static List<PlantData> GetCurrentLoadout()
    {
        List<PlantData> result = new List<PlantData>();
        PlantRegistry registry = Resources.Load<PlantRegistry>("PlantRegistry");
        if (SaveManager.instance == null || registry == null || registry.plants == null) return result;
        foreach (string plantName in SaveManager.instance.selectedLoadout)
            foreach (PlantData data in registry.plants)
                if (data != null && data.plantName == plantName) { result.Add(data); break; }
        return result;
    }

    public void NotifyPlantPlaced(Plant plant) => activeEffect?.OnPlantPlaced(plant);
    public void NotifyWaveStarted(int wave) => activeEffect?.OnWaveStarted(wave);
    public void NotifyWaveEnded(int wave) => activeEffect?.OnWaveEnded(wave);
    public void NotifyInsectLeaked(Insect insect) => activeEffect?.OnInsectLeaked(insect);
    public void NotifyPlantBought(Plant plant) => activeEffect?.OnPlantBought(plant);

    // what placing a fresh plant of this species costs right now: the skill tree's reduction, then
    // market day's multiplier, then flat discounts (bargain seeds). Tile charges this and
    // PlantSlotButton shows it, so the shown and charged cost always agree
    public static int GetPlacementCost(PlantData data)
    {
        int cost = SkillTreeManager.GetEffectiveSunCost(data);
        if (data == null) return cost;
        cost = Mathf.RoundToInt(cost * Mathf.Max(0f, PlacementCostMultiplier));
        int discount = instance != null && instance.activeEffect != null ? instance.activeEffect.PlacementDiscount : 0;
        return Mathf.Max(0, cost - discount);
    }

    // solar loan: pays the debt off out of sun just gained, returns what's left to bank
    public static int PayDebtFrom(int gained)
    {
        if (SunDebt <= 0 || gained <= 0) return gained;
        int paid = Mathf.Min(SunDebt, gained);
        SunDebt -= paid;
        return gained - paid;
    }
}
