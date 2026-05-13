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
        Load();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(saveData);
        File.WriteAllText(savePath, json);
        Debug.Log("Saved data");
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
        {
            saveData.unlockedPlants.Add("AcornSprout");
        }
    }
    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.uKey.wasPressedThisFrame)
        {
            string[] all = { "AcornSprout", "Sunflower", "Waterlily", "LeafRanger", "Snowdrop", "PoisonShroom" };
            foreach (string p in all)
                if (!saveData.unlockedPlants.Contains(p))
                    saveData.unlockedPlants.Add(p);
            Save();
            Debug.Log("Unlocked all plants");
        }
    }

    public void CompleteLevel(int level)
    {
        if (level > saveData.highestLevelUnlocked)
        {
            saveData.highestLevelUnlocked = level;
            string unlock = GetPlantUnlockedByLevel(level);
            if (unlock != null && !saveData.unlockedPlants.Contains(unlock))
            {
                saveData.unlockedPlants.Add(unlock);
            }
            Save();
        }
    }

    private string GetPlantUnlockedByLevel(int level)
    {
        switch (level)
        {
            case 1: return "Sunflower";
            case 2: return null; // TBD
            case 3: return "Waterlily";
            case 4: return "LeafRanger";
            default: return null;
        }
    }
}