using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class LoadoutSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private CanvasGroup canvasGroup;

    public string PlantName { get; private set; }
    private PlantData plantData;
    private Action<string> onClick;
    private Action<PlantData> onHover;
    private Action onHoverExit;

    public void Initialize(PlantData data, Action<string> onClickCallback, Action<PlantData> onHoverCallback = null, Action onHoverExitCallback = null)
    {
        PlantName = data.plantName;
        plantData = data;
        onClick = onClickCallback;
        onHover = onHoverCallback;
        onHoverExit = onHoverExitCallback;
        if (background != null) background.sprite = data.icon;
        if (nameText != null) nameText.text = data.displayName;
    }

    public void OnClicked()
    {
        onClick?.Invoke(PlantName);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHover?.Invoke(plantData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }

    public void SetDimmed(bool dimmed)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = dimmed ? 0.4f : 1f;
    }
}
