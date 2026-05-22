using UnityEngine;
using TMPro;

public class StatusEffectPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text effectsText;

    private Entity trackedEntity;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(Entity entity)
    {
        trackedEntity = entity;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        trackedEntity = null;
        gameObject.SetActive(false);
    }

    private const string Green  = "<color=green>";
    private const string Red    = "<color=red>";
    private const string Grey   = "<color=#888888>";
    private const string Pink   = "<color=#FFB6C1>";
    private const string End    = "</color>";

    public void Refresh()
    {
        if (trackedEntity == null || effectsText == null) return;

        var buffs   = trackedEntity.activeEffects.FindAll(ef => ef.effectType == StatusEffect.Type.positive);
        var debuffs = trackedEntity.activeEffects.FindAll(ef => ef.effectType == StatusEffect.Type.negative);
        var neutral = trackedEntity.activeEffects.FindAll(ef => ef.effectType == StatusEffect.Type.neutral);
        var primers = trackedEntity.activeEffects.FindAll(ef => ef.effectType == StatusEffect.Type.primer);

        bool hasAny = buffs.Count > 0 || debuffs.Count > 0 || neutral.Count > 0 || primers.Count > 0;
        gameObject.SetActive(hasAny);
        if (!hasAny) return;

        bool isInsect = trackedEntity is Insect;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<size=+8><color=white><b><u>Status Effects</u></b></color></size>");

        if (isInsect)
        {
            AppendSection(sb, "Debuffs", debuffs, Green);
            AppendSection(sb, "Buffs",   buffs,   Red);
        }
        else
        {
            AppendSection(sb, "Buffs",   buffs,   Green);
            AppendSection(sb, "Debuffs", debuffs, Red);
        }

        AppendSection(sb, "Neutral", neutral, Grey);
        AppendSection(sb, "Primers", primers, Pink);

        effectsText.text = sb.ToString().TrimEnd();
        Canvas.ForceUpdateCanvases();
        RectTransform panelRect = GetComponent<RectTransform>();
        float maxHeight = Screen.height * 0.85f;
        float height = Mathf.Min(effectsText.preferredHeight + 40f, maxHeight);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private static void AppendSection(System.Text.StringBuilder sb, string title, System.Collections.Generic.List<StatusEffect> effects, string color)
    {
        if (effects.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine($"<size=+4>{color}<b><u>{title}</u></b>{End}</size>");
        foreach (var ef in effects)
        {
            string duration = FormatDuration(ef.duration);
            sb.AppendLine($"<size=-2>({duration}) {color}<b>{ef.GetName()}</b>{End} [{color}<b>{ef.level}</b>{End}]</size>");
            string desc = ef.GetDescription();
            if (!string.IsNullOrEmpty(desc))
                sb.AppendLine($"<size=-2>{desc}</size>");
            sb.AppendLine();
        }
    }

    private static string FormatDuration(float seconds)
    {
        if (seconds >= float.MaxValue / 2f) return "--:--";
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:D2}:{total % 60:D2}";
    }
}
