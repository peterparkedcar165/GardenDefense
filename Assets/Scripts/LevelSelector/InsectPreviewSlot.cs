using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class InsectPreviewSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameLabel;

    private InsectData data;
    private Action<InsectData> onHoverEnter;
    private Action onHoverExit;

    public void Initialize(InsectData insectData, Action<InsectData> hoverEnter, Action hoverExit)
    {
        data = insectData;
        onHoverEnter = hoverEnter;
        onHoverExit = hoverExit;

        if (icon != null)
        {
            icon.sprite = data.sprite;
            icon.enabled = data.sprite != null;
            // insect art faces left; here they face right
            MirrorGraphic.Apply(icon);
        }
        if (nameLabel != null)
            nameLabel.text = data.displayName;
    }

    public void OnPointerEnter(PointerEventData eventData) => onHoverEnter?.Invoke(data);
    public void OnPointerExit(PointerEventData eventData) => onHoverExit?.Invoke();
}
