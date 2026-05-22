using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public abstract class EntityInfoPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] protected GameObject panel;

    [Header("Header")]
    [SerializeField] protected Image entityIcon;
    [SerializeField] protected TMP_Text entityNameText;

    protected bool IsOpen => panel != null && panel.activeSelf;

    protected virtual void Awake()
    {
        panel.SetActive(false);
    }

    protected virtual void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && IsOpen && !ShouldBlockRightClickClose())
            HidePanel();

        if (IsOpen)
            RefreshStats();
    }

    protected virtual bool ShouldBlockRightClickClose() => false;

    public virtual void HidePanel()
    {
        panel.SetActive(false);
    }

    protected abstract void RefreshStats();
}
