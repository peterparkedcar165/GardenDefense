using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlantSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // the bevel portrait (gd_portrait_bevel_grey, drawn 72 by 72, shown at the button's 100 by 100,
    // Tommy): background is the greyscale backdrop filling the frame's window (64 of its 72), tinted
    // with the element colour mixed 60% toward white; icon is the plant, 10% smaller than the frame;
    // frame is the greyscale bevel on top, tinted with the element colour. both colours come from
    // PlantData (ElementColor, ElementBackdropColor)
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text sunCostText;

    private PlantData data;
    private Plant plant;
    // an empty square's hover text: how a locked slot unlocks, or that no plant was picked for it
    private string emptyHint;
    // the square's own colours, and whether it's darkened for now because the plant can't be afforded
    private Color iconColor = Color.white;
    private Color backgroundColor = Color.white;
    private Color frameColor = Color.white;
    private bool darkened;
    // the cost the price text shows now (ShownMax while it says MAX, ShownNothing before Update ran)
    private const int ShownMax = -1;
    private const int ShownNothing = int.MinValue;
    private int shownCost = ShownNothing;

    public PlantData Data => data;

    public void Setup(PlantData plantData)
    {
        data = plantData;
        plant = plantData.plantPrefab;
        if (icon != null) icon.sprite = plantData.icon;
        // Tommy's plant card (2026-11-25, PlantCardLook): the wood (background) in the element's panel
        // colour, the trim (frame) as drawn, the plant over both in the card's window
        if (background != null) background.color = PlantCardLook.Wood(plantData);
        if (frame != null) frame.color = Color.white;
        if (sunCostText != null) sunCostText.text = FertilizerManager.GetPlacementCost(data).ToString();
        shownCost = ShownNothing;
        if (icon != null) iconColor = icon.color;
        if (background != null) backgroundColor = background.color;
        if (frame != null) frameColor = frame.color;
        darkened = false;
    }

    // too little Sun for the plant: the whole square darkens, the price stays red (Tommy, 2026-11-22).
    // affordable, it's drawn as normal
    private void SetDarkened(bool on)
    {
        if (darkened == on) return;
        darkened = on;
        Color tint = on ? PlantSlotLook.UnaffordableTint : Color.white;
        if (icon != null) icon.color = iconColor * tint;
        if (background != null) background.color = backgroundColor * tint;
        if (frame != null) frame.color = frameColor * tint;
    }

    // an empty square on the bar (Tommy, 2026-11-22): a slot no plant was picked for and a slot not
    // unlocked yet look the same, with a padlock and darkened like a plant that can't be afforded.
    // the hover says which one it is
    public void SetupEmpty(string hint)
    {
        data = null;
        plant = null;
        emptyHint = hint;
        if (icon != null) icon.enabled = false;
        if (sunCostText != null) sunCostText.gameObject.SetActive(false);
        Color tint = PlantSlotLook.UnaffordableTint;
        if (background != null) background.color = PlantCardLook.NeutralWood * tint;
        if (frame != null) frame.color = tint;
        // the button off, so an empty square doesn't light up like a plant does under the pointer
        Button button = GetComponent<Button>();
        if (button != null) button.enabled = false;
        Image padlock = PlantSlotLook.AddPadlock(transform);
        if (padlock != null) padlock.rectTransform.anchoredPosition = PlantCardLook.WindowCentre;
    }

    void Update()
    {
        if (data == null || sunCostText == null || GameManager.instance == null) return;
        if (Plant.AtPlacementLimit(data))
        {
            sunCostText.text = "MAX";
            shownCost = ShownMax;
            sunCostText.fontStyle = FontStyles.Bold;
            sunCostText.color = Color.red;
            SetDarkened(false);
            return;
        }
        int effectiveCost = FertilizerManager.GetPlacementCost(data);
        // the number is only turned into text again when it changes, not every frame
        if (effectiveCost != shownCost)
        {
            shownCost = effectiveCost;
            sunCostText.text = effectiveCost.ToString();
        }
        if (GameManager.instance.SunCount >= effectiveCost)
        {
            sunCostText.fontStyle = FontStyles.Bold;
            sunCostText.color = Color.green;
            SetDarkened(false);
        }
        else
        {
            sunCostText.fontStyle = FontStyles.Normal;
            sunCostText.color = Color.red;
            SetDarkened(true);
        }
    }

    public void OnClicked()
    {
        if (data == null) return;
        if (GameManager.instance == null || GameManager.instance.SunCount < FertilizerManager.GetPlacementCost(data)) return;
        if (Plant.AtPlacementLimit(data)) return;
        if (FertilizerSelectionUI.instance != null && FertilizerSelectionUI.instance.IsOpen) return;
        if (MulchDebugMenu.instance != null && MulchDebugMenu.instance.IsOpen) return;
        if (PlantSelector.instance == null) return;
        PlantSelector.instance.SelectPlant(data.plantPrefab.gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (PlantUpgradeUI.instance == null) return;
        if (plant == null)
        {
            if (emptyHint != null) PlantUpgradeUI.instance.ShowTooltip(emptyHint);
            return;
        }
        string text = $"{plant.GetName()}\n\n{plant.GetDescription()}\n\n<b>Element</b>:\n\n{plant.GetElement()}\n{plant.GetElementDescription()}";
        PlantUpgradeUI.instance.ShowTooltip(text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (plant == null && emptyHint == null) return;
        PlantUpgradeUI.instance?.HideTooltip();
    }
}
