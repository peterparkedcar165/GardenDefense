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
