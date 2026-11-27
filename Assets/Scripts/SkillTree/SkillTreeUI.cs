using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

// top-level controller for the Skill Tree screen. every plant's own SkillTreePlantPanel shows
// or hides itself and manages its own nodes - this class only owns what's global: the spendable
// points readout, the Confirm/Reset buttons, and the shared tooltip box
public class SkillTreeUI : MonoBehaviour
{
    public static SkillTreeUI instance;

    [Header("Info")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Plant Panel Spawning")]
    [SerializeField] private PlantRegistry plantRegistry;
    [SerializeField] private SkillTreePlantPanel panelPrefab;
    [SerializeField] private Transform panelsContainer;

    private readonly List<SkillTreePlantPanel> panels = new List<SkillTreePlantPanel>();

    // reused every frame the tooltip follows the cursor instead of a new array each time
    private static readonly Vector3[] tooltipCorners = new Vector3[4];
    private RectTransform tooltipRect;
    private Canvas tooltipCanvas;

    void Awake()
    {
        instance = this;
        SkinScreen();
        SpawnPanels();
    }

    // picks staged but never confirmed are dropped when the screen is left, as OnBack warns,
    // instead of staying staged for the next visit
    void OnDestroy()
    {
        SkillTreeSession.ClearPending();
        if (instance == this) instance = null;
    }

    private RectTransform TooltipRect
    {
        get
        {
            if (tooltipRect == null && tooltipPanel != null) tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            return tooltipRect;
        }
    }

    // one panel per plant that has an authored tree - each panel then decides for itself
    // (in its own Start) whether the plant is actually unlocked and worth showing. new plants
    // getting a tree, or a save unlocking a new plant, both need zero changes here
    // the plants with a skill tree, in the registry's order: the plant cards measure every name, so they all share
    // one width (Tommy, 2026-11-27)
    public IEnumerable<PlantData> TreePlants
    {
        get
        {
            if (plantRegistry == null || plantRegistry.plants == null) yield break;
            foreach (PlantData d in plantRegistry.plants)
                if (d != null && d.skillTree != null) yield return d;
        }
    }

    private void SpawnPanels()
    {
        if (plantRegistry == null || panelPrefab == null || panelsContainer == null) return;
        // each row is as tall as its card now (SkillTreePlantPanel), so the rows only need a gap between them
        VerticalLayoutGroup rows = panelsContainer.GetComponent<VerticalLayoutGroup>();
        if (rows != null) rows.spacing = SkillTreePlantPanel.RowGap;
        foreach (PlantData data in plantRegistry.plants)
        {
            if (data == null || data.skillTree == null) continue;
            SkillTreePlantPanel panel = Instantiate(panelPrefab, panelsContainer);
            panel.Bind(data);
        }
    }

    void Start()
    {
        HideTooltip();
        RefreshAll();
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !TutorialManager.BlocksEscape)
            OnBack();

        if (tooltipPanel != null && tooltipPanel.activeSelf)
            PositionTooltipAtCursor();
    }

    // called by each SkillTreePlantPanel once it decides it should be shown, so RefreshAll can
    // reach every visible panel without this controller needing to know the plant list itself
    public void RegisterPanel(SkillTreePlantPanel panel)
    {
        if (!panels.Contains(panel)) panels.Add(panel);
    }

    public void RefreshAll()
    {
        // skill points are per-plant now (see SaveData.PlantExpRecord) - this readout is just an
        // at-a-glance sum across every plant's own balance (net of anything currently staged),
        // not a spendable global pool. each panel shows its own plant's actual balance for the
        // number that matters to a purchase
        if (pointsText != null && SaveManager.instance != null)
        {
            int totalDisplay = 0;
            foreach (PlantExpRecord record in SaveManager.instance.saveData.plantExp)
                totalDisplay += SkillTreeSession.DisplaySkillPoints(record.plantName);
            SkillTreeStyle s = SkillTreeStyle.Instance;
            Color number = totalDisplay > 0 ? s.points : s.mutedText;
            pointsText.text = $"Unspent Points: <color=#{ColorUtility.ToHtmlStringRGB(number)}>{totalDisplay}</color>";
        }
        foreach (SkillTreePlantPanel panel in panels)
            if (panel != null) panel.Refresh();
        RefreshUndo();
    }

    // hooked to the Confirm button - commits every staged pick this visit into the real save
    public void OnConfirm()
    {
        SkillTreeSession.ConfirmAll();
    }

    // hooked to the Reset button - refunds everything ever spent and wipes every plant's tree,
    // staged picks included
    public void OnReset()
    {
        SkillTreeSession.ResetAll();
        RefreshAll();
    }

    // mirrors PlantUpgradeUI.ShowTooltip exactly: fixed width (set once on the panel's
    // RectTransform in the Editor, never touched here), auto-fit height only. deliberately not
    // using ContentSizeFitter/VerticalLayoutGroup - the in-level tooltip this is meant to match
    // doesn't use them either, and they fight anything that also tries to position the panel
    // (like following the cursor) or sits inside another layout-controlled container
    public void ShowTooltip(string text, ElementalType element = ElementalType.Neutral) => ShowTooltip(null, text, element);

    // the title (bold, Tommy 2026-11-24) on its own line with the kit's seam under it, then the text;
    // every piece hangs from the panel's top edge, so the pivot PositionTooltipAtCursor picks each
    // frame doesn't move them
    public void ShowTooltip(string title, string text, ElementalType element)
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(true);
        // the kit panel in the hovered plant's element colour, its text shadows to match
        Color shadow = PlantData.ElementTextShadowColor(element);
        // (in the flower frame only its inside takes the colour)
        if (tooltipImage != null)
        {
            if (TooltipFrameLook.Available) TooltipFrameLook.SetInterior(tooltipImage, PlantData.ElementPanelColor(element));
            else tooltipImage.color = PlantData.ElementPanelColor(element);
        }
        if (tooltipShadow != null) tooltipShadow.Color = shadow;
        if (tooltipTitleShadow != null) tooltipTitleShadow.Color = shadow;
        if (tooltipText == null) return;

        Canvas.ForceUpdateCanvases();
        RectTransform panelRect = TooltipRect;
        float width = panelRect.rect.width - 2f * TipSide;
        float y = TipPad;

        bool hasTitle = tooltipTitle != null && !string.IsNullOrEmpty(title);
        if (tooltipTitle != null) tooltipTitle.gameObject.SetActive(hasTitle);
        if (tooltipSeam != null) tooltipSeam.gameObject.SetActive(hasTitle);
        if (hasTitle)
        {
            tooltipTitle.text = title;
            float titleHeight = Mathf.Ceil(tooltipTitle.GetPreferredValues(title, width, 0f).y);
            HangFromTop(tooltipTitle.rectTransform, y, titleHeight, TipSide);
            y += titleHeight + 6f;
            if (tooltipSeam != null) HangFromTop(tooltipSeam, y, 4f, TooltipFrameLook.Available ? 18f : 8f);
            y += 4f + 10f;
        }

        tooltipText.text = text;
        float bodyHeight = Mathf.Ceil(tooltipText.GetPreferredValues(text, width, 0f).y);
        HangFromTop(tooltipText.rectTransform, y, bodyHeight, TipSide);
        y += bodyHeight + TipPad;

        float maxHeight = Screen.height * 0.975f;
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(y, maxHeight));

        Canvas.ForceUpdateCanvases();
        PositionTooltipAtCursor();
    }

    // canvas units: the tooltip's space above the title and below the text, and at its sides (more inside
    // the flower frame, whose band is 14)
    private static float TipPad => TooltipFrameLook.Available ? 22f : 16f;
    private static float TipSide => TooltipFrameLook.Available ? 22f : 20f;

    private static void HangFromTop(RectTransform r, float top, float height, float side)
    {
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.offsetMin = new Vector2(side, -(top + height));
        r.offsetMax = new Vector2(-side, -top);
    }

    // sets world position directly (not anchoredPosition) so this doesn't care what the
    // parent's own pivot/anchor happens to be - anchoredPosition is measured relative to the
    // anchor point, and a mismatch between the panel's anchor and its parent's pivot produces a
    // constant, position-independent offset (this was the "always ~800px off" bug)
    //
    // the pivot itself is picked per-frame from which screen quadrant the cursor is in, so the
    // panel always grows back toward the center of the screen instead of off whichever edge the
    // cursor is near - top-left quadrant puts the cursor at the panel's top-left corner (grows
    // right/down), top-right puts it at the panel's top-right corner (grows left/down), and so
    // on for the bottom two quadrants. changing pivot via script keeps the rect's current screen
    // position, so this is safe to do every frame before repositioning
    //
    // no cursor->corner gap needed - Raycast Target is off on the tooltip's graphics, so it
    // never steals the hover off whatever node triggered it even while sitting right under the
    // cursor, which is what used to cause the show/hide flicker
    private void PositionTooltipAtCursor()
    {
        if (tooltipPanel == null || Mouse.current == null) return;
        RectTransform panelRect = TooltipRect;
        RectTransform parentRect = panelRect.parent as RectTransform;
        if (parentRect == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        bool right = mouseScreenPos.x >= Screen.width * 0.5f;
        bool top   = mouseScreenPos.y >= Screen.height * 0.5f;
        panelRect.pivot = new Vector2(right ? 1f : 0f, top ? 1f : 0f);

        if (tooltipCanvas == null) tooltipCanvas = panelRect.GetComponentInParent<Canvas>();
        Canvas canvas = tooltipCanvas;
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, mouseScreenPos, cam, out Vector3 worldPoint))
            panelRect.position = worldPoint;

        ClampToScreen(panelRect);
    }

    private void ClampToScreen(RectTransform panelRect)
    {
        Vector3[] corners = tooltipCorners;
        panelRect.GetWorldCorners(corners);
        float leftX   = corners[0].x;
        float bottomY = corners[0].y;
        float rightX  = corners[2].x;
        float topY    = corners[2].y;

        Vector3 correction = Vector3.zero;
        if (topY > Screen.height) correction.y = Screen.height - topY;
        else if (bottomY < 0f)    correction.y = -bottomY;

        if (rightX > Screen.width) correction.x = Screen.width - rightX;
        else if (leftX < 0f)       correction.x = -leftX;

        panelRect.position += correction;
    }

    // ---- the look (Tommy's pixel kit, 2026-11-24) ----
    // the scene's own objects keep their places; this dresses them: a kit board behind everything,
    // the scroll view's haze gone, kit scroll bars, the tooltip a kit panel, and Unspent Points,
    // Confirm and Reset in a kit box with Confirm and Reset as the game controls' planks (green and
    // red) that click the scene's own buttons. colours in SkillTreeStyle

    private const float Px = 2f;
    private Image tooltipImage;
    private PixelTextEffect tooltipShadow;
    private TMP_Text tooltipTitle;
    private PixelTextEffect tooltipTitleShadow;
    private RectTransform tooltipSeam;

    private void SkinScreen()
    {
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        SkillTreeStyle s = SkillTreeStyle.Instance;
        Canvas canvas = panelsContainer != null ? panelsContainer.GetComponentInParent<Canvas>(true) : null;
        if (canvas != null) canvas = canvas.rootCanvas;

        // the board, behind everything on the screen
        if (canvas != null)
        {
            RectTransform board = NewImage("Board", canvas.transform, kit.panelBase, s.board, true);
            Stretch(board);
            board.SetAsFirstSibling();
            Stretch(NewImage("Shine", board, kit.panelShine, Color.white, true));
        }

        ScrollRect scroll = panelsContainer != null ? panelsContainer.GetComponentInParent<ScrollRect>(true) : null;
        if (scroll != null)
        {
            // still takes the drags, just isn't seen
            Image haze = scroll.GetComponent<Image>();
            if (haze != null) haze.color = Color.clear;
            SkinScrollbar(scroll.horizontalScrollbar);
            SkinScrollbar(scroll.verticalScrollbar);
        }

        if (tooltipPanel != null)
        {
            tooltipImage = tooltipPanel.GetComponent<Image>();
            // Tommy's flower frame (2026-11-26), drawn as is, its inside in the hovered plant's element colour
            if (tooltipImage != null && TooltipFrameLook.Available)
            {
                TooltipFrameLook.Apply(tooltipImage, PlantData.ElementPanelColor(ElementalType.Neutral));
                tooltipImage.raycastTarget = false;
            }
            else if (tooltipImage != null)
            {
                SetKit(tooltipImage, kit.panelBase, PlantData.ElementPanelColor(ElementalType.Neutral), true);
                tooltipImage.raycastTarget = false;
                RectTransform shine = NewImage("Shine", tooltipPanel.transform, kit.panelShine, Color.white, true);
                Stretch(shine);
                shine.SetAsFirstSibling();
            }
            if (tooltipText != null)
            {
                if (kit.font != null) tooltipText.font = kit.font;
                tooltipText.color = kit.text;
                tooltipText.raycastTarget = false;
                tooltipText.verticalAlignment = VerticalAlignmentOptions.Top;
                tooltipShadow = tooltipText.gameObject.AddComponent<PixelTextEffect>();

                // the title line and the seam under it
                GameObject titleObject = new GameObject("TooltipTitle", typeof(RectTransform));
                titleObject.layer = tooltipPanel.layer;
                titleObject.transform.SetParent(tooltipPanel.transform, false);
                TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
                title.font = tooltipText.font;
                title.fontSize = tooltipText.fontSize;
                title.fontStyle = FontStyles.Bold;
                title.color = kit.text;
                title.horizontalAlignment = tooltipText.horizontalAlignment;
                title.verticalAlignment = VerticalAlignmentOptions.Top;
                title.textWrappingMode = TextWrappingModes.Normal;
                title.richText = true;
                title.raycastTarget = false;
                title.margin = Vector4.zero;
                tooltipTitle = title;
                tooltipTitleShadow = titleObject.AddComponent<PixelTextEffect>();
                tooltipSeam = NewImage("Seam", tooltipPanel.transform, kit.seam, Color.white, false);
            }
        }

        SkinDock();
    }

    // Unspent Points over Undo, Confirm and Reset in one box: Tommy's wood panel (2026-11-27, the plant cards' wood in the
    // dock colour, its gold corners over it), at least its own smallest height, in the screen's bottom right, the line
    // and the planks in its middle, clear of its frame and its corner flowers; the kit's box without the wood. the
    // planks keep the old buttons' spacing and widths
    private const float DockLineGap = 16f;
    private const float DockMargin = 12f;

    private void SkinDock()
    {
        if (pointsText == null) return;
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        SkillTreeStyle s = SkillTreeStyle.Instance;
        Transform parent = pointsText.transform.parent;
        RectTransform confirm = parent.Find("ConfirmButton") as RectTransform;
        RectTransform reset = parent.Find("ResetButton") as RectTransform;

        if (kit.font != null) pointsText.font = kit.font;
        pointsText.fontSize = 20f;
        pointsText.color = kit.text;
        pointsText.textWrappingMode = TextWrappingModes.NoWrap;
        pointsText.overflowMode = TextOverflowModes.Overflow;
        pointsText.raycastTarget = false;
        pointsText.gameObject.AddComponent<PixelTextEffect>().Color = new Color32(0x30, 0x1E, 0x12, 0xFF);

        if (confirm == null || reset == null) return;
        RectTransform label = pointsText.rectTransform;
        bool wood = s.woodPanel != null;
        RectTransform area = parent as RectTransform;
        Rect a = Bounds(confirm, PlankHeight);
        Rect b = Bounds(reset, PlankHeight);
        // Undo goes left of Confirm, as far from it as Reset is on the other side
        Rect u = new Rect(a.x - (b.x - a.x), a.y, a.width, a.height);
        float left, right, top, bottom;
        if (wood && area != null)
        {
            float lineCaps = 7f * Px;
            float block = lineCaps + DockLineGap + PlankHeight;
            float padX = (SkillTreePlantPanel.WoodCornerPx + SkillTreePlantPanel.WoodGapPx) * Px;
            float padY = (SkillTreePlantPanel.WoodFramePx + SkillTreePlantPanel.WoodGapPx) * Px;
            float contentLeft = Mathf.Min(u.xMin, a.xMin, b.xMin), contentRight = Mathf.Max(a.xMax, b.xMax);
            float width = Mathf.Max(SkillTreePlantPanel.WoodMinWidthPx * Px, Mathf.Ceil((contentRight - contentLeft + 2f * padX) / Px) * Px);
            float height = Mathf.Max(SkillTreePlantPanel.WoodMinHeightPx * Px, Mathf.Ceil((block + 2f * padY) / Px) * Px);
            left = Mathf.Round(((contentLeft + contentRight) * 0.5f - width * 0.5f) / Px) * Px;
            // kept on the screen, in its bottom right
            float dx = Mathf.Min(0f, Mathf.Floor((area.rect.xMax - DockMargin - (left + width)) / Px) * Px);
            left += dx;
            right = left + width;
            bottom = Mathf.Round((area.rect.yMin + DockMargin) / Px) * Px;
            top = bottom + height;
            // the line and the planks in the middle of the wood
            float blockTop = Mathf.Round(((bottom + top) * 0.5f + block * 0.5f) / Px) * Px;
            float planksY = blockTop - lineCaps - DockLineGap - PlankHeight * 0.5f;
            confirm.anchoredPosition = new Vector2(confirm.anchoredPosition.x + dx, planksY);
            reset.anchoredPosition = new Vector2(reset.anchoredPosition.x + dx, planksY);
            a = Bounds(confirm, PlankHeight);
            b = Bounds(reset, PlankHeight);
            u = new Rect(a.x - (b.x - a.x), a.y, a.width, a.height);
            pointsText.alignment = TextAlignmentOptions.Top;
            label.anchoredPosition = new Vector2((left + right) * 0.5f, blockTop + 3f * Px - label.sizeDelta.y * (1f - label.pivot.y));
        }
        else
        {
            left = Mathf.Min(u.xMin, a.xMin, b.xMin) - 12f;
            right = Mathf.Max(a.xMax, b.xMax) + 12f;
            bottom = Mathf.Min(a.yMin, b.yMin) - 10f;
            label.anchoredPosition = new Vector2((left + right) * 0.5f, label.anchoredPosition.y);
            float rectTop = label.anchoredPosition.y + label.sizeDelta.y * (1f - label.pivot.y);
            top = rectTop - 3f * Px + 14f;
        }

        RectTransform dock = NewImage("Dock", parent, wood ? s.woodPanel : kit.panelBase, s.dock, !wood);
        dock.anchorMin = dock.anchorMax = confirm.anchorMin;
        dock.pivot = new Vector2(0.5f, 0.5f);
        dock.anchoredPosition = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);
        dock.sizeDelta = new Vector2(right - left, top - bottom);
        dock.SetSiblingIndex(Mathf.Min(confirm.GetSiblingIndex(), reset.GetSiblingIndex(), label.GetSiblingIndex()));
        if (wood)
        {
            Image dockImage = dock.GetComponent<Image>();
            dockImage.type = Image.Type.Tiled;
            dockImage.fillCenter = true;
            if (s.woodPanelTrim != null)
            {
                RectTransform dockTrim = NewImage("Trim", dock, s.woodPanelTrim, Color.white, false);
                dockTrim.GetComponent<Image>().type = Image.Type.Tiled;
                Stretch(dockTrim);
            }
        }
        else Stretch(NewImage("Shine", dock, kit.panelShine, Color.white, true));

        Plank(confirm, GameControlsStyle.Instance.start);
        Plank(reset, s.resetPlank);

        // Undo has no button in the scene: a plank of its own in the game controls' blue, faded while
        // there's nothing to undo
        PlankButton undo = PlankButton.Create((RectTransform)parent, "Undo", GameControlsStyle.Instance.speed, kit.font, OnUndo);
        RectTransform ur = undo.Rect;
        ur.anchorMin = ur.anchorMax = confirm.anchorMin;
        ur.pivot = new Vector2(0.5f, 0.5f);
        ur.anchoredPosition = u.center;
        ur.sizeDelta = new Vector2(Mathf.Max(ur.sizeDelta.x, u.width), ur.sizeDelta.y);
        undoGroup = undo.gameObject.AddComponent<CanvasGroup>();
        RefreshUndo();
    }

    private CanvasGroup undoGroup;

    // hooked to the Undo plank - un-stages the newest pick not yet confirmed
    public void OnUndo() => SkillTreeSession.UndoLast();

    private void RefreshUndo()
    {
        if (undoGroup == null) return;
        bool any = SkillTreeSession.HasPending;
        undoGroup.alpha = any ? 1f : 0.45f;
        undoGroup.interactable = any;
        undoGroup.blocksRaycasts = any;
    }

    // canvas units: the buttons' height (GameControlsStyle.buttonHeight art px)
    private static float PlankHeight => GameControlsStyle.Instance.buttonHeight * Px;

    // a button's footprint once it is a plank: its own centre and width, the plank's height
    private static Rect Bounds(RectTransform r, float height) =>
        new Rect(r.anchoredPosition.x - r.sizeDelta.x * 0.5f, r.anchoredPosition.y - height * 0.5f, r.sizeDelta.x, height);

    // the button's old look hides and a plank with the same word sits in its place, as wide as it was,
    // clicking the same button (so Confirm and Reset keep their wiring)
    private static void Plank(RectTransform buttonRect, GameControlsStyle.ButtonColors colors)
    {
        Button button = buttonRect.GetComponent<Button>();
        if (button == null || buttonRect.GetComponentInChildren<PlankButton>(true) != null) return;
        Image face = buttonRect.GetComponent<Image>();
        TMP_Text word = buttonRect.GetComponentInChildren<TMP_Text>(true);
        string text = word != null && !string.IsNullOrEmpty(word.text) ? word.text : buttonRect.name;
        if (face != null) face.enabled = false;
        if (word != null) word.gameObject.SetActive(false);
        PlankButton plank = PlankButton.Create(buttonRect, text, colors, InsectShowcaseStyle.Instance.font, () => button.onClick.Invoke());
        RectTransform r = plank.Rect;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(Mathf.Max(r.sizeDelta.x, Mathf.Round(buttonRect.sizeDelta.x / Px) * Px), r.sizeDelta.y);
    }

    private static void SkinScrollbar(Scrollbar bar)
    {
        if (bar == null) return;
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        SkillTreeStyle s = SkillTreeStyle.Instance;
        bar.transition = Selectable.Transition.None;
        SetKit(bar.GetComponent<Image>(), kit.stripSunken, s.scrollTrack, true);
        if (bar.handleRect != null) SetKit(bar.handleRect.GetComponent<Image>(), s.white, s.scrollHandle, false);
    }

    private static RectTransform NewImage(string name, Transform parent, Sprite sprite, Color color, bool sliced)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        SetKit(image, sprite, color, sliced);
        image.raycastTarget = false;
        return r;
    }

    private static void SetKit(Image image, Sprite sprite, Color color, bool sliced)
    {
        if (image == null) return;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 1f;
        }
        image.color = color;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public void OnBack()
    {
        if (SkillTreeSession.HasPending)
            Debug.Log("Skill Tree: leaving with unconfirmed picks - they will be lost.");
        if (SceneTransition.IsTransitioning) return;
        SceneTransition t = FindAnyObjectByType<SceneTransition>();
        if (t != null) t.StartCoroutine(t.FadeToScene("MainMenu"));
    }
}
