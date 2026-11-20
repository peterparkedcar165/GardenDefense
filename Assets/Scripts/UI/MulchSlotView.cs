using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// one square of the mulch bar: an inventory slot, or one of the selected plant's mulch slots.
// inventory slots drag (onto another slot to merge or swap, onto a plant or its slots to equip)
// and sell on right click; plant slots only show what's equipped and take drops, since equipped
// mulches can't come off. hovering either shows the mulch's tooltip. built by MulchInventoryUI
public class MulchSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public MulchInventoryUI ui;
    public bool isPlantSlot;
    public int index;
    public Image frame;
    public Image icon;

    private bool hovered;

    // an inventory slot a drop is still flying into reads as empty until it lands
    public MulchItem Item =>
        isPlantSlot ? ui.PlantSlotItem(index)
        : ui.IsIncoming(index) ? null
        : MulchManager.instance != null ? MulchManager.instance.GetSlot(index) : null;

    // draws the slot: a rarity coloured frame and tinted icon, or a dim empty square
    public void Refresh(Sprite placeholder)
    {
        MulchItem item = Item;
        bool has = item != null && item.data != null;
        icon.enabled = has;
        if (has)
        {
            icon.sprite = item.data.icon != null ? item.data.icon : placeholder;
            icon.color = MulchTierColors.Get(item.rarity);
            frame.color = Color.Lerp(MulchTierColors.Get(item.rarity), Color.black, 0.25f);
        }
        else
        {
            frame.color = new Color(0.32f, 0.3f, 0.26f, 0.9f);
        }
        if (hovered) ShowTooltip();
    }

    // a quick bump when a picked up mulch lands in this slot
    public void Pop()
    {
        if (isActiveAndEnabled) StartCoroutine(PopRoutine());
    }

    private System.Collections.IEnumerator PopRoutine()
    {
        const float duration = 0.18f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            float scale = 1f + 0.25f * Mathf.Sin(k * Mathf.PI);
            icon.rectTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        icon.rectTransform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPlantSlot || eventData.button != PointerEventData.InputButton.Right || Item == null) return;
        MulchManager.instance?.Sell(index);
        MulchTooltip.Hide();
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
