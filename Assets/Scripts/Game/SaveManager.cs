using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    public SaveData saveData = new SaveData();
    private string savePath;
    public int selectedLevel;
    [SerializeField] private PlantRegistry plantRegistry;

    [System.NonSerialized] public List<string> selectedLoadout = new List<string>();
    
    void Awake()
    {
        if (instance != null) // if an instance already exists, destroy it, and return
        {
            Destroy(gameObject);
            Debug.Log("Another save manager found, removed, and replaced with original.");
            return;
        }

        instance = this; // sets the instance of savemanager to this object
        Debug.Log("SaveManager instance initialized");
        DontDestroyOnLoad(gameObject);
        savePath = Application.persistentDataPath + "/save.json";
        saveData = new SaveData { highestLevelUnlocked = 0 };
        Load();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            string json = JsonUtility.ToJson(saveData);
            File.WriteAllText(savePath, json);
            Debug.Log("Saved to: " + savePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
        }
    }

    public void Load()
    {
        Debug.Log("Started");
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            saveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Loaded save data");
        }

        // on load, fill any plant gaps caused by levels already completed
        RepairPlantsFromLevels();
    }

    // derives plants strictly from highestLevelUnlocked, levels are the single source of truth
    private void RepairPlantsFromLevels()
    {
        // index 0 is always unlocked regardless of level
        string defaultPlant = GetPlantUnlockedByLevel(0);
        if (defaultPlant != null && !saveData.unlockedPlants.Contains(defaultPlant))
            saveData.unlockedPlants.Add(defaultPlant);

        for (int i = 1; i <= saveData.highestLevelUnlocked; i++)
        {
            string plant = GetPlantUnlockedByLevel(i);
            if (plant != null && !saveData.unlockedPlants.Contains(plant))
                saveData.unlockedPlants.Add(plant);
        }
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.uKey.wasPressedThisFrame)
        {
            saveData.highestLevelUnlocked = plantRegistry != null ? plantRegistry.plants.Length - 1 : 40;
            saveData.unlockedPlants.Clear();
            saveData.unlockedPlants.Add("AcornSprout");
            RepairPlantsFromLevels();
            Save();
            LoadoutSelectionUI.instance?.RefreshUI();
            Debug.Log("Unlocked all levels and plants");
        }
        if (UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame)
        {
            saveData.highestLevelUnlocked = 0;
            saveData.unlockedPlants.Clear();
            saveData.unlockedPlants.Add("AcornSprout");
            Save();
            LoadoutSelectionUI.instance?.RefreshUI();
            Debug.Log("Reset to level 1 only");
        }
        if (UnityEngine.InputSystem.Keyboard.current.minusKey.wasPressedThisFrame)
        {
            if (saveData.highestLevelUnlocked > 0)
            {
                string plant = GetPlantUnlockedByLevel(saveData.highestLevelUnlocked);
                if (plant != null) saveData.unlockedPlants.Remove(plant);
                saveData.highestLevelUnlocked--;
            }
            Save();
            LoadoutSelectionUI.instance?.RefreshUI();
            Debug.Log($"highestLevelUnlocked decreased to {saveData.highestLevelUnlocked}");
        }
        if (UnityEngine.InputSystem.Keyboard.current.equalsKey.wasPressedThisFrame)
        {
            int maxLevel = plantRegistry != null ? plantRegistry.plants.Length - 1 : 40;
            saveData.highestLevelUnlocked = Mathf.Min(maxLevel, saveData.highestLevelUnlocked + 1);
            RepairPlantsFromLevels();
            Save();
            LoadoutSelectionUI.instance?.RefreshUI();
            Debug.Log($"highestLevelUnlocked increased to {saveData.highestLevelUnlocked}");
        }
        if (UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame)
        {
            saveData.flowerPotLevel = 0;
            saveData.waterPotLevel  = 0;
            Save();
            Debug.Log("Reset shop upgrades");
        }
        if (UnityEngine.InputSystem.Keyboard.current.leftBracketKey.wasPressedThisFrame)
        {
            saveData.currency += 99999;
            Save();
            Debug.Log("Added 99999 currency");
        }
        if (UnityEngine.InputSystem.Keyboard.current.rightBracketKey.wasPressedThisFrame)
        {
            saveData.currency = 0;
            Save();
            Debug.Log("Removed all currency");
        }
        if (UnityEngine.InputSystem.Keyboard.current.semicolonKey.wasPressedThisFrame)
        {
            // no single "current plant" to target from here, so this dev shortcut just tops up
            // every unlocked plant at once
            foreach (string plantName in saveData.unlockedPlants)
                saveData.AddPlantSkillPoints(plantName, 20);
            Save();
            SkillTreeUI.instance?.RefreshAll();
            Debug.Log("Added 20 skill points to every unlocked plant");
        }
        if (UnityEngine.InputSystem.Keyboard.current.quoteKey.wasPressedThisFrame)
        {
            ResetAllSkillTrees();
            Debug.Log("Reset all skill trees and refunded skill points");
        }
    }

    // refunds one plant's spent skill points (computed from its tree's own costPerRank * rank
    // per purchased node, since points are per-species, not a global pool) and wipes its
    // purchased nodes
    private void ResetSkillTree(string plantName, SkillTreeData tree)
    {
        if (tree == null) return;
        int refund = 0;
        foreach (SkillTreeStep step in tree.steps)
            foreach (SkillTreeNode node in step.nodes)
            {
                int rank = SkillTreeManager.GetRank(plantName, node.id);
                if (rank > 0) refund += node.costPerRank * rank;
            }
        saveData.AddPlantSkillPoints(plantName, refund);
        saveData.skillPurchases.RemoveAll(p => p.plantName == plantName);
    }

    // resets every plant that has any purchased nodes - wired to the quote-key debug shortcut
    // for now, later becomes a UI reset button
    public void ResetAllSkillTrees()
    {
        if (plantRegistry == null) return;
        HashSet<string> plantNames = new HashSet<string>();
        foreach (SkillNodePurchase p in saveData.skillPurchases) plantNames.Add(p.plantName);

        foreach (string plantName in plantNames)
        {
            PlantData data = System.Array.Find(plantRegistry.plants, d => d != null && d.plantName == plantName);
            if (data != null) ResetSkillTree(plantName, data.skillTree);
        }
        Save();
        SkillTreeUI.instance?.RefreshAll();
    }

    // checks every plant with any banked exp for level-ups earned since the last check, granting
    // 1 skill point per level gained (up to PlantLevelCurve.MaxLevel) - called once per level
    // completion, since leveling is deliberately a post-game event, not a live one.
    // totalExp is progress within the CURRENT level, not a lifetime total - each level-up
    // subtracts that level's threshold rather than resetting to 0, so overflow exp carries into
    // the next level and a single big completion can chain multiple level-ups
    private void ProcessPlantLevelUps()
    {
        foreach (PlantExpRecord record in saveData.plantExp)
        {
            while (record.level < PlantLevelCurve.MaxLevel &&
                   record.totalExp >= PlantLevelCurve.ExpForNextLevel(record.level))
            {
                record.totalExp -= PlantLevelCurve.ExpForNextLevel(record.level);
                record.level++;
                saveData.AddPlantSkillPoints(record.plantName, 1);
            }
        }
    }

    public void CompleteLevel(int level)
    {
        bool firstClear = level > saveData.highestLevelUnlocked;
        saveData.highestLevelUnlocked = Mathf.Max(saveData.highestLevelUnlocked, level);
        string plant = GetPlantUnlockedByLevel(level);
        if (plant != null && !saveData.unlockedPlants.Contains(plant))
            saveData.unlockedPlants.Add(plant);
        saveData.currency += 200 + level * 40;
        // plant exp was already banked and committed to saveData by Plant.BankAllExpForLevelEnd()
        // and Plant.CommitPendingExpToSave() just before this call (see ProceduralLevel) - a game
        // over never reaches this method, so exp only ever becomes persistent on an actual win.
        // level-ups (and the skill points they grant) are processed here since leveling only ever
        // happens on a completed level, never mid-level
        ProcessPlantLevelUps();
        Save();
        Debug.Log($"Level {level} completed. Unlocked: {plant ?? "none"}. highestLevelUnlocked={saveData.highestLevelUnlocked}. firstClear={firstClear}");
    }

    private string GetPlantUnlockedByLevel(int level)
    {
        if (plantRegistry == null) return null;
        if (level < 0 || level >= plantRegistry.plants.Length) return null;
        // empty registry slots mark levels that unlock no plant, such as the level before a boss
        if (plantRegistry.plants[level] == null) return null;
        return plantRegistry.plants[level].plantName;
    }
}