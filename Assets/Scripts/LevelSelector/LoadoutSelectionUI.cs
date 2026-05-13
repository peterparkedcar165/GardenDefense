using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LoadoutSelectionUI : MonoBehaviour
{
    public static LoadoutSelectionUI instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private PlantData[] allPlantData;
    [SerializeField] private Transform unlockedContainer;
    [SerializeField] private Transform selectedContainer;
    [SerializeField] private LoadoutSlot slotPrefab;
    [SerializeField] private Button confirmButton;

    private int pendingLevel;
    private List<string> selectedLoadout = new List<string>();
    private List<LoadoutSlot> unlockedSlots = new List<LoadoutSlot>();
    private List<LoadoutSlot> selectedSlots = new List<LoadoutSlot>();

    public bool IsOpen => panel.activeSelf;

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    void Update()
    {
        if (panel.activeSelf && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            panel.SetActive(false);
            selectedLoadout.Clear();
        }
    }

    public void Show(int level)
    {
        pendingLevel = level;
        List<string> unlocked = SaveManager.instance.saveData.unlockedPlants;

        // if less than 4 unlocked, skip the screen
        if (unlocked.Count < 1)
        {
            SaveManager.instance.selectedLoadout = new List<string>(unlocked);
            LoadLevel();
            return;
        }

        panel.SetActive(true);
        selectedLoadout.Clear();
        RefreshUI();
        
    }

    private void RefreshUI()
    {
        Debug.Log("RefreshUI called. Unlocked plants: " + SaveManager.instance.saveData.unlockedPlants.Count);
        foreach (string p in SaveManager.instance.saveData.unlockedPlants)
            Debug.Log("Plant: " + p);

        foreach (var s in unlockedSlots){
            Destroy(s.gameObject);
        }
        unlockedSlots.Clear();

        foreach (string plantName in SaveManager.instance.saveData.unlockedPlants)
        {
            PlantData data = GetPlantData(plantName);

            if (data == null) continue;

            LoadoutSlot slot = Instantiate(slotPrefab, unlockedContainer);
            slot.Initialize(data, OnUnlockedSlotClicked);
            slot.SetDimmed(selectedLoadout.Contains(plantName));
            unlockedSlots.Add(slot);
        }

        foreach (var s in selectedSlots) Destroy(s.gameObject);
        selectedSlots.Clear();

        foreach (string plantName in selectedLoadout)
        {
            PlantData data = GetPlantData(plantName);
            if (data == null) continue;
            LoadoutSlot slot = Instantiate(slotPrefab, selectedContainer);
            slot.Initialize(data, OnSelectedSlotClicked);
            selectedSlots.Add(slot);
        }

        confirmButton.interactable = selectedLoadout.Count > 0;
    }

    private void OnUnlockedSlotClicked(string plantName)
    {
        if (selectedLoadout.Contains(plantName) || selectedLoadout.Count >= 4) return;
        selectedLoadout.Add(plantName);
        RefreshUI();
    }

    private void OnSelectedSlotClicked(string plantName)
    {
        selectedLoadout.Remove(plantName);
        RefreshUI();
    }

    public void Confirm()
    {
        SaveManager.instance.selectedLoadout = new List<string>(selectedLoadout);
        panel.SetActive(false);
        LoadLevel();
    }

    private void LoadLevel()
    {
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Level"+pendingLevel));
    }

    private PlantData GetPlantData(string plantName)
    {
        foreach (var data in allPlantData)
        {
            Debug.Log($"Comparing '{plantName}' to '{data.plantName}'");
            if (data.plantName == plantName) return data;
        }

        return null;
    }
}
