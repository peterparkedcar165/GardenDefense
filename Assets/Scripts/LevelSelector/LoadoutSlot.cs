using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LoadoutSlot : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private CanvasGroup canvasGroup;

    public string PlantName {get; private set;}
    private Action<string> onClick;

    public void Initialize(PlantData data, Action<string> onClickCallback)
    {
        PlantName = data.plantName;
        onClick = onClickCallback;
        if (background != null) background.sprite = data.icon;
        if (nameText != null) nameText.text = data.displayName;
    }

    public void OnClicked()
    {
        onClick?.Invoke(PlantName);
    }

    public void SetDimmed(bool dimmed)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = dimmed ? 0.4f : 1f;
        }
    }
}
