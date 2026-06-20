using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopManager : MonoBehaviour
{
    // plant slot: 4 purchases to go from 4 → 8 slots
    private static readonly int[] PlantSlotCosts    = { 1500, 3500, 7500, 15000 };

    // flower pot: unlock + 3 upgrades (sun cost drops 25 → 20 → 15 → 10)
    private static readonly int[] FlowerPotCosts    = { 750, 1500, 3000, 6000 };
    private static readonly int[] FlowerPotSunCosts = { 25, 20, 15, 10 };

    // water pot: same curve
    private static readonly int[] WaterPotCosts     = { 750, 1500, 3000, 6000 };
    private static readonly int[] WaterPotSunCosts  = { 25, 20, 15, 10 };

    [Header("Currency")]
    [SerializeField] private TMP_Text currencyText;

    [Header("Plant Slot")]
    [SerializeField] private TMP_Text plantSlotStatusText;
    [SerializeField] private TMP_Text plantSlotCostText;
    [SerializeField] private Button   plantSlotBuyButton;

    [Header("Flower Pot")]
    [SerializeField] private FlowerPot flowerPotPrefab;
    [SerializeField] private Image     flowerPotIconImage;
    [SerializeField] private TMP_Text  flowerPotStatusText;
    [SerializeField] private TMP_Text  flowerPotCostText;
    [SerializeField] private Button    flowerPotBuyButton;

    [Header("Water Pot")]
    [SerializeField] private WaterPot waterPotPrefab;
    [SerializeField] private Image    waterPotIconImage;
    [SerializeField] private TMP_Text waterPotStatusText;
    [SerializeField] private TMP_Text waterPotCostText;
    [SerializeField] private Button   waterPotBuyButton;

    private SaveData Data => SaveManager.instance.saveData;

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            OnBack();
        RefreshAll();
    }

    private void Start()
    {
        flowerPotIconImage.sprite = SpriteFromVisual(flowerPotPrefab);
        waterPotIconImage.sprite  = SpriteFromVisual(waterPotPrefab);
        RefreshAll();
    }

    private static Sprite SpriteFromVisual(Component prefab)
    {
        if (prefab == null) return null;
        Transform visual = prefab.transform.Find("Visual");
        if (visual == null) return null;
        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }

    public void BuyPlantSlot()
    {
        int level = Data.plantSlotLevel;
        if (level >= PlantSlotCosts.Length) return;
        if (!TrySpend(PlantSlotCosts[level])) return;
        Data.plantSlotLevel++;
        SaveManager.instance.Save();
        RefreshAll();
    }

    public void BuyFlowerPot()
    {
        int level = Data.flowerPotLevel;
        if (level >= FlowerPotCosts.Length) return;
        if (!TrySpend(FlowerPotCosts[level])) return;
        Data.flowerPotLevel++;
        SaveManager.instance.Save();
        RefreshAll();
    }

    public void BuyWaterPot()
    {
        int level = Data.waterPotLevel;
        if (level >= WaterPotCosts.Length) return;
        if (!TrySpend(WaterPotCosts[level])) return;
        Data.waterPotLevel++;
        SaveManager.instance.Save();
        RefreshAll();
    }

    public void OnBack()
    {
        if (SceneTransition.IsTransitioning) return;
        SceneTransition t = FindAnyObjectByType<SceneTransition>();
        if (t != null) t.StartCoroutine(t.FadeToScene("MainMenu"));
    }

    private bool TrySpend(int cost)
    {
        if (Data.currency < cost) return false;
        Data.currency -= cost;
        return true;
    }

    private void RefreshAll()
    {
        if (currencyText != null)
            currencyText.text = $"${Data.currency}";

        RefreshItem(
            plantSlotStatusText, plantSlotCostText, plantSlotBuyButton,
            Data.plantSlotLevel, PlantSlotCosts,
            l => $"Plant Slots: {4 + l} / 8",
            l => $"→ {4 + l + 1} slots"
        );

        RefreshPotItem(
            flowerPotStatusText, flowerPotCostText, flowerPotBuyButton,
            Data.flowerPotLevel, FlowerPotCosts, FlowerPotSunCosts,
            "Flower Pot"
        );

        RefreshPotItem(
            waterPotStatusText, waterPotCostText, waterPotBuyButton,
            Data.waterPotLevel, WaterPotCosts, WaterPotSunCosts,
            "Water Pot"
        );
    }

    private void RefreshItem(TMP_Text statusText, TMP_Text costText, Button buyButton,
        int level, int[] costs, Func<int, string> statusLabel, Func<int, string> upgradeLabel)
    {
        bool maxed = level >= costs.Length;
        if (statusText != null) statusText.text = statusLabel(level);
        if (costText   != null) costText.text   = maxed ? "MAX" : $"${costs[level]}  ({upgradeLabel(level)})";
        if (buyButton  != null) buyButton.interactable = !maxed && Data.currency >= costs[level];
    }

    private void RefreshPotItem(TMP_Text statusText, TMP_Text costText, Button buyButton,
        int level, int[] costs, int[] sunCosts, string label)
    {
        bool maxed = level >= costs.Length;
        string status = level == 0
            ? $"{label}: Locked"
            : $"{label}: {sunCosts[level - 1]} Sun";
        string upgrade = level == 0
            ? "Unlock"
            : (maxed ? "MAX" : $"→ {sunCosts[level]} Sun");

        if (statusText != null) statusText.text = status;
        if (costText   != null) costText.text   = maxed ? "MAX" : $"${costs[level]}  ({upgrade})";
        if (buyButton  != null) buyButton.interactable = !maxed && Data.currency >= costs[level];
    }
}
