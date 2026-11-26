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
    private Vector2 targetPosition;

    public void Initialize(FertilizerData fertilizer, FertilizerSelectionUI selectionUI)
    {
        data = fertilizer;
        ui = selectionUI;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;
        highlight.SetActive(false);
        if (rerollButton != null) rerollButton.gameObject.SetActive(false);

        RefreshDisplay();
        StartCoroutine(AnimateIn());
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
    // (402 by 471), its greyscale frame and parchment tinted with the fertilizer's colour and its gold
    // ornaments over it as drawn. the icon, the name, a seam and the description in a dark inset box fit
    // its parchment (art px 14 to 119 across, 14 to 144 down); the name and the description shrink to fit.
    // the pointer's highlight takes the packet's shape
    private const float PacketPx = 3f;
    private static readonly Vector2 PacketArt = new Vector2(134f, 157f);
    private static readonly Rect Parchment = Rect.MinMaxRect(14f, 14f, 120f, 145f);
    // canvas units between the parchment's edge and what's on it
    private const float PacketPad = 12f;

    private void SkinAsPacket(Transform body, Image bodyImage, Color color, InsectShowcaseStyle s)
    {
        RectTransform bodyRect = (RectTransform)body;
        Vector2 size = PacketArt * PacketPx;
        bodyRect.sizeDelta = size;
        SetKit(bodyImage, s.seedPacketBase, color, false);
        bodyImage.preserveAspect = false;
        if (s.seedPacketTrim != null)
        {
            RectTransform trim = AddKitImage(body, "Trim", s.seedPacketTrim, Color.white, false);
            trim.anchorMin = Vector2.zero;
            trim.anchorMax = Vector2.one;
            trim.offsetMin = trim.offsetMax = Vector2.zero;
            trim.SetSiblingIndex(0);
        }

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

        float boxTop = y, boxBottom = bottom + PacketPad;
        RectTransform stats = statsText.rectTransform;
        stats.anchorMin = stats.anchorMax = stats.pivot = new Vector2(0.5f, 0.5f);
        stats.sizeDelta = new Vector2(inner - 24f, boxTop - boxBottom - 12f);
        stats.anchoredPosition = new Vector2(centreX, Mathf.Round((boxTop + boxBottom) * 0.5f));
        statsText.enableAutoSizing = true;
        statsText.fontSizeMax = statsText.fontSize;
        statsText.fontSizeMin = 12f;
        RectTransform inset = AddKitImage(body, "Inset", s.insetBox, color, true);
        inset.anchorMin = inset.anchorMax = inset.pivot = new Vector2(0.5f, 0.5f);
        inset.sizeDelta = new Vector2(inner, boxTop - boxBottom);
        inset.anchoredPosition = stats.anchoredPosition;
        inset.SetSiblingIndex(stats.GetSiblingIndex());

        // the pointer's highlight in the packet's shape, the packet's size
        Image glow = highlight != null ? highlight.GetComponent<Image>() : null;
        if (glow != null)
        {
            glow.sprite = s.seedPacketBase;
            glow.type = Image.Type.Simple;
            glow.preserveAspect = false;
            glow.rectTransform.sizeDelta = size;
            glow.rectTransform.anchoredPosition = bodyRect.anchoredPosition;
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

    private IEnumerator AnimateIn()
    {
        yield return null;
        targetPosition = rectTransform.anchoredPosition;
        Vector2 startPos = targetPosition + Vector2.up * Screen.height;
        rectTransform.anchoredPosition = startPos;

        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        canvasGroup.interactable = true;
    }
}
