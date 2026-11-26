using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

// the mulch UI in the right side panel (Tommy's layout, 2026-11-20): the player's mulch bag
// (MulchRegistry.inventorySize slots) as two rows of 3 left of the pot and shovel boxes, right
// above the plant info panel, and the selected plant's mulch slots (plantSlots) as a row at the
// bottom of that panel (both the plant info panel pack's round 6, Tommy 2026-11-20). this
// component sits on Canvas.prefab's MulchBag object, whose Slot0 to Slot5 are placed in the prefab
// and taken over here; without them the bag builds rows of bagColumns sized to its rect. plantSlotsRoot is the plant's row: the info panel's MulchSlots window, whose
// Slot0 to Slot5 children (slot sprite plus an Icon child) are placed in the prefab and taken over
// here; a plantSlotsRoot without them gets a built row with a "Mulches 2/6" label instead. drag a
// bag mulch onto another slot to merge or swap, onto a plant on the map or onto the plant's row to
// equip it; right click opens a Sell popup on the mulch (MulchSellPopup) and that sells it.
// tooltips, the sell popup and drag ghosts go on an overlay that's the canvas's last child, above everything
public class MulchInventoryUI : MonoBehaviour
{
    public static MulchInventoryUI instance;

    [Tooltip("the selected plant's row: label on the left, its slots after it")]
    [SerializeField] private RectTransform plantSlotsRoot;
    [Tooltip("width of the plant row's label (\"Mulches 2/6\")")]
    [SerializeField] private float plantLabelWidth = 112f;
    [Tooltip("slots per row in the mulch bag; the rows fill the bag's rect top to bottom (Tommy, 2026-11-20: " +
        "two rows of 3, as tall as the tool buttons)")]
    [SerializeField] private int bagColumns = 3;

    private MulchRegistry registry;
    private TMP_FontAsset font;
    private Canvas canvas;
    private RectTransform canvasRect;
    private bool built;

    private TextMeshProUGUI bagLabel;
    private TextMeshProUGUI plantLabel;
    private readonly List<MulchSlotView> inventorySlots = new List<MulchSlotView>();
    private readonly List<MulchSlotView> plantSlots = new List<MulchSlotView>();
    private float bagSlotSize = 46f;
    private Plant shownPlant;
    private int shownPlantMulchCount = -1;

    // tooltip and drag ghost live on their own layer, the canvas's last child, above everything
    private RectTransform overlay;
    private RectTransform tooltipRect;
    private TextMeshProUGUI tooltipText;
    private Image dragGhost;
    private int draggingSlot = -1;
    private bool dropHandled;

    // the Sell popup and the bag slot it's open on (-1: closed), with the mulch it was opened for
    private MulchSellPopup sellPopup;
    private int sellSlot = -1;
    private MulchItem sellItem;
    // the slot whose popup the current right press closed, so that press's click doesn't open it again
    private int closedOnPress = -1;
    // the same popup opened on a ground drop by a right click on it (Tommy, 2026-11-22), and the drop
    // whose popup the current right press closed
    private MulchDrop sellDrop;
    private bool sellOnDrop;
    private MulchDrop closedDropOnPress;
    public bool SellPopupOpen => sellPopup != null && sellPopup.IsOpen;

    // plants a hovered (or dragged) bag mulch would merge with show a highlight in its rarity's
    // colour (Tommy, 2026-11-22), and the same for a ground drop (2026-11-23)
    private int hoveredSlot = -1;
    private readonly HashSet<Plant> mergeHighlighted = new HashSet<Plant>();
    private readonly HashSet<Plant> mergeWanted = new HashSet<Plant>();

    // inventory slots a picked up drop is still flying into: already filled, shown empty until it lands
    private readonly HashSet<int> incoming = new HashSet<int>();
    public bool IsIncoming(int slot) => incoming.Contains(slot);

    private void Awake()
    {
        // a level scene's own copy of the canvas is thrown away when the persistent one already exists
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    // called at every level start (MulchManager.ResetForLevel). normally the component is already
    // on Canvas.prefab; without it, a bag is made in the right panel at the prefab's default spot
    public static void Ensure()
    {
        if (instance == null && PersistentCanvas.instance != null)
        {
            Debug.LogWarning("MulchInventoryUI: no MulchBag on the canvas, building one in code");
            GameObject go = new GameObject("MulchBag", typeof(RectTransform));
            go.transform.SetParent(PersistentCanvas.instance.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(596f, 126f);
            rect.sizeDelta = new Vector2(200f, 132f);
            go.AddComponent<MulchInventoryUI>();
        }
        if (instance == null) return;
        if (!instance.built) instance.Build();
        instance.RefreshAll();
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
        if (!built) return;
        UpdateSellPopup();
        UpdateMergeHighlights();
        Plant selected = PlantUpgradeUI.instance != null ? PlantUpgradeUI.instance.GetSelectedPlant() : null;
        if (selected != null && !selected.IsAlive) selected = null;
        if (selected != shownPlant || (selected != null && selected.mulches.Count != shownPlantMulchCount))
            RefreshPlantPart(selected);
    }

    // ── building ────────────────────────────────────────────────────────────

    private void Build()
    {
        built = true;
        registry = MulchManager.instance != null ? MulchManager.instance.Registry : Resources.Load<MulchRegistry>("MulchRegistry");
        font = registry != null ? registry.font : null;
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        float spacing = registry != null ? registry.slotSpacing : 6f;

        int inventorySize = MulchManager.instance != null ? MulchManager.instance.InventorySize : 6;
        RectTransform bag = (RectTransform)transform;
        // round 6: the bag's slots are placed in the prefab like the plant row's (2 rows of 3, no
        // label or count). without all of them it builds its own grid with a "Mulch bag 3/6" label
        if (PlacedSlotCount(bag, inventorySize) == inventorySize)
        {
            AdoptPlacedSlots(bag, inventorySize, inventorySlots, false);
            bagSlotSize = ((RectTransform)inventorySlots[0].transform).rect.width;
        }
        else
        {
            bagSlotSize = BuildGrid(bag, 0f, inventorySize, bagColumns, spacing, inventorySlots, false);
            bagLabel = NewText("BagLabel", bag, 15f, TextAlignmentOptions.BottomLeft);
            RectTransform bagLabelRect = bagLabel.rectTransform;
            bagLabelRect.anchorMin = new Vector2(0f, 1f);
            bagLabelRect.anchorMax = new Vector2(1f, 1f);
            bagLabelRect.pivot = new Vector2(0f, 0f);
            bagLabelRect.anchoredPosition = new Vector2(2f, 2f);
            bagLabelRect.sizeDelta = new Vector2(0f, 18f);
        }

        int placedSlotCount = MulchManager.instance != null ? MulchManager.instance.PlantSlots : 6;
        if (plantSlotsRoot != null && AdoptPlacedSlots(plantSlotsRoot, placedSlotCount, plantSlots, true) == 0)
        {
            int plantSlotCount = placedSlotCount;
            plantLabel = NewText("PlantMulchLabel", plantSlotsRoot, 15f, TextAlignmentOptions.MidlineLeft);
            RectTransform labelRect = plantLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(plantLabelWidth, 0f);
            BuildGrid(plantSlotsRoot, plantLabelWidth, plantSlotCount, plantSlotCount, spacing, plantSlots, true);
        }

        BuildOverlay();
        RefreshPlantPart(null);
    }

    // slots placed in the prefab (the plant info panel's row, the bag): children named Slot0, Slot1
    // and so on, each an Image (the slot sprite, drawn as is while empty) with an Icon child.
    // returns how many it took over, stopping at the first one missing
    private int AdoptPlacedSlots(RectTransform root, int count, List<MulchSlotView> slots, bool isPlant)
    {
        int adopted = 0;
        for (int i = 0; i < count; i++)
        {
            if (!PlacedSlot(root, i, out Image frame, out Image icon)) break;
            frame.raycastTarget = true;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            MulchSlotView view = frame.gameObject.AddComponent<MulchSlotView>();
            view.ui = this;
            view.isPlantSlot = isPlant;
            view.index = i;
            view.frame = frame;
            view.icon = icon;
            view.spriteFrame = true;
            slots.Add(view);
            adopted++;
        }
        return adopted;
    }

    private static int PlacedSlotCount(RectTransform root, int count)
    {
        int found = 0;
        while (found < count && PlacedSlot(root, found, out _, out _)) found++;
        return found;
    }

    private static bool PlacedSlot(RectTransform root, int index, out Image frame, out Image icon)
    {
        Transform slot = root.Find($"Slot{index}");
        frame = slot != null ? slot.GetComponent<Image>() : null;
        Transform iconTransform = slot != null ? slot.Find("Icon") : null;
        icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        return frame != null && icon != null;
    }

    // square slots in rows of columns (left to right, then top to bottom) inside parent, from
    // leftInset to its right edge, as big as fit the parent's height and width, the block centred
    // in that area. returns the slot size
    private float BuildGrid(RectTransform parent, float leftInset, int count, int columns, float spacing, List<MulchSlotView> slots, bool isPlant)
    {
        Rect area = parent.rect;
        columns = Mathf.Clamp(columns, 1, Mathf.Max(1, count));
        int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
        float width = Mathf.Max(1f, area.width - leftInset);
        float size = Mathf.Floor(Mathf.Min((area.height - (rows - 1) * spacing) / rows,
            (width - (columns - 1) * spacing) / columns));
        float gridWidth = columns * size + (columns - 1) * spacing;
        float gridHeight = rows * size + (rows - 1) * spacing;
        float x0 = leftInset + (width - gridWidth) / 2f;
        // the top row's top edge, from the parent's middle
        float y0 = gridHeight / 2f;

        for (int i = 0; i < count; i++)
        {
            GameObject slotGO = new GameObject($"Slot{i}", typeof(RectTransform));
            slotGO.transform.SetParent(parent, false);
            RectTransform slotRect = (RectTransform)slotGO.transform;
            slotRect.anchorMin = slotRect.anchorMax = new Vector2(0f, 0.5f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.anchoredPosition = new Vector2(x0 + (i % columns) * (size + spacing), y0 - (i / columns) * (size + spacing));
            slotRect.sizeDelta = new Vector2(size, size);

            Image frame = slotGO.AddComponent<Image>();
            frame.raycastTarget = true;

            float border = Mathf.Max(2f, Mathf.Round(size * 0.06f));
            GameObject innerGO = new GameObject("Inner", typeof(RectTransform));
            innerGO.transform.SetParent(slotGO.transform, false);
            RectTransform innerRect = (RectTransform)innerGO.transform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(border, border);
            innerRect.offsetMax = new Vector2(-border, -border);
            Image inner = innerGO.AddComponent<Image>();
            inner.color = new Color(0.12f, 0.11f, 0.09f, 1f);
            inner.raycastTarget = false;

            float iconInset = Mathf.Round(size * 0.14f);
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(slotGO.transform, false);
            RectTransform iconRect = (RectTransform)iconGO.transform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(iconInset, iconInset);
            iconRect.offsetMax = new Vector2(-iconInset, -iconInset);
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
        return size;
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

        // first, so the tooltip and the drag ghost draw over it
        sellPopup = MulchSellPopup.Create(overlay, font);

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
        // Tommy's flower frame (2026-11-26), the text clear of it
        if (TooltipFrameLook.Available)
        {
            TooltipFrameLook.Apply(tipBackground);
            int inset = (int)TooltipFrameLook.Inset;
            layout.padding = new RectOffset(inset, inset, inset, inset);
        }
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
        dragGhost.rectTransform.sizeDelta = new Vector2(bagSlotSize, bagSlotSize);
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

    public void RefreshAll()
    {
        if (!built) return;
        // a new level empties the inventory: anything still flying in is gone
        if (MulchManager.instance != null)
            incoming.RemoveWhere(slot => MulchManager.instance.GetSlot(slot) == null);
        // the mulch the Sell popup was opened for left its slot (sold, moved, merged, equipped, new level)
        if (sellSlot >= 0 && (MulchManager.instance == null || MulchManager.instance.GetSlot(sellSlot) != sellItem))
            CloseSellPopup();
        foreach (MulchSlotView slot in inventorySlots) slot.Refresh();
        if (bagLabel != null && MulchManager.instance != null)
        {
            int held = 0;
            foreach (MulchItem item in MulchManager.instance.Inventory)
                if (item != null) held++;
            bagLabel.text = $"Mulch bag <color=#BBBBBB>{held}/{MulchManager.instance.InventorySize}</color>";
        }
        RefreshPlantPart(shownPlant);
    }

    private void RefreshPlantPart(Plant plant)
    {
        if (plant != null && !plant.IsAlive) plant = null;
        shownPlant = plant;
        shownPlantMulchCount = plant != null ? plant.mulches.Count : -1;
        if (plantSlotsRoot == null) return;
        plantSlotsRoot.gameObject.SetActive(plant != null);
        if (plant == null) return;
        int slots = MulchManager.instance != null ? MulchManager.instance.PlantSlots : plantSlots.Count;
        if (plantLabel != null)
            plantLabel.text = $"Mulches <color=#BBBBBB>{plant.mulches.Count}/{slots}</color>";
        foreach (MulchSlotView slot in plantSlots) slot.Refresh();
    }

    public MulchEffect PlantSlotEffect(int index) =>
        shownPlant != null && index >= 0 && index < shownPlant.mulches.Count ? shownPlant.mulches[index] : null;

    public MulchItem PlantSlotItem(int index) => PlantSlotEffect(index)?.Item;
    // the plant whose slots the panel shows
    public Plant ShownPlant => shownPlant;

    // ── dragging ────────────────────────────────────────────────────────────

    public void BeginDrag(int slot, Vector2 screenPosition)
    {
        MulchItem item = MulchManager.instance != null ? MulchManager.instance.GetSlot(slot) : null;
        if (item == null) return;
        CloseSellPopup();
        draggingSlot = slot;
        dropHandled = false;
        MulchIcon.Show(dragGhost, item, 0.85f);
        dragGhost.rectTransform.sizeDelta = new Vector2(bagSlotSize, bagSlotSize);
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
    public static Plant PlantAt(Vector2 screenPosition)
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

    // ── dragging a ground drop ──────────────────────────────────────────────

    // a ground drop being dragged shows this opaque copy under the pointer, at the drop's own size
    // on screen, while the drop itself stays where it lies, faded (MulchDrop)
    private bool groundDragging;
    private Coroutine ghostReturn;

    public void BeginGroundDrag(MulchItem item, Vector2 screenPosition, Vector3 worldPosition, float worldSize)
    {
        if (item == null || overlay == null) return;
        if (ghostReturn != null) { StopCoroutine(ghostReturn); ghostReturn = null; }
        groundDragging = true;
        HideTooltip();
        MulchIcon.Show(dragGhost, item);
        float size = WorldSizeOnOverlay(worldPosition, worldSize);
        dragGhost.rectTransform.sizeDelta = new Vector2(size, size);
        dragGhost.gameObject.SetActive(true);
        overlay.SetAsLastSibling();
        MoveGroundDrag(screenPosition);
    }

    public void MoveGroundDrag(Vector2 screenPosition)
    {
        if (!groundDragging) return;
        dragGhost.rectTransform.anchoredPosition = ScreenToOverlay(screenPosition);
    }

    // what a ground drop was let go over: the inventory bar, a plant (on the map, or the selected
    // plant's slots), or nothing
    public enum GroundDropTarget { Nothing, Inventory, Plant }

    public GroundDropTarget TargetUnder(Vector2 screenPosition, out Plant plant)
    {
        plant = null;
        if (UIUnder(screenPosition, out MulchSlotView slot))
        {
            if (slot == null) return GroundDropTarget.Nothing;
            if (!slot.isPlantSlot) return GroundDropTarget.Inventory;
            plant = shownPlant;
            return plant != null ? GroundDropTarget.Plant : GroundDropTarget.Nothing;
        }
        plant = PlantAt(screenPosition);
        return plant != null ? GroundDropTarget.Plant : GroundDropTarget.Nothing;
    }

    // the bag slot under the pointer, -1 for none (another UI, a plant slot or the map)
    public int InventorySlotAt(Vector2 screenPosition) =>
        UIUnder(screenPosition, out MulchSlotView slot) && slot != null && !slot.isPlantSlot ? slot.index : -1;

    // true when the topmost UI under the pointer is on this canvas, with the mulch slot it belongs to
    // (null for other UI)
    private static bool UIUnder(Vector2 screenPosition, out MulchSlotView slot)
    {
        slot = null;
        if (EventSystem.current == null) return false;
        PointerEventData pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        foreach (RaycastResult hit in hits)
        {
            if (!(hit.module is GraphicRaycaster)) continue;
            slot = hit.gameObject.GetComponentInParent<MulchSlotView>();
            return true;
        }
        return false;
    }

    // ── merge highlights ────────────────────────────────────────────────────

    // a bag slot's pointer enter and exit (MulchSlotView)
    public void SetHoveredSlot(int slot, bool hovering)
    {
        if (hovering) hoveredSlot = slot;
        else if (hoveredSlot == slot) hoveredSlot = -1;
    }

    // every frame, since other highlights (a plant's own hover ending) clear it. a ground drop being
    // hovered or dragged highlights the same way (Tommy, 2026-11-23); whatever is dragged comes first
    private void UpdateMergeHighlights()
    {
        MulchItem item = BagItem(draggingSlot) ?? MulchDrop.DraggedItem ?? BagItem(hoveredSlot) ?? MulchDrop.HoveredItem;

        mergeWanted.Clear();
        if (item != null)
            foreach (Plant plant in Plant.allPlants)
                if (MulchManager.MergesOnto(item, plant)) mergeWanted.Add(plant);

        foreach (Plant plant in mergeHighlighted)
            if (plant != null && !mergeWanted.Contains(plant)) plant.ClearHighlight();
        mergeHighlighted.Clear();
        if (item == null) return;

        Color color = MulchTierColors.Get(item.rarity);
        foreach (Plant plant in mergeWanted)
        {
            plant.SetHighlight(color);
            mergeHighlighted.Add(plant);
        }
    }

    // the mulch in a bag slot, null for none, -1 or a slot a drop is still flying into
    private MulchItem BagItem(int slot) =>
        slot >= 0 && MulchManager.instance != null && !IsIncoming(slot) ? MulchManager.instance.GetSlot(slot) : null;

    // the drop went somewhere: the copy just goes
    public void EndGroundDrag()
    {
        groundDragging = false;
        if (ghostReturn != null) { StopCoroutine(ghostReturn); ghostReturn = null; }
        dragGhost.gameObject.SetActive(false);
    }

    // let go over nothing: the copy glides back onto the drop, then the drop shows again (done)
    public void ReturnGroundDrag(Vector3 worldPosition, System.Action done)
    {
        groundDragging = false;
        Camera cam = Camera.main;
        if (cam == null || !dragGhost.gameObject.activeSelf) { dragGhost.gameObject.SetActive(false); done?.Invoke(); return; }
        if (ghostReturn != null) StopCoroutine(ghostReturn);
        ghostReturn = StartCoroutine(ReturnGhost(ScreenToOverlay(cam.WorldToScreenPoint(worldPosition)), done));
    }

    private System.Collections.IEnumerator ReturnGhost(Vector2 to, System.Action done)
    {
        const float duration = 0.18f;
        RectTransform rect = dragGhost.rectTransform;
        Vector2 from = rect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(from, to, 1f - Mathf.Pow(1f - k, 3f));
            yield return null;
        }
        dragGhost.gameObject.SetActive(false);
        ghostReturn = null;
        done?.Invoke();
    }

    // a world size (at a world spot) in the overlay's units
    private float WorldSizeOnOverlay(Vector3 worldPosition, float worldSize)
    {
        Camera cam = Camera.main;
        if (cam == null) return bagSlotSize;
        Vector2 a = ScreenToOverlay(cam.WorldToScreenPoint(worldPosition));
        Vector2 b = ScreenToOverlay(cam.WorldToScreenPoint(worldPosition + new Vector3(worldSize, 0f, 0f)));
        return Mathf.Max(8f, Mathf.Abs(b.x - a.x));
    }

    // ── picking up ──────────────────────────────────────────────────────────

    // a clicked ground drop flies from where it lay into its inventory slot, the way collected sun
    // flies to the counter in plants vs. zombies: it speeds off and eases into the slot, shrinking
    // or growing to the slot's icon size, then the slot pops. real time, so it also lands while paused
    public void FlyToSlot(MulchItem item, int slot, Vector3 worldPosition, float worldSize)
    {
        if (item == null || slot < 0 || slot >= inventorySlots.Count || overlay == null) return;
        Camera cam = Camera.main;
        if (cam == null) { inventorySlots[slot].Refresh(); return; }

        incoming.Add(slot);
        inventorySlots[slot].Refresh();

        GameObject go = new GameObject("FlyingMulch", typeof(RectTransform));
        go.transform.SetParent(overlay, false);
        Image image = go.AddComponent<Image>();
        image.preserveAspect = true;
        MulchIcon.Show(image, item);
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
        float toSize = Mathf.Max(8f, bagSlotSize * 0.72f);
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
        // the slot sees the mulch is new and pops it in
        if (slot < inventorySlots.Count) inventorySlots[slot].Refresh();
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

    // the sell popup: a right click on a bag mulch opens it on that slot, or closes it when it's
    // already open there
    public void ToggleSellPopup(int slot)
    {
        if (slot == closedOnPress) { closedOnPress = -1; return; }
        if (slot == sellSlot) { CloseSellPopup(); return; }
        MulchItem item = MulchManager.instance != null && !IsIncoming(slot) ? MulchManager.instance.GetSlot(slot) : null;
        if (item == null || sellPopup == null || slot < 0 || slot >= inventorySlots.Count) return;
        CloseSellPopup();
        sellSlot = slot;
        sellItem = item;
        HideTooltip();
        overlay.SetAsLastSibling();
        sellPopup.Open(SlotCenter(slot), MulchManager.instance.SellPrice(item), SellFromPopup, CloseSellPopup);
    }

    private void SellFromPopup()
    {
        int slot = sellSlot;
        CloseSellPopup();
        if (slot >= 0) MulchManager.instance?.Sell(slot);
    }

    // a right click on a ground drop opens the same popup over it, or closes it when it's already
    // open there; its button sells the drop where it lies
    public void ToggleGroundSellPopup(MulchDrop drop)
    {
        if (drop == null) return;
        if (drop == closedDropOnPress) { closedDropOnPress = null; return; }
        if (sellOnDrop && drop == sellDrop) { CloseSellPopup(); return; }
        Camera cam = Camera.main;
        if (sellPopup == null || MulchManager.instance == null || cam == null || drop.Item == null) return;
        CloseSellPopup();
        sellDrop = drop;
        sellOnDrop = true;
        HideTooltip();
        overlay.SetAsLastSibling();
        sellPopup.Open(ScreenToOverlay(cam.WorldToScreenPoint(drop.VisualPosition)), MulchManager.instance.SellPrice(drop.Item),
            SellDropFromPopup, CloseSellPopup);
    }

    private void SellDropFromPopup()
    {
        MulchDrop drop = sellDrop;
        CloseSellPopup();
        if (drop != null) drop.Sell();
    }

    public void CloseSellPopup()
    {
        sellSlot = -1;
        sellItem = null;
        sellDrop = null;
        sellOnDrop = false;
        if (sellPopup != null && sellPopup.IsOpen) sellPopup.Close();
    }

    // a click anywhere but on the popup closes it, and so does escape. a right press that closes it
    // over its own slot (or drop) is remembered, so that click toggles it shut instead of opening it
    // again. a drop that vanished (expired) takes its popup with it
    private void UpdateSellPopup()
    {
        if (sellOnDrop && sellDrop == null) CloseSellPopup();
        bool open = sellSlot >= 0 || sellOnDrop;
        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
        {
            closedOnPress = -1;
            closedDropOnPress = null;
            if (open && !OverSellPopup(mouse.position.ReadValue()))
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    closedOnPress = sellSlot;
                    closedDropOnPress = sellDrop;
                }
                CloseSellPopup();
            }
        }
        if ((sellSlot >= 0 || sellOnDrop) && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseSellPopup();
    }

    private bool OverSellPopup(Vector2 screenPosition)
    {
        if (sellPopup == null || !sellPopup.IsOpen) return false;
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(sellPopup.Rect, screenPosition, cam);
    }

    // what the open tooltip shows, kept so it can be rebuilt every frame: timers and other live
    // numbers (drought's seconds left, sun sap's next batch, ironclad riposte's shield) tick while
    // it's up, like status effect tooltips (Tommy, 2026-11-21)
    private MulchItem tooltipItem;
    private MulchEffect tooltipEffect;
    private string tooltipHint;

    public void ShowTooltip(MulchItem item, MulchEffect effect, string hint)
    {
        if (item == null || tooltipRect == null) return;
        tooltipItem = item;
        tooltipEffect = effect;
        tooltipHint = hint;
        tooltipText.text = TooltipText(item, effect, hint);
        tooltipRect.gameObject.SetActive(true);
        overlay.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        PlaceTooltip();
    }

    // title, a blank line, the lead in line, a blank line, the description and its live status,
    // then a blank line before the hint (Tommy, 2026-11-20)
    private static string TooltipText(MulchItem item, MulchEffect effect, string hint)
    {
        AppendTooltipText(tooltipBuilder.Clear(), item, effect, hint);
        return tooltipBuilder.ToString();
    }

    // one builder for the tooltip text, so the live refresh in LateUpdate only makes a string
    // when the text changed
    private static readonly System.Text.StringBuilder tooltipBuilder = new System.Text.StringBuilder(256);

    private static void AppendTooltipText(System.Text.StringBuilder sb, MulchItem item, MulchEffect effect, string hint)
    {
        sb.Append("<b>").Append(item.Name).Append("</b>  <size=80%>").Append(MulchData.RarityLabel(item.rarity)).Append("</size>\n\n")
          .Append("<size=90%>When equipped on a plant:</size>\n\n<size=90%>")
          .Append(item.data != null ? item.data.GetDescription(item, effect) : "").Append("</size>");
        string status = effect != null ? effect.GetStatus() : null;
        if (!string.IsNullOrEmpty(status)) sb.Append("\n<size=85%><color=#FFD27F>").Append(status).Append("</color></size>");
        if (!string.IsNullOrEmpty(hint)) sb.Append("\n\n<size=75%><color=#AAAAAA>").Append(hint).Append("</color></size>");
    }

    // compares the builder with a string without making a new string
    private static bool BuilderMatches(System.Text.StringBuilder builder, string text)
    {
        if (text == null || builder.Length != text.Length) return false;
        for (int i = 0; i < text.Length; i++)
            if (builder[i] != text[i]) return false;
        return true;
    }

    public void HideTooltip()
    {
        if (tooltipRect != null) tooltipRect.gameObject.SetActive(false);
        tooltipItem = null;
        tooltipEffect = null;
        tooltipHint = null;
    }

    private void LateUpdate()
    {
        if (tooltipRect == null || !tooltipRect.gameObject.activeSelf) return;
        if (tooltipItem != null)
        {
            // only re lay out when the text really changed (a timer's whole second ticking over). an
            // equipped mulch follows its effect, which a merge moves to the higher rarity copy
            MulchItem shown = tooltipEffect != null && tooltipEffect.Item != null ? tooltipEffect.Item : tooltipItem;
            AppendTooltipText(tooltipBuilder.Clear(), shown, tooltipEffect, tooltipHint);
            if (!BuilderMatches(tooltipBuilder, tooltipText.text))
            {
                tooltipText.text = tooltipBuilder.ToString();
                LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
            }
        }
        PlaceTooltip();
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
