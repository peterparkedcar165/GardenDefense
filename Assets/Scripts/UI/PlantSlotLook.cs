using UnityEngine;
using UnityEngine.UI;

// the look the plant bar's buttons and the loadout screen's cards share for a slot that isn't unlocked
// yet: the card darkened with a padlock on it (Tommy, 2026-11-22). the sprites and colours are on
// PlantInfoPanelStyle
public static class PlantSlotLook
{
    // the padlock at 2 canvas units an art px, as big as on the plant panel's locked skill square and the
    // skill tree's locked nodes (Tommy's padlock, 2026-11-25: 44 by 52)
    public const float PadlockPx = 2f;

    public static Color LockedTint =>
        PlantInfoPanelStyle.Instance != null ? PlantInfoPanelStyle.Instance.lockedSlotTint : new Color(0.35f, 0.35f, 0.35f, 1f);

    public static Color UnaffordableTint =>
        PlantInfoPanelStyle.Instance != null ? PlantInfoPanelStyle.Instance.unaffordableSlotTint : new Color(0.45f, 0.45f, 0.45f, 1f);

    // a padlock in the middle of the slot, drawn over everything else in it, in its own colours (a card
    // moves it to its window's centre: PlantCardLook.PlacePadlock)
    public static Image AddPadlock(Transform slot)
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        Sprite sprite = style != null ? style.Padlock : null;
        if (slot == null || sprite == null) return null;
        GameObject go = new GameObject("Padlock", typeof(RectTransform), typeof(Image));
        go.layer = slot.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(slot, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sprite.rect.size * PadlockPx;
        rect.SetAsLastSibling();
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        // nothing brightens it here, so it's shown as drawn
        image.color = style.PadlockColor(true);
        image.raycastTarget = false;
        return image;
    }
}
