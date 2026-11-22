using UnityEngine;
using UnityEngine.UI;

// the look the plant bar's buttons and the loadout screen's squares share: the bevel frame on its
// backdrop, and for a slot that isn't unlocked yet the same square darkened with a padlock on it
// (Tommy, 2026-11-22). the sprites and colours are on PlantInfoPanelStyle
public static class PlantSlotLook
{
    // padlock_x3 at 2 canvas units an art px, the size the plant panel's skill square padlock has
    public const float PadlockWidth = 34f;
    public const float PadlockHeight = 48f;

    public static Color LockedTint =>
        PlantInfoPanelStyle.Instance != null ? PlantInfoPanelStyle.Instance.lockedSlotTint : new Color(0.35f, 0.35f, 0.35f, 1f);

    public static Color UnaffordableTint =>
        PlantInfoPanelStyle.Instance != null ? PlantInfoPanelStyle.Instance.unaffordableSlotTint : new Color(0.45f, 0.45f, 0.45f, 1f);

    // a padlock in the middle of the slot, drawn over everything else in it
    public static Image AddPadlock(Transform slot)
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        if (slot == null || style == null || style.slotPadlock == null) return null;
        GameObject go = new GameObject("Padlock", typeof(RectTransform), typeof(Image));
        go.layer = slot.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(slot, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(PadlockWidth, PadlockHeight);
        rect.SetAsLastSibling();
        Image image = go.GetComponent<Image>();
        image.sprite = style.slotPadlock;
        image.color = style.lockedText;
        image.raycastTarget = false;
        return image;
    }
}
