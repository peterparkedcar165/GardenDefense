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
        if (icon != null && data.icon != null) icon.sprite = data.icon;
        if (tierText != null) tierText.text = data.fertilizerName;
        if (statsText != null)
            statsText.text = data.GetDescription();
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
