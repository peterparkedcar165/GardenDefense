using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// one square of the mulch bar: an inventory slot, or one of the selected plant's mulch slots.
// inventory slots drag (onto another slot to merge or swap, onto a plant or its slots to equip)
// and open a Sell popup on right click; plant slots only show what's equipped and take drops, since equipped
// mulches can't come off. hovering either shows the mulch's tooltip. built by MulchInventoryUI
public class MulchSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public MulchInventoryUI ui;
    public bool isPlantSlot;
    public int index;
    public Image frame;
    public Image icon;
    [Tooltip("the frame is the info panel's slot sprite: drawn as is while empty, tinted toward the rarity when filled")]
    public bool spriteFrame;
    [Tooltip("Tommy's vine frame (2026-11-26): always its own colours, never tinted by the rarity")]
    public bool keepFrameColour;

    private bool hovered;
    // what the slot last showed, and whose slot it was (the plant, for a plant slot), so a mulch
    // that newly lands here pops in, but switching to another plant's slots doesn't
    private MulchItem shownItem;
    private Object shownOwner;
    private Coroutine popRoutine;

    // the pop when a mulch appears in a slot (Tommy, 2026-11-20): from nothing up past full size,
    // then settling back to it. real time, so it also plays while paused
    private const float PopSeconds = 0.32f;
    private const float PopPeak = 1.35f;
    // the share of the time spent growing to the peak; the rest settles back
    private const float PopRise = 0.55f;

    // an inventory slot a drop is still flying into reads as empty until it lands
    public MulchItem Item =>
        isPlantSlot ? ui.PlantSlotItem(index)
        : ui.IsIncoming(index) ? null
        : MulchManager.instance != null ? MulchManager.instance.GetSlot(index) : null;

    // draws the slot: a rarity coloured frame and the mulch's paper bag icon (MulchIcon), or a dim
    // empty square. a mulch new to this slot (picked up, moved, merged, equipped) pops in
    public void Refresh()
    {
        MulchItem item = Item;
        Object owner = isPlantSlot ? ui.ShownPlant : null;
        bool appeared = item != null && item != shownItem && owner == shownOwner;
        shownItem = item;
        shownOwner = owner;
        bool has = item != null && item.data != null;
        if (has) MulchIcon.Show(icon, item);
        else MulchIcon.Hide(icon);
        if (keepFrameColour)
            frame.color = Color.white;
        else if (has)
        {
            frame.color = spriteFrame
                ? Color.Lerp(Color.white, MulchTierColors.Get(item.rarity), 0.5f)
                : Color.Lerp(MulchTierColors.Get(item.rarity), Color.black, 0.25f);
        }
        else
        {
            frame.color = spriteFrame ? Color.white : new Color(0.32f, 0.3f, 0.26f, 0.9f);
        }
        if (hovered) ShowTooltip();
        if (appeared && has) Pop();
    }

    public void Pop()
    {
        if (!isActiveAndEnabled) return;
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopRoutine());
    }

    private System.Collections.IEnumerator PopRoutine()
    {
        RectTransform rect = icon.rectTransform;
        float elapsed = 0f;
        while (elapsed < PopSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / PopSeconds);
            float scale;
            if (k < PopRise)
            {
                // fast at first, slowing into the peak
                float u = k / PopRise;
                scale = PopPeak * (1f - (1f - u) * (1f - u));
            }
            else
            {
                // eases back down to full size
                float u = (k - PopRise) / (1f - PopRise);
                scale = Mathf.Lerp(PopPeak, 1f, u * u * (3f - 2f * u));
            }
            rect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;
        popRoutine = null;
    }

    // a pop cut short (the panel closed) must not leave the icon small
    private void OnDisable()
    {
        if (hovered && !isPlantSlot && ui != null) ui.SetHoveredSlot(index, false);
        hovered = false;
        popRoutine = null;
        if (icon != null) icon.rectTransform.localScale = Vector3.one;
    }

    // a hovered bag slot also lights up the plants its mulch would merge with (MulchInventoryUI)
    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (!isPlantSlot) ui.SetHoveredSlot(index, true);
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        if (!isPlantSlot) ui.SetHoveredSlot(index, false);
        MulchTooltip.Hide();
    }

    private void ShowTooltip()
    {
        MulchItem item = Item;
        if (item == null) { MulchTooltip.Hide(); return; }
        if (isPlantSlot)
        {
            MulchTooltip.Show(item, ui.PlantSlotEffect(index), null);
            return;
        }
        int price = MulchManager.instance != null ? MulchManager.instance.SellPrice(item) : 0;
        MulchTooltip.Show(item, null, $"Drag onto a plant to give it this mulch. Right click to sell for {price} Sun.");
    }

    // right click opens the Sell popup on this mulch (or closes it); the popup's button sells
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPlantSlot || eventData.button != PointerEventData.InputButton.Right) return;
        MulchTooltip.Hide();
        ui.ToggleSellPopup(index);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlantSlot || eventData.button != PointerEventData.InputButton.Left || Item == null) return;
        MulchTooltip.Hide();
        ui.BeginDrag(index, eventData.position);
    }

    public void OnDrag(PointerEventData eventData) => ui.MoveDrag(eventData.position);

    public void OnEndDrag(PointerEventData eventData) => ui.EndDrag(eventData.position);

    public void OnDrop(PointerEventData eventData) => ui.DropOnSlot(this);
}
