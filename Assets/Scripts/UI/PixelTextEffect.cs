using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

// a pixel exact shadow or outline for a TextMeshPro text: copies of the text drawn behind it, one
// art pixel off (a shadow: one copy straight below; an outline: eight copies all around). TMP's own
// underlay and outline are soft, so they never land on whole pixels. the copies are made at runtime
// as siblings just before the text (so they draw behind it) and follow its text, rect, colour alpha
// and active state. colour tags in the text are left out of the copies so they keep one colour.
// outline below: the outline plus one more pixel under it (the locked skill's price, round 6)
public class PixelTextEffect : MonoBehaviour
{
    public enum Mode { Shadow, Outline, OutlineBelow }

    [SerializeField] private Mode mode = Mode.Shadow;
    [SerializeField] private Color color = Color.black;
    [Tooltip("canvas units per art pixel (the plant info panel draws 1 art pixel as 2 units)")]
    [SerializeField] private float pixel = 2f;

    private static readonly Regex ColorTags = new Regex(@"</?color(=[^>]*)?>|<#[0-9a-fA-F]{3,8}>", RegexOptions.Compiled);
    private static readonly Vector2[] ShadowOffsets = { new Vector2(0f, -1f) };
    private static readonly Vector2[] OutlineOffsets =
    {
        new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
        new Vector2(-1f, 0f), new Vector2(1f, 0f),
        new Vector2(-1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
    };
    private static readonly Vector2[] OutlineBelowOffsets =
    {
        new Vector2(-1f, -2f), new Vector2(0f, -2f), new Vector2(1f, -2f),
        new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
        new Vector2(-1f, 0f), new Vector2(1f, 0f),
        new Vector2(-1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
    };

    private TMP_Text source;
    private TextMeshProUGUI[] copies;
    private Vector2[] offsets;
    private string lastText;

    public Color Color
    {
        get => color;
        set
        {
            if (color == value) return;
            color = value;
            ApplyColor();
        }
    }

    private void Awake()
    {
        source = GetComponent<TMP_Text>();
        if (source == null) return;
        offsets = mode == Mode.Shadow ? ShadowOffsets : mode == Mode.Outline ? OutlineOffsets : OutlineBelowOffsets;
        copies = new TextMeshProUGUI[offsets.Length];
        Transform parent = transform.parent;
        string copyName = $"{name} {(mode == Mode.Shadow ? "Shadow" : "Outline")}";
        DropClonedCopies(parent, copyName);
        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject go = new GameObject(copyName, typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(transform.GetSiblingIndex());
            TextMeshProUGUI copy = go.AddComponent<TextMeshProUGUI>();
            copy.raycastTarget = false;
            copies[i] = copy;
        }
        CopyStyle();
        Sync(true);
    }

    // a parent copied with Instantiate (the info panel's health bar, cloned for Rhodiola's golden sap bar) brings this
    // text's old copies along as plain texts nothing updates: the old number in the shadow colour, where the old rect
    // was. they sit right before the text under its copy name, and go before this one makes its own
    private void DropClonedCopies(Transform parent, string copyName)
    {
        if (parent == null) return;
        for (int i = transform.GetSiblingIndex() - 1, left = offsets.Length; i >= 0 && left > 0; i--, left--)
        {
            Transform sibling = parent.GetChild(i);
            if (sibling.name != copyName || sibling.GetComponent<TextMeshProUGUI>() == null) break;
            sibling.gameObject.SetActive(false);
            Destroy(sibling.gameObject);
        }
    }

    private void OnEnable()
    {
        if (copies == null) return;
        foreach (TextMeshProUGUI copy in copies)
            if (copy != null) copy.gameObject.SetActive(true);
        Sync(true);
    }

    private void OnDisable()
    {
        if (copies == null) return;
        foreach (TextMeshProUGUI copy in copies)
            if (copy != null) copy.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (copies == null) return;
        foreach (TextMeshProUGUI copy in copies)
            if (copy != null) Destroy(copy.gameObject);
    }

    private void LateUpdate() => Sync(false);

    // font, size and layout settings, copied once (and again if the font size ever changes)
    private void CopyStyle()
    {
        foreach (TextMeshProUGUI copy in copies)
        {
            copy.font = source.font;
            copy.fontSharedMaterial = source.fontSharedMaterial;
            copy.fontSize = source.fontSize;
            copy.fontStyle = source.fontStyle;
            copy.alignment = source.alignment;
            copy.textWrappingMode = source.textWrappingMode;
            copy.overflowMode = source.overflowMode;
            copy.richText = source.richText;
            copy.characterSpacing = source.characterSpacing;
            copy.wordSpacing = source.wordSpacing;
            copy.lineSpacing = source.lineSpacing;
            copy.margin = source.margin;
        }
    }

    private void Sync(bool force)
    {
        if (source == null || copies == null) return;
        if (copies[0] != null && copies[0].fontSize != source.fontSize) CopyStyle();
        string text = source.text;
        if (force || text != lastText)
        {
            lastText = text;
            string plain = string.IsNullOrEmpty(text) ? text : ColorTags.Replace(text, "");
            foreach (TextMeshProUGUI copy in copies) copy.text = plain;
        }

        // the copies' rects are only written when the text's own rect changed since the last sync
        RectTransform from = source.rectTransform;
        Vector2 anchorMin = from.anchorMin, anchorMax = from.anchorMax, pivot = from.pivot;
        Vector2 sizeDelta = from.sizeDelta, anchoredPosition = from.anchoredPosition;
        Vector3 localScale = from.localScale;
        if (force || !rectSynced || anchorMin != syncedAnchorMin || anchorMax != syncedAnchorMax || pivot != syncedPivot
            || sizeDelta != syncedSizeDelta || anchoredPosition != syncedAnchoredPosition || localScale != syncedLocalScale)
        {
            rectSynced = true;
            syncedAnchorMin = anchorMin;
            syncedAnchorMax = anchorMax;
            syncedPivot = pivot;
            syncedSizeDelta = sizeDelta;
            syncedAnchoredPosition = anchoredPosition;
            syncedLocalScale = localScale;
            for (int i = 0; i < copies.Length; i++)
            {
                RectTransform to = copies[i].rectTransform;
                to.anchorMin = anchorMin;
                to.anchorMax = anchorMax;
                to.pivot = pivot;
                to.sizeDelta = sizeDelta;
                to.anchoredPosition = anchoredPosition + offsets[i] * pixel;
                to.localScale = localScale;
            }
        }
        foreach (TextMeshProUGUI copy in copies) copy.enabled = source.enabled;
        ApplyColor();
    }

    // the text's rect as the copies were last given it
    private bool rectSynced;
    private Vector2 syncedAnchorMin, syncedAnchorMax, syncedPivot, syncedSizeDelta, syncedAnchoredPosition;
    private Vector3 syncedLocalScale;

    private void ApplyColor()
    {
        if (copies == null || source == null) return;
        Color c = color;
        c.a *= source.color.a;
        foreach (TextMeshProUGUI copy in copies)
            if (copy != null) copy.color = c;
    }
}
