using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

// one square of the loadout screen's Current Loadout grid (Tommy, 2026-11-22): the plant bar's bevel
// frame on its backdrop. a filled square holds the picked plant's LoadoutSlot, tinted with its element
// like the plant bar; an open one stays blank until a plant fills it; a locked one is darkened with a
// padlock and its hover says which level unlocks it. built by LoadoutSelectionUI
public class LoadoutSlotSquare : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // the plant bar button's proportions: frame 100, the backdrop filling the frame's window (64 of
    // its 72 art px), the plant 10% smaller than the frame
    private const float FrameSize = 100f;
    private const float BackdropSize = FrameSize * 64f / 72f;
    private const float IconSize = FrameSize * 0.9f;

    private Image backdrop;
    private Image frame;
    private string lockedHint;
    private Action<LoadoutSlotSquare, string> onLockedHover;
    private Action onLockedExit;

    public RectTransform Rect => (RectTransform)transform;

    public static LoadoutSlotSquare Create(Transform parent, Action<LoadoutSlotSquare, string> onLockedHover, Action onLockedExit)
    {
        GameObject go = new GameObject("PlantSlotSquare", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        LoadoutSlotSquare square = go.AddComponent<LoadoutSlotSquare>();
        square.onLockedHover = onLockedHover;
        square.onLockedExit = onLockedExit;
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        // the backdrop takes the pointer, so an open or locked square can be hovered too
        square.backdrop = square.AddImage("Backdrop", BackdropSize, style != null ? style.slotBackdrop : null, true);
        square.frame = square.AddImage("Frame", FrameSize, style != null ? style.slotFrame : null, false);
        return square;
    }

    private Image AddImage(string name, float size, Sprite sprite, bool raycast)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = raycast;
        return image;
    }

    // a slot the player can fill: the frame and backdrop untinted, nothing in it
    public void ShowOpen()
    {
        lockedHint = null;
        backdrop.color = Color.white;
        frame.color = Color.white;
    }

    public void ShowLocked(string hint)
    {
        lockedHint = hint;
        Color tint = PlantSlotLook.LockedTint;
        backdrop.color = tint;
        frame.color = tint;
        PlantSlotLook.AddPadlock(transform);
    }

    // puts the plant's LoadoutSlot (its icon, name and click) between the backdrop and the frame,
    // where the plant bar draws its icon
    public void ShowPlant(LoadoutSlot slot, PlantData data)
    {
        lockedHint = null;
        backdrop.color = PlantData.ElementBackdropColor(data.elementalType);
        frame.color = PlantData.ElementColor(data.elementalType);
        RectTransform rect = (RectTransform)slot.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(IconSize, IconSize);
        rect.SetSiblingIndex(frame.transform.GetSiblingIndex());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (lockedHint != null) onLockedHover?.Invoke(this, lockedHint);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (lockedHint != null) onLockedExit?.Invoke();
    }

    private void OnDisable()
    {
        if (lockedHint != null) onLockedExit?.Invoke();
    }
}
