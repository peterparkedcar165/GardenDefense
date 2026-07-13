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
        if (UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame)
        {
            saveData.flowerPotLevel = 0;
            saveData.waterPotLevel  = 0;
            saveData.plantSlotLevel = 0;
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
            saveData.skillPoints += 20;
            Save();
            SkillTreeUI.instance?.RefreshAll();
            Debug.Log("Added 20 skill points");
        }
        if (UnityEngine.InputSystem.Keyboard.current.quoteKey.wasPressedThisFrame)
        {
            saveData.skillPoints = 0;
            saveData.skillPurchases.Clear();
            Save();
            SkillTreeUI.instance?.RefreshAll();
            Debug.Log("Reset skill points and purchases");
        }
    }

    public void CompleteLevel(int level)
    {
        // first clear grants bonus skill points
        bool firstClear = level > saveData.highestLevelUnlocked;
        saveData.highestLevelUnlocked = Mathf.Max(saveData.highestLevelUnlocked, level);
        string plant = GetPlantUnlockedByLevel(level);
        if (plant != null && !saveData.unlockedPlants.Contains(plant))
            saveData.unlockedPlants.Add(plant);
        saveData.currency += 200 + level * 40;
        saveData.skillPoints += firstClear ? 3 : 1;
        Save();
        Debug.Log($"Level {level} completed. Unlocked: {plant ?? "none"}. highestLevelUnlocked={saveData.highestLevelUnlocked}");
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