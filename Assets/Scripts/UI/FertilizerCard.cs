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
    [SerializeField] private Button rerollButton;

    private FertilizerData data;
    private FertilizerStat[] rolledStats;
    private float[] rolledValues;
    private FertilizerSelectionUI ui;
    private RectTransform rectTransform;
    private Vector2 targetPosition;

    // mid-level ("generated") mode - a procedurally rolled bundle rather than a hand-authored
    // FertilizerData asset. isGeneratedMode picks which branch RefreshDisplay/OnSelectClicked use
    private bool isGeneratedMode;
    private GeneratedFertilizer generatedData;

    public void Initialize(FertilizerData fertilizer, FertilizerSelectionUI selectionUI)
    {
        isGeneratedMode = false;
        data = fertilizer;
        ui = selectionUI;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;
        highlight.SetActive(false);
        // reroll only ever applies to mid-level procedural choices, never the pre-level pool pick
        if (rerollButton != null) rerollButton.gameObject.SetActive(false);

        Roll();
        StartCoroutine(AnimateIn());
    }

    public void InitializeGenerated(GeneratedFertilizer fertilizer, FertilizerSelectionUI selectionUI)
    {
        isGeneratedMode = true;
        generatedData = fertilizer;
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

        RefreshDisplayGenerated();
        StartCoroutine(AnimateIn());
    }

    // rerolls just this card's bundle at the same tier, independent of the other 2 cards and of
    // the queue itself - nothing is consumed by a reroll, only by an actual selection.
    // wired via the Reroll button's OnClick() in the Inspector, same as OnSelectClicked.
    // limited to a single use per card - disabled immediately after firing
    public void OnRerollClicked()
    {
        generatedData = FertilizerManager.instance.RerollSingle(generatedData.tier);
        RefreshDisplayGenerated();
        if (rerollButton != null) rerollButton.interactable = false;
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

    // tier name + rolled stat list for a mid-level procedural bundle - no element/family
    // targeting to show, since GeneratedFertilizerStat's scope isn't a single fertilizer-wide tag
    private void RefreshDisplayGenerated()
    {
        if (tierText != null) tierText.text = generatedData.tier.ToString();

        if (statsText != null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (GeneratedFertilizerStat stat in generatedData.stats)
            {
                bool isGood = stat.value >= 0f;
                if (FertilizerFormat.IsInvertedStat(stat.statType)) isGood = !isGood;
                string color = isGood ? "green" : "red";
                sb.AppendLine($"{FertilizerFormat.FormatStatName(stat.statType)}: <color={color}><b>{FertilizerFormat.FormatValue(stat.statType, stat.value)}</b></color>");
            }
            statsText.text = sb.ToString().TrimEnd();
        }
    }

    public void OnSelectClicked()
    {
        if (isGeneratedMode)
        {
            FertilizerManager.instance.CommitGenerated(generatedData);
            ui.CloseAfterSelect();
            return;
        }
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
