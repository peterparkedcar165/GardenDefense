using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

// the mulch bar, always on screen during a level: the player's inventory (MulchRegistry.inventorySize
// slots), and next to it, while a plant is selected, that plant's mulch slots (plantSlots). drag an
// inventory mulch onto another slot to merge or swap, onto a plant on the map or onto the plant's
// slots to equip it; right click sells. built in code under the game's own canvas (PersistentCanvas),
// low in its draw order so the plant panel, pick screens and tutorials stay on top. where it sits
// (barAnchor, barOffset) and its slot size are MulchRegistry fields. ground drops never land under it
// (MulchDrop checks the UI under each spot)
public class MulchInventoryUI : MonoBehaviour
{
    public static MulchInventoryUI instance;

    private const float Padding = 6f;
    private const float PartGap = 16f;

    private MulchRegistry registry;
    private TMP_FontAsset font;
    private Canvas canvas;
    private RectTransform canvasRect;

    private RectTransform inventoryPart, plantPart;
    private TextMeshProUGUI plantLabel;
    private readonly List<MulchSlotView> inventorySlots = new List<MulchSlotView>();
    private readonly List<MulchSlotView> plantSlots = new List<MulchSlotView>();
    private Plant shownPlant;
    private int shownPlantMulchCount = -1;

    // tooltip and drag ghost live on their own layer, the canvas's last child, above everything
    private RectTransform overlay;
    private RectTransform tooltipRect;
    private TextMeshProUGUI tooltipText;
    private Image dragGhost;
    private int draggingSlot = -1;
    private bool dropHandled;

    // inventory slots a picked up drop is still flying into: already filled, shown empty until it lands
    private readonly HashSet<int> incoming = new HashSet<int>();
    public bool IsIncoming(int slot) => incoming.Contains(slot);

    // called at every level start (MulchManager.ResetForLevel): built once, kept with the canvas
    public static void Ensure()
    {
        if (instance != null) { instance.RefreshAll(); return; }
        if (PersistentCanvas.instance == null) return;
        GameObject go = new GameObject("MulchInventoryUI", typeof(RectTransform));
        go.transform.SetParent(PersistentCanvas.instance.transform, false);
        // right after the tutorial system, so every other part of the HUD draws over it
        go.transform.SetSiblingIndex(Mathf.Min(1, go.transform.parent.childCount - 1));
        instance = go.AddComponent<MulchInventoryUI>();
        instance.Build();
    }

    private void OnEnable() => MulchManager.OnChanged += RefreshAll;
    private void OnDisable() => MulchManager.OnChanged -= RefreshAll;

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (overlay != null) Destroy(overlay.gameObject);
    }

    private void Update()
    {
        Plant selected = PlantUpgradeUI.instance != null ? PlantUpgradeUI.instance.GetSelectedPlant() : null;
        if (selected != null && !selected.IsAlive) selected = null;
        if (selected != shownPlant || (selected != null && selected.mulches.Count != shownPlantMulchCount))
            RefreshPlantPart(selected);
    }

    // ── building ────────────────────────────────────────────────────────────

    private void Build()
    {
        registry = MulchManager.instance != null ? MulchManager.instance.Registry : Resources.Load<MulchRegistry>("MulchRegistry");
        font = registry != null ? registry.font : null;
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? (RectTransform)canvas.transform : null;

        RectTransform root = (RectTransform)transform;
        Vector2 anchor = registry != null ? registry.barAnchor : new Vector2(0.38f, 0f);
        root.anchorMin = root.anchorMax = anchor;
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = registry != null ? registry.barOffset : new Vector2(0f, 6f);
        root.sizeDelta = Vector2.zero;

        int inventorySize = MulchManager.instance != null ? MulchManager.instance.InventorySize : 8;
        int plantSlotCount = MulchManager.instance != null ? MulchManager.instance.PlantSlots : 6;

        inventoryPart = NewPart("Inventory", inventorySize, new Vector2(0.5f, 0f), Vector2.zero, inventorySlots, false);
        float inventoryWidth = inventoryPart.sizeDelta.x;
        plantPart = NewPart("PlantSlots", plantSlotCount, new Vector2(0f, 0f), new Vector2(inventoryWidth / 2f + PartGap, 0f), plantSlots, true);

        plantLabel = NewText("PlantName", plantPart, 18f, TextAlignmentOptions.BottomLeft);
        RectTransform labelRect = plantLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 0f);
        labelRect.anchoredPosition = new Vector2(4f, 2f);
        labelRect.sizeDelta = new Vector2(0f, 26f);

        BuildOverlay();
        RefreshAll();
        RefreshPlantPart(null);
    }

    private RectTransform NewPart(string name, int slotCount, Vector2 pivot, Vector2 position, List<MulchSlotView> slots, bool isPlant)
    {
        float size = registry != null ? registry.slotSize : 54f;
        float spacing = registry != null ? registry.slotSpacing : 6f;

        GameObject partGO = new GameObject(name, typeof(RectTransform));
        partGO.transform.SetParent(transform, false);
        RectTransform part = (RectTransform)partGO.transform;
        part.anchorMin = part.anchorMax = new Vector2(0.5f, 0f);
        part.pivot = pivot;
        part.anchoredPosition = position;
        part.sizeDelta = new Vector2(slotCount * size + (slotCount - 1) * spacing + Padding * 2f, size + Padding * 2f);
        Image background = partGO.AddComponent<Image>();
        background.color = new Color(0.07f, 0.06f, 0.05f, 0.78f);

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotGO = new GameObject($"Slot{i}", typeof(RectTransform));
            slotGO.transform.SetParent(part, false);
            RectTransform slotRect = (RectTransform)slotGO.transform;
            slotRect.anchorMin = slotRect.anchorMax = slotRect.pivot = new Vector2(0f, 0f);
            slotRect.anchoredPosition = new Vector2(Padding + i * (size + spacing), Padding);
            slotRect.sizeDelta = new Vector2(size, size);

            Image frame = slotGO.AddComponent<Image>();
            frame.raycastTarget = true;

            GameObject innerGO = new GameObject("Inner", typeof(RectTransform));
            innerGO.transform.SetParent(slotGO.transform, false);
            RectTransform innerRect = (RectTransform)innerGO.transform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);
            Image inner = innerGO.AddComponent<Image>();
            inner.color = new Color(0.12f, 0.11f, 0.09f, 1f);
            inner.raycastTarget = false;

            GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(slotGO.transform, false);
            RectTransform iconRect = (RectTransform)iconGO.transform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(7f, 7f);
            iconRect.offsetMax = new Vector2(-7f, -7f);
            Image icon = iconGO.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            MulchSlotView view = slotGO.AddComponent<MulchSlotView>();
            view.ui = this;
            view.isPlantSlot = isPlant;
            view.index = i;
            view.frame = frame;
            view.icon = icon;
            slots.Add(view);
        }
        return part;
    }

    private void BuildOverlay()
    {
        GameObject overlayGO = new GameObject("MulchOverlay", typeof(RectTransform));
        overlayGO.transform.SetParent(canvasRect != null ? canvasRect : transform.parent, false);
        overlay = (RectTransform)overlayGO.transform;
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;

        GameObject tipGO = new GameObject("Tooltip", typeof(RectTransform));
        tipGO.transform.SetParent(overlay, false);
        tooltipRect = (RectTransform)tipGO.transform;
        tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.sizeDelta = new Vector2(380f, 0f);
        Image tipBackground = tipGO.AddComponent<Image>();
        tipBackground.color = new Color(0.06f, 0.06f, 0.06f, 0.95f);
        tipBackground.raycastTarget = false;
        VerticalLayoutGroup layout = tipGO.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        tipGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tooltipText = NewText("Text", tooltipRect, 20f, TextAlignmentOptions.TopLeft);
        tipGO.SetActive(false);

        GameObject ghostGO = new GameObject("DragGhost", typeof(RectTransform));
        ghostGO.transform.SetParent(overlay, false);
        dragGhost = ghostGO.AddComponent<Image>();
        dragGhost.raycastTarget = false;
        dragGhost.preserveAspect = true;
        float size = registry != null ? registry.slotSize : 54f;
        dragGhost.rectTransform.sizeDelta = new Vector2(size, size);
        ghostGO.SetActive(false);
    }

    private TextMeshProUGUI NewText(string name, Transform parent, float size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.richText = true;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    // ── refreshing ──────────────────────────────────────────────────────────

    private Sprite Placeholder => registry != null ? registry.placeholderIcon : null;

    public void RefreshAll()
    {
        // a new level empties the inventory: anything still flying in is gone
        if (MulchManager.instance != null)
            incoming.RemoveWhere(slot => MulchManager.instance.GetSlot(slot) == null);
        foreach (MulchSlotView slot in inventorySlots) slot.Refresh(Placeholder);
        RefreshPlantPart(shownPlant);
    }

    private void RefreshPlantPart(Plant plant)
    {
        if (plant != null && !plant.IsAlive) plant = null;
        shownPlant = plant;
        shownPlantMulchCount = plant != null ? plant.mulches.Count : -1;
        if (plantPart == null) return;
        plantPart.gameObject.SetActive(plant != null);
        if (plant == null) return;
        int slots = MulchManager.instance != null ? MulchManager.instance.PlantSlots : plantSlots.Count;
        plantLabel.text = $"{plant.GetName()} <size=80%><color=#BBBBBB>{plant.mulches.Count}/{slots}</color></size>";
        foreach (MulchSlotView slot in plantSlots) slot.Refresh(Placeholder);
    }

    public MulchEffect PlantSlotEffect(int index) =>
        shownPlant != null && index >= 0 && index < shownPlant.mulches.Count ? shownPlant.mulches[index] : null;

    public MulchItem PlantSlotItem(int index) => PlantSlotEffect(index)?.Item;

    // ── dragging ────────────────────────────────────────────────────────────

    public void BeginDrag(int slot, Vector2 screenPosition)
    {
        MulchItem item = MulchManager.instance != null ? MulchManager.instance.GetSlot(slot) : null;
        if (item == null) return;
        draggingSlot = slot;
        dropHandled = false;
        dragGhost.sprite = item.data.icon != null ? item.data.icon : Placeholder;
        dragGhost.color = new Color(MulchTierColors.Get(item.rarity).r, MulchTierColors.Get(item.rarity).g, MulchTierColors.Get(item.rarity).b, 0.85f);
        dragGhost.gameObject.SetActive(true);
        overlay.SetAsLastSibling();
        MoveDrag(screenPosition);
    }

    public void MoveDrag(Vector2 screenPosition)
    {
        if (draggingSlot < 0) return;
        dragGhost.rectTransform.anchoredPosition = ScreenToOverlay(screenPosition);
    }

    // a slot under the pointer takes it (it runs before EndDrag), so this only handles the map
    public void EndDrag(Vector2 screenPosition)
    {
        if (draggingSlot < 0) return;
        int slot = draggingSlot;
        draggingSlot = -1;
        dragGhost.gameObject.SetActive(false);
        if (dropHandled || MulchManager.instance == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Plant plant = PlantAt(screenPosition);
        if (plant != null) MulchManager.instance.EquipFromSlot(slot, plant);
    }

    public void DropOnSlot(MulchSlotView target)
    {
        if (draggingSlot < 0 || target == null || MulchManager.instance == null) return;
        dropHandled = true;
        // a slot a picked up drop is still flying into only looks empty: leave it alone
        if (!target.isPlantSlot && IsIncoming(target.index)) return;
        if (target.isPlantSlot)
        {
            if (shownPlant != null) MulchManager.instance.EquipFromSlot(draggingSlot, shownPlant);
        }
        else
        {
            MulchManager.instance.MoveOrMerge(draggingSlot, target.index);
        }
    }

    // the plant on the tile under the pointer, or any plant whose collider is there
    private static Plant PlantAt(Vector2 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z));
        world.z = 0f;
        if (Tile.allTiles.TryGetValue(Tile.TileKey(world), out Tile tile))
        {
            Plant onTile = Plant.GetPlantOnTile(tile);
            if (onTile != null && onTile.IsAlive) return onTile;
        }
        foreach (Collider2D hit in Physics2D.OverlapPointAll(world))
        {
            Plant plant = hit != null ? hit.GetComponentInParent<Plant>() : null;
            if (plant != null && plant.IsAlive) return plant;
        }
        return null;
    }

    // ── picking up ──────────────────────────────────────────────────────────

    // a clicked ground drop flies from where it lay into its inventory slot, the way collected sun
    // flies to the counter in plants vs. zombies: it speeds off and eases into the slot, shrinking
    // or growing to the slot's icon size, then the slot pops. real time, so it also lands while paused
    public void FlyToSlot(MulchItem item, int slot, Vector3 worldPosition, float worldSize)
    {
        if (item == null || slot < 0 || slot >= inventorySlots.Count || overlay == null) return;
        Camera cam = Camera.main;
        if (cam == null) { inventorySlots[slot].Refresh(Placeholder); return; }

        incoming.Add(slot);
        inventorySlots[slot].Refresh(Placeholder);

        GameObject go = new GameObject("FlyingMulch", typeof(RectTransform));
        go.transform.SetParent(overlay, false);
        Image image = go.AddComponent<Image>();
        image.sprite = item.data.icon != null ? item.data.icon : Placeholder;
        image.color = MulchTierColors.Get(item.rarity);
        image.preserveAspect = true;
        image.raycastTarget = false;
        overlay.SetAsLastSibling();

        Vector2 from = ScreenToOverlay(cam.WorldToScreenPoint(worldPosition));
        Vector2 edge = ScreenToOverlay(cam.WorldToScreenPoint(worldPosition + new Vector3(worldSize, 0f, 0f)));
        float fromSize = Mathf.Max(8f, Mathf.Abs(edge.x - from.x));
        StartCoroutine(Fly(image, slot, from, fromSize));
    }

    private System.Collections.IEnumerator Fly(Image image, int slot, Vector2 from, float fromSize)
    {
        float duration = registry != null ? Mathf.Max(0.05f, registry.flySeconds) : 0.55f;
        float size = registry != null ? registry.slotSize : 54f;
        float toSize = Mathf.Max(8f, size - 14f);
        RectTransform rect = image.rectTransform;
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            // ease out: quick start, gentle arrival
            float e = 1f - Mathf.Pow(1f - k, 3f);
            Vector2 to = SlotCenter(slot);
            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            float s = Mathf.Lerp(fromSize, toSize, e);
            rect.sizeDelta = new Vector2(s, s);
            yield return null;
        }
        if (image != null) Destroy(image.gameObject);
        incoming.Remove(slot);
        if (slot < inventorySlots.Count)
        {
            inventorySlots[slot].Refresh(Placeholder);
            inventorySlots[slot].Pop();
        }
    }

    // a slot's middle, in the overlay's space
    private Vector2 SlotCenter(int slot)
    {
        RectTransform slotRect = (RectTransform)inventorySlots[slot].transform;
        Vector3 world = slotRect.TransformPoint(slotRect.rect.center);
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return ScreenToOverlay(RectTransformUtility.WorldToScreenPoint(cam, world));
    }

    // ── tooltip ─────────────────────────────────────────────────────────────

    public void ShowTooltip(MulchItem item, MulchEffect effect, string hint)
    {
        if (item == null || tooltipRect == null) return;
        // title, a blank line, the lead in line, a blank line, then the description (Tommy, 2026-11-20)
        string text = $"<b>{item.Name}</b>  <size=80%>{MulchData.RarityLabel(item.rarity)}</size>\n\n" +
            $"<size=90%>Whenever this mulch is equipped on a plant:</size>\n\n<size=90%>{item.Description}</size>";
        string status = effect != null ? effect.GetStatus() : null;
        if (!string.IsNullOrEmpty(status)) text += $"\n<size=85%><color=#FFD27F>{status}</color></size>";
        if (!string.IsNullOrEmpty(hint)) text += $"\n<size=75%><color=#AAAAAA>{hint}</color></size>";
        tooltipText.text = text;
        tooltipRect.gameObject.SetActive(true);
        overlay.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        PlaceTooltip();
    }

    public void HideTooltip()
    {
        if (tooltipRect != null) tooltipRect.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (tooltipRect != null && tooltipRect.gameObject.activeSelf) PlaceTooltip();
    }

    // next to the pointer, on whichever side keeps it on screen (above the pointer near the bottom)
    private void PlaceTooltip()
    {
        if (Mouse.current == null || overlay == null) return;
        Vector2 local = ScreenToOverlay(Mouse.current.position.ReadValue());
        Vector2 size = tooltipRect.sizeDelta;
        Rect area = overlay.rect;
        bool right = local.x + 18f + size.x <= area.xMax;
        bool up = local.y + 18f + size.y <= area.yMax;
        tooltipRect.pivot = new Vector2(right ? 0f : 1f, up ? 0f : 1f);
        tooltipRect.anchoredPosition = local + new Vector2(right ? 18f : -18f, up ? 18f : -18f);
    }

    private Vector2 ScreenToOverlay(Vector2 screenPosition)
    {
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screenPosition, cam, out Vector2 local);
        return local;
    }
}

// tooltips for mulches, from the inventory bar, plant slots and ground drops alike
public static class MulchTooltip
{
    public static void Show(MulchItem item, MulchEffect effect, string hint) => MulchInventoryUI.instance?.ShowTooltip(item, effect, hint);
    public static void Hide() => MulchInventoryUI.instance?.HideTooltip();
}
