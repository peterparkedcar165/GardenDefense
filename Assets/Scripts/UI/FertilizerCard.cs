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
        if (icon != null) icon.sprite = data.icon;
        if (tierText != null) tierText.text = data.fertilizerName;

        if (statsText != null)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rolledStats.Length; i++)
            {
                string valueStr = IsRawValue(rolledStats[i].statType)
                    ? $"+{Mathf.RoundToInt(rolledValues[i])}"
                    : $"+{rolledValues[i] * 100f:F0}%";
                sb.AppendLine($"{FormatStatName(rolledStats[i].statType)}: <color=green><b>{valueStr}</b></color>");
            }
            statsText.text = sb.ToString();
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

    private bool IsRawValue(StatType statType)
    {
        switch (statType)
        {
            case StatType.Piercing:
            case StatType.ImmobilizeDurationAdder:
            case StatType.SkillDurationAdder:
            case StatType.IlluminationRangeAdder:
                return true;
            default:
                return false;
        }
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
            case StatType.SkillCooldown:  return "Skill Cd. Reduction";
            case StatType.DoTDamage:      return "DoT Damage";
            case StatType.Piercing:                     return "Piercing";
            case StatType.ImmobilizeDurationAdder:      return "Immobilize Duration";
            case StatType.ImmobilizeDurationMultiplier: return "Immobilize Duration";
            case StatType.PassiveCooldown:              return "Passive Cd. Reduction";
            case StatType.SkillDurationAdder:           return "Skill Duration";
            case StatType.SkillDurationMultiplier:      return "Skill Duration";
            case StatType.CoordinatedDamage:            return "Coordinated Damage";
            case StatType.HealingBonus:                 return "Healing Bonus";
            case StatType.IlluminationRangeAdder:       return "Illumination Range";
            case StatType.IlluminationRangeMultiplier:  return "Illumination Range";
            default:                      return statType.ToString();
        }
    }
}
