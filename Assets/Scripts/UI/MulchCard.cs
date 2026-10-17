using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// mulch counterpart to FertilizerCard - same card, but there is no stat roll: the card just
// displays the drafted MulchData's own name/description/tier color directly. reroll swaps out
// for a new eligible MulchData of the same tier (see MulchManager.RerollSingle)
public class MulchCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Button selectButton;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject highlight;
    [SerializeField] private Button rerollButton;

    private MulchData data;
    private MulchSelectionUI ui;
    private RectTransform rectTransform;
    private Vector2 targetPosition;

    public void Initialize(MulchData mulch, MulchSelectionUI selectionUI)
    {
        data = mulch;
        ui = selectionUI;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;
        highlight.SetActive(false);
        if (rerollButton != null)
        {
            rerollButton.gameObject.SetActive(true);
            rerollButton.interactable = true;
        }

        RefreshDisplay();
        StartCoroutine(AnimateIn());
    }

    // rerolls just this card at the same tier, independent of the other cards and of the queue
    // itself - nothing is consumed by a reroll, only by an actual selection. wired via the
    // Reroll button's OnClick() in the Inspector, same as OnSelectClicked. limited to a single
    // use per card - disabled immediately after firing
    public void OnRerollClicked()
    {
        MulchData rerolled = MulchManager.instance.RerollSingle(data.tier);
        if (rerolled != null) data = rerolled;
        RefreshDisplay();
        if (rerollButton != null) rerollButton.interactable = false;
    }

    private void RefreshDisplay()
    {
        if (icon != null) icon.sprite = data.icon;
        if (background != null) background.color = data.color;
        if (nameText != null) nameText.text = data.mulchName;
        if (descriptionText != null) descriptionText.text = data.description;
    }

    public void OnSelectClicked()
    {
        MulchManager.instance.CommitChosen(data);
        ui.CloseAfterSelect();
    }

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
