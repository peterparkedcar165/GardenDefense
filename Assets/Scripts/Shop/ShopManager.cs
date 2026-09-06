using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
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

        RefreshPlantSlotItem();

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

    // no longer purchasable - slots unlock automatically via level progression (see
    // SaveData.MaxLoadoutSize). shown read-only so the row isn't left blank in the shop
    private void RefreshPlantSlotItem()
    {
        int slots = Data.MaxLoadoutSize;
        if (plantSlotStatusText != null) plantSlotStatusText.text = $"Plant Slots: {slots} / 8";
        if (plantSlotCostText   != null) plantSlotCostText.text   = slots >= 8 ? "MAX" : "Unlocks via levels";
        if (plantSlotBuyButton  != null) plantSlotBuyButton.interactable = false;
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
