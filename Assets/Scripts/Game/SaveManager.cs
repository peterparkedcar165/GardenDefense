using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    public SaveData saveData = new SaveData();
    private string savePath;
    public int selectedLevel;
    
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
    }
}
