using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;

public class FertilizerCard : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private Image icon;
    [SerializeField] private Button selectButton;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject highlight;

    private FertilizerData data;
    private FertilizerStat[] rolledStats;
    private float[] rolledValues;
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

        Roll();
        StartCoroutine(AnimateIn());
    }

    private void Roll()
    {
        (rolledStats, rolledValues) = FertilizerManager.instance.RollFor(data);
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        // TODO: uncomment when FertilizerData icons are assigned
        // if (icon != null) icon.sprite = data.icon;
        if (tierText != null) tierText.text = data.fertilizerName;

        if (statsText != null)
        {
            var sb = new StringBuilder();

            bool hasElements  = data.targetElementalTypes != null && data.targetElementalTypes.Length > 0;
            bool hasFamilies  = data.targetFamilies        != null && data.targetFamilies.Length        > 0;

            if (data.appliesToAll)
            {
                sb.AppendLine("Applies to all plants");
            }
            else
            {
                if (hasElements)
                    sb.AppendLine($"Element: {string.Join(", ", data.targetElementalTypes)}");
                if (hasFamilies)
                    sb.AppendLine($"Family: {string.Join(", ", data.targetFamilies)}");
            }

            sb.AppendLine();

            for (int i = 0; i < rolledStats.Length; i++)
            {
                bool isGood = rolledValues[i] >= 0f;
                if (FertilizerFormat.IsInvertedStat(rolledStats[i].statType)) isGood = !isGood;
                string color = isGood ? "green" : "red";
                sb.AppendLine($"{FertilizerFormat.FormatStatName(rolledStats[i].statType)}: <color={color}><b>{FertilizerFormat.FormatValue(rolledStats[i].statType, rolledValues[i])}</b></color>");
            }
            statsText.text = sb.ToString().TrimEnd();
        }
    }

    public void OnSelectClicked()
    {
        FertilizerManager.instance.Commit(data, rolledStats, rolledValues);
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
