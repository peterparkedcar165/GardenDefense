using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

// one plant card of the loadout screen, in Current Loadout and in Available Plants (Tommy, 2026-11-22;
// the plant card art since 2026-11-25, PlantCardLook): the wood tinted with the plant's element, the trim
// with the plant window and the cost slot over it, the plant's LoadoutSlot in the window and its cost in
// the slot. an open card stays empty in plain wood; a locked one is darkened with a padlock and its hover
// says which level unlocks it. built by LoadoutSelectionUI, at scale times the art's size (every part is
// placed as a share of the card, so the grid's cell decides how big it is drawn)
public class LoadoutSlotSquare : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image backdrop;
    private Image frame;
    private float scale = 1f;
    private string lockedHint;
    private Action<LoadoutSlotSquare, string> onLockedHover;
    private Action onLockedExit;

    public RectTransform Rect => (RectTransform)transform;

    public static LoadoutSlotSquare Create(Transform parent, float scale, Action<LoadoutSlotSquare, string> onLockedHover, Action onLockedExit)
    {
        GameObject go = new GameObject("PlantSlotSquare", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = PlantCardLook.SizeAt(scale);
        LoadoutSlotSquare square = go.AddComponent<LoadoutSlotSquare>();
        square.scale = scale;
        square.onLockedHover = onLockedHover;
        square.onLockedExit = onLockedExit;
        // the wood takes the pointer, so an open or locked card can be hovered too
        square.backdrop = square.AddImage("Backdrop", true);
        square.frame = square.AddImage("Frame", false);
        PlantCardLook.SetSprites(square.backdrop, square.frame);
        return square;
    }

    private Image AddImage(string name, bool raycast)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        PlantCardLook.Fill(rect);
        Image image = go.GetComponent<Image>();
        image.raycastTarget = raycast;
        return image;
    }

    // a card the player can fill: plain wood, nothing in it
    public void ShowOpen()
    {
        lockedHint = null;
        backdrop.color = PlantCardLook.NeutralWood;
        frame.color = Color.white;
        SetCost(null, Color.white);
    }

    public void ShowLocked(string hint)
    {
        lockedHint = hint;
        Color tint = PlantSlotLook.LockedTint;
        backdrop.color = PlantCardLook.NeutralWood * tint;
        frame.color = tint;
        PlantCardLook.PlacePadlock(PlantSlotLook.AddPadlock(transform));
        SetCost(null, Color.white);
    }

    // the plant's LoadoutSlot (its icon and click) goes in the card's window, over the trim; the card has
    // no room for the slot's own name (the tooltip has it)
    public void ShowPlant(LoadoutSlot slot, PlantData data)
    {
        lockedHint = null;
        backdrop.color = PlantCardLook.Wood(data);
        frame.color = Color.white;
        RectTransform rect = (RectTransform)slot.transform;
        rect.SetParent(transform, false);
        PlantCardLook.InWindow(rect);
        rect.SetSiblingIndex(frame.transform.GetSiblingIndex() + 1);
        foreach (TMP_Text own in slot.GetComponentsInChildren<TMP_Text>(true)) own.gameObject.SetActive(false);
    }

    // the cost in the card's slot; null hides it
    public void SetCost(string text, Color color)
    {
        if (text == null)
        {
            Transform found = transform.Find("Cost");
            if (found != null) found.gameObject.SetActive(false);
            return;
        }
        TextMeshProUGUI cost = PlantCardLook.Cost(transform, scale);
        cost.gameObject.SetActive(true);
        cost.text = text;
        cost.color = color;
        cost.transform.SetAsLastSibling();
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
