using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    public SaveData saveData = new SaveData();
    private string savePath;
    public int selectedLevel;

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

        if (saveData.unlockedPlants.Count == 0)
            saveData.unlockedPlants.Add("AcornSprout");

        // on load, fill any plant gaps caused by levels already completed
        RepairPlantsFromLevels();
    }

    // derives plants strictly from highestLevelUnlocked , levels are the single source of truth
    private void RepairPlantsFromLevels()
    {
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
            saveData.highestLevelUnlocked = 40;
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
    }

    public void CompleteLevel(int level)
    {
        saveData.highestLevelUnlocked = Mathf.Max(saveData.highestLevelUnlocked, level);
        string plant = GetPlantUnlockedByLevel(level);
        if (plant != null && !saveData.unlockedPlants.Contains(plant))
            saveData.unlockedPlants.Add(plant);
        saveData.currency += 200 + level * 40;
        Save();
        Debug.Log($"Level {level} completed. Unlocked: {plant ?? "none"}. highestLevelUnlocked={saveData.highestLevelUnlocked}");
    }

    private string GetPlantUnlockedByLevel(int level)
    {
        switch (level)
        {
            case 1: return "Sunflower";
            case 2: return "Waterlily"; // TBD
            case 3: return "LeafRanger";
            case 4: return "Dandelion";
            case 5: return "Calendula";
            case 6: return "BogIris";
            case 7: return "PoisonShroom";
            case 8: return "Holly";
            case 9: return "Begonia";
            case 10: return "AloeVera";
            case 11: return "Cactus";
            case 12: return "Aeonium";
            case 13: return "Snowdrop";
            case 14: return "NeriumOleander";
            case 15: return "Glowshroom";
            case 16: return "MorningGlory";
            case 17: return "Stargazer";
            case 18: return "GhostFungus";
            case 19: return "Cattail";
            case 20: return "Gloriosa";
            case 21: return "Hellebore";
            case 22: return "Rhodiola";
            default: return null;
        }
    }
}