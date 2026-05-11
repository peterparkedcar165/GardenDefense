using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;

public class FertilizerCard : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private Image icon;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button rerollButton;
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

        targetPosition = rectTransform.anchoredPosition;

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
        if (icon != null) icon.sprite = data.icon;
        if (descriptionText != null) descriptionText.text = data.description;
        if (tierText != null) tierText.text = data.tier.ToString();

        if (statsText != null)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rolledStats.Length; i++)
                sb.AppendLine($"{FormatStatName(rolledStats[i].statType)}: <color=green><b>+{rolledValues[i]:F1}</b></color>");
            statsText.text = sb.ToString();
        }
    }

    public void OnSelectClicked()
    {
        ui.SelectCard(this);
    }

    public void OnRerollClicked()
    {
        if (!ui.TryReroll(this)) return;
        Roll();
    }

    public void Commit()
    {
        FertilizerManager.instance.Commit(data, rolledStats, rolledValues);
    }

    public void SetHighlight(bool active)
    {
        highlight.SetActive(active);
    }

    private IEnumerator AnimateIn()
    {
        Vector2 startPos = targetPosition + Vector2.up * Screen.height;
        rectTransform.anchoredPosition = startPos;

        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f); // ease out cubic
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        canvasGroup.interactable = true;
    }

    private string FormatStatName(StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackDamage:   return "Attack Damage";
            case StatType.AttackSpeed:    return "Attack Speed";
            case StatType.AttackRange:    return "Attack Range";
            case StatType.FireDamage:     return "Fire Damage";
            case StatType.IceDamage:      return "Ice Damage";
            case StatType.WaterDamage:    return "Water Damage";
            case StatType.NatureDamage:   return "Nature Damage";
            case StatType.PoisonDamage:   return "Poison Damage";
            case StatType.WindDamage:     return "Wind Damage";
            case StatType.CriticalChance: return "Critical Chance";
            case StatType.CriticalDamage: return "Critical Damage";
            case StatType.ElementalPower: return "Elemental Power";
            case StatType.PassiveDamage:  return "Passive Damage";
            case StatType.SkillDamage:    return "Skill Damage";
            case StatType.SkillCooldown:  return "Skill Cooldown";
            case StatType.DoTDamage:      return "DoT Damage";
            default:                      return statType.ToString();
        }
    }
}
