using UnityEngine;
using System.Collections.Generic;

// the level's fertilizer: one is picked before the level from LevelConfig.fertilizers (see
// FertilizerSelectionUI) and stays active until the level ends. session only, reset at every level
// start (ProceduralLevel.Start). the level wide values below are what fertilizer effects nudge;
// the game code they change reads them directly, and ResetForLevel puts them back to neutral
public class FertilizerManager : MonoBehaviour
{
    public static FertilizerManager instance;

    // greedy growth raises it, garden fence lowers it: what a leaked insect's damage is multiplied by
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
    // seed loan: sun still owed after the loan came due. every sun gained pays this off first
    // (GameManager.AddSun)
    public static int SunDebt = 0;
    // clean garden: every this many waves, the guaranteed end of wave mulch is Epic (0 = off).
    // read by MulchManager
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

    // the fertilizers of a level's list the current loadout can use, in list order
    public static List<FertilizerData> GetOffered(FertilizerData[] list)
    {
        List<PlantData> loadout = GetCurrentLoadout();
        List<FertilizerData> result = new List<FertilizerData>();
        if (list == null) return result;
        foreach (FertilizerData data in list)
            if (data != null && !result.Contains(data) && data.IsOffered(loadout)) result.Add(data);
        return result;
    }

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

    // seed loan: pays the debt off out of sun just gained, returns what's left to bank
    public static int PayDebtFrom(int gained)
    {
        if (SunDebt <= 0 || gained <= 0) return gained;
        int paid = Mathf.Min(SunDebt, gained);
        SunDebt -= paid;
        return gained - paid;
    }
}
