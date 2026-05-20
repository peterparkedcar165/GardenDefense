using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class PlantEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;

    private PlantData data;
    private Action<PlantData> onHover;
    private Action onHoverExit;
    private Action<PlantData> onClick;

    public void Initialize(PlantData plantData, Action<PlantData> onHoverCallback, Action onHoverExitCallback, Action<PlantData> onClickCallback)
    {
        data = plantData;
        onHover = onHoverCallback;
        onHoverExit = onHoverExitCallback;
        onClick = onClickCallback;
        if (nameText) nameText.text = plantData.displayName;
        if (icon && plantData.icon) icon.sprite = plantData.icon;
    }

    public void OnClicked()
    {
        onClick?.Invoke(data);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHover?.Invoke(data);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }
}
