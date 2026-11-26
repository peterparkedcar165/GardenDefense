using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// one fertilizer on the pre level pick screen: its name on top and what it does below.
// the description is built from the fertilizer's own numbers (FertilizerData.GetDescription)
public class FertilizerCard : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private Image icon;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject highlight;
    // unused since fertilizers stopped rolling random stats, kept so the card prefab's reference stays valid
    [SerializeField] private Button rerollButton;

    private FertilizerData data;
    private FertilizerSelectionUI ui;
    private RectTransform rectTransform;

    // fallDelay: real seconds it waits above the screen before falling (the screen's title falls first, then
    // the cards left to right, Tommy 2026-11-26)
    public void Initialize(FertilizerData fertilizer, FertilizerSelectionUI selectionUI, float fallDelay = 0f)
    {
        data = fertilizer;
        ui = selectionUI;
        this.fallDelay = fallDelay;
        rectTransform = GetComponent<RectTransform>();
        // not clickable until it's face up, but in its clickable look (Tommy: switching the button off greyed
        // it): the pointer passes through it instead
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = false;
        // hidden until it's above the screen (its first frame is spent where the row lays it out)
        canvasGroup.alpha = 0f;
        highlight.SetActive(false);
        if (rerollButton != null) rerollButton.gameObject.SetActive(false);

        RefreshDisplay();
        // face down while it falls (Tommy, 2026-11-26): turned over by FertilizerSelectionUI once every card
        // has landed
        ShowFace(false);
        StartCoroutine(AnimateIn());
    }

    // the drop (Tommy, 2026-11-26): like a card slid across a table, fast and gliding to a stop, no squash.
    // real seconds; the screen's title slides in the same way (FertilizerSelectionUI.DropTitle)
    public const float FallSeconds = 0.35f;

    // the slide's progress for t from 0 to 1: fast at first, easing to a stop
    public static float Fall(float t) => 1f - Mathf.Pow(1f - t, 4f);
    // each half of the turn over: narrowing to nothing, then widening back with its face up
    private const float FlipHalfSeconds = 0.14f;

    public bool Landed { get; private set; }
    private bool faceUp = true;

    // face up: the fertilizer's icon, its name, the seam, the description and its box; face down: the bare
    // packet (its interior and frame)
    private void ShowFace(bool up)
    {
        faceUp = up;
        Transform body = statsText != null ? statsText.transform.parent : null;
        if (icon != null) icon.gameObject.SetActive(up);
        if (tierText != null) tierText.gameObject.SetActive(up);
        if (statsText != null) statsText.gameObject.SetActive(up);
        if (body != null)
            foreach (string part in new[] { "Seam", "Inset" })
            {
                Transform found = body.Find(part);
                if (found != null) found.gameObject.SetActive(up);
            }
        if (!up && highlight != null) highlight.SetActive(false);
    }

    // turned over where it lies: narrowed to nothing, its face shown, widened back, then it can be picked.
    // real time (the game is paused on this screen)
    public void Flip()
    {
        if (!faceUp && isActiveAndEnabled) StartCoroutine(FlipRoutine());
    }

    private IEnumerator FlipRoutine()
    {
        for (float t = 0f; t < FlipHalfSeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / FlipHalfSeconds;
            rectTransform.localScale = new Vector3(1f - k * k, 1f, 1f);
            yield return null;
        }
        rectTransform.localScale = new Vector3(0f, 1f, 1f);
        ShowFace(true);
        for (float t = 0f; t < FlipHalfSeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / FlipHalfSeconds;
            rectTransform.localScale = new Vector3(1f - (1f - k) * (1f - k), 1f, 1f);
            yield return null;
        }
        rectTransform.localScale = Vector3.one;
        canvasGroup.blocksRaycasts = true;
    }

    private void RefreshDisplay()
    {
        if (icon != null)
        {
            // the mulches' paper bag with the symbol of the fertilizer's kind, 4 canvas units per art pixel
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
            MulchIcon.ShowFertilizer(icon, data);
        }
        if (tierText != null) tierText.text = data.fertilizerName;
        if (statsText != null)
            statsText.text = data.GetDescription();
        Skin();
    }

    private const float IconSize = 128f;
    // a neutral fertilizer's card colour (proposal): an earthy brown next to the element colours
    private static readonly Color NeutralCardColor = new Color32(0x7A, 0x52, 0x30, 0xFF);
    private bool skinned;

    // the card in Tommy's panel kit (2026-11-24, like the loadout screen's panels): panel_base and its
    // shine for the body, tinted with the fertilizer's element colour (the plant info panel's), a seam
    // under the name and the description in a dark inset box
    private void Skin()
    {
        if (skinned || statsText == null || tierText == null) return;
        Transform body = statsText.transform.parent;
        Image bodyImage = body != null ? body.GetComponent<Image>() : null;
        if (bodyImage == null) return;
        skinned = true;
        InsectShowcaseStyle s = InsectShowcaseStyle.Instance;
        Color color = data.elementalType == ElementalType.Neutral ? NeutralCardColor : PlantData.ElementPanelColor(data.elementalType);
        if (s.seedPacketBase != null) { SkinAsPacket(body, bodyImage, color, s); return; }

        SetKit(bodyImage, s.panelBase, color, true);
        RectTransform shine = AddKitImage(body, "Shine", s.panelShine, Color.white, true);
        shine.anchorMin = Vector2.zero;
        shine.anchorMax = Vector2.one;
        shine.offsetMin = shine.offsetMax = Vector2.zero;
        shine.SetSiblingIndex(0);

        // the seam under the name
        RectTransform name = tierText.rectTransform;
        RectTransform seam = AddKitImage(body, "Seam", s.seam, Color.white, false);
        seam.anchorMin = name.anchorMin;
        seam.anchorMax = name.anchorMax;
        seam.pivot = new Vector2(0.5f, 0.5f);
        seam.sizeDelta = new Vector2(name.sizeDelta.x + 24f, 4f);
        seam.anchoredPosition = new Vector2(name.anchoredPosition.x, Mathf.Round(name.anchoredPosition.y - name.sizeDelta.y * 0.5f - 4f));

        // the description in a dark inset box, drawn behind it: exactly as wide as the seam and under it
        // (Tommy), the text 12 in from each side and 6 from the top and bottom
        RectTransform stats = statsText.rectTransform;
        float width = seam.sizeDelta.x;
        stats.sizeDelta = new Vector2(width - 24f, stats.sizeDelta.y);
        stats.anchoredPosition = new Vector2(seam.anchoredPosition.x, stats.anchoredPosition.y);
        RectTransform inset = AddKitImage(body, "Inset", s.insetBox, color, true);
        inset.anchorMin = stats.anchorMin;
        inset.anchorMax = stats.anchorMax;
        inset.pivot = stats.pivot;
        inset.anchoredPosition = stats.anchoredPosition;
        inset.sizeDelta = new Vector2(width, stats.sizeDelta.y + 12f);
        inset.SetSiblingIndex(stats.GetSiblingIndex());
    }

    // Tommy's seed packet (2026-11-26): the card's body is the packet at 3 canvas units an art pixel
    // (402 by 471), drawn in three layers (Tommy): its greyscale interior (seed_packet_interior) at the
    // back, tinted with the fertilizer's colour, then the card's contents, then its frame
    // (seed_packet_frame: the wood, gold corners and vines, as drawn) over everything. the icon, the name,
    // a seam and the description in a dark inset box stay where the frame's vines never reach (art px 21
    // to 112 across, 18 to 140 down); the name and the description shrink to fit. the pointer's highlight
    // takes the interior's shape
    private const float PacketPx = 3f;
    private static readonly Vector2 PacketArt = new Vector2(134f, 157f);
    private static readonly Rect Parchment = Rect.MinMaxRect(21f, 18f, 113f, 141f);
    // canvas units between that area's edge and what's on it
    private const float PacketPad = 6f;

    private void SkinAsPacket(Transform body, Image bodyImage, Color color, InsectShowcaseStyle s)
    {
        RectTransform bodyRect = (RectTransform)body;
        Vector2 size = PacketArt * PacketPx;
        bodyRect.sizeDelta = size;
        SetKit(bodyImage, s.seedPacketBase, color, false);
        bodyImage.preserveAspect = false;

        // the parchment in the body's own units (its middle at 0, 0)
        float left = -size.x * 0.5f + Parchment.xMin * PacketPx;
        float right = -size.x * 0.5f + Parchment.xMax * PacketPx;
        float top = size.y * 0.5f - Parchment.yMin * PacketPx;
        float bottom = size.y * 0.5f - Parchment.yMax * PacketPx;
        float inner = right - left - 2f * PacketPad;
        float centreX = Mathf.Round((left + right) * 0.5f);

        float y = top - PacketPad;
        if (icon != null)
        {
            RectTransform ir = icon.rectTransform;
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0.5f, 0.5f);
            ir.anchoredPosition = new Vector2(centreX, Mathf.Round(y - IconSize * 0.5f));
            y -= IconSize + 6f;
        }

        RectTransform name = tierText.rectTransform;
        name.anchorMin = name.anchorMax = name.pivot = new Vector2(0.5f, 0.5f);
        name.sizeDelta = new Vector2(inner - 24f, 42f);
        name.anchoredPosition = new Vector2(centreX, Mathf.Round(y - 21f));
        tierText.enableAutoSizing = true;
        tierText.fontSizeMax = tierText.fontSize;
        tierText.fontSizeMin = 18f;
        y -= 42f + 4f;

        RectTransform seam = AddKitImage(body, "Seam", s.seam, Color.white, false);
        seam.anchorMin = seam.anchorMax = seam.pivot = new Vector2(0.5f, 0.5f);
        seam.sizeDelta = new Vector2(inner, 4f);
        seam.anchoredPosition = new Vector2(centreX, Mathf.Round(y - 2f));
        y -= 4f + 8f;

        // the description in its box, in Tommy's section frame (SectionFrameLook, 2026-11-26) when there is
        // one, the text clear of it
        float boxTop = y, boxBottom = bottom + PacketPad;
        bool framed = SectionFrameLook.Available;
        float sidePad = framed ? SectionFrameLook.Inset : 12f;
        float endPad = framed ? SectionFrameLook.Inset - 2f : 6f;
        RectTransform stats = statsText.rectTransform;
        stats.anchorMin = stats.anchorMax = stats.pivot = new Vector2(0.5f, 0.5f);
        stats.sizeDelta = new Vector2(inner - 2f * sidePad, boxTop - boxBottom - 2f * endPad);
        stats.anchoredPosition = new Vector2(centreX, Mathf.Round((boxTop + boxBottom) * 0.5f));
        statsText.enableAutoSizing = true;
        statsText.fontSizeMax = statsText.fontSize;
        statsText.fontSizeMin = 12f;
        RectTransform inset = AddKitImage(body, "Inset", s.insetBox, color, true);
        inset.anchorMin = inset.anchorMax = inset.pivot = new Vector2(0.5f, 0.5f);
        inset.sizeDelta = new Vector2(inner, boxTop - boxBottom);
        inset.anchoredPosition = stats.anchoredPosition;
        inset.SetSiblingIndex(stats.GetSiblingIndex());
        SectionFrameLook.Add(inset.GetComponent<Image>());

        // the frame last, over the contents
        if (s.seedPacketTrim != null)
        {
            RectTransform frame = AddKitImage(body, "Frame", s.seedPacketTrim, Color.white, false);
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.offsetMin = frame.offsetMax = Vector2.zero;
            frame.SetAsLastSibling();
        }

        // the pointer's highlight in the packet's shape, the packet's size: the interior's shape with the
        // frame's over it in the same light (the two never overlap, so no pixel is lit twice)
        Image glow = highlight != null ? highlight.GetComponent<Image>() : null;
        if (glow != null)
        {
            glow.sprite = s.seedPacketBase;
            glow.type = Image.Type.Simple;
            glow.preserveAspect = false;
            glow.rectTransform.sizeDelta = size;
            glow.rectTransform.anchoredPosition = bodyRect.anchoredPosition;
            if (s.seedPacketTrim != null)
            {
                RectTransform glowFrame = AddKitImage(glow.transform, "Frame", s.seedPacketTrim, glow.color, false);
                glowFrame.anchorMin = Vector2.zero;
                glowFrame.anchorMax = Vector2.one;
                glowFrame.offsetMin = glowFrame.offsetMax = Vector2.zero;
            }
        }
    }

    private static RectTransform AddKitImage(Transform parent, string name, Sprite sprite, Color color, bool sliced)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        SetKit(image, sprite, color, sliced);
        image.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    private static void SetKit(Image image, Sprite sprite, Color color, bool sliced)
    {
        image.sprite = sprite;
        image.type = sliced && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = color;
    }

    public void OnSelectClicked()
    {
        TutorialManager.instance?.NotifyEvent(TutorialEvents.SelectFertilizer);
        FertilizerManager.instance?.Commit(data);
        ui.CloseAfterSelect();
    }

    // the prefab's hidden reroll button still points here
    public void OnRerollClicked() {}

    public void SetHighlight(bool active)
    {
        highlight.SetActive(active);
    }

    private float fallDelay;

    // the card's own rect belongs to the row's layout, which can lay it out again a frame after the screen
    // first opens and snapped it back into the middle mid slide (Tommy saw the cards there for a frame). so
    // the card stays put and its packet and highlight slide instead, from a canvas height above
    private IEnumerator AnimateIn()
    {
        RectTransform body = statsText != null ? statsText.transform.parent as RectTransform : null;
        RectTransform glow = highlight != null ? highlight.transform as RectTransform : null;
        Vector2 bodyHome = body != null ? body.anchoredPosition : Vector2.zero;
        Vector2 glowHome = glow != null ? glow.anchoredPosition : Vector2.zero;
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform root = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        Vector2 lift = Vector2.up * (root != null ? root.rect.height : Screen.height);
        Place(body, bodyHome + lift);
        Place(glow, glowHome + lift);
        yield return null;
        canvasGroup.alpha = 1f;
        if (fallDelay > 0f) yield return new WaitForSecondsRealtime(fallDelay);
        UISounds.Slide();

        for (float elapsed = 0f; elapsed < FallSeconds; elapsed += Time.unscaledDeltaTime)
        {
            Vector2 offset = lift * (1f - Fall(elapsed / FallSeconds));
            Place(body, bodyHome + offset);
            Place(glow, glowHome + offset);
            yield return null;
        }
        Place(body, bodyHome);
        Place(glow, glowHome);
        // picked only once it's been turned face up (Flip)
        Landed = true;
    }

    private static void Place(RectTransform r, Vector2 position)
    {
        if (r != null) r.anchoredPosition = position;
    }
}
