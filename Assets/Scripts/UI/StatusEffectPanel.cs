using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class StatusEffectPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text effectsText;

    private Entity trackedEntity;

    // reused on every refresh so the lists and the builder are not made again each frame
    private readonly List<StatusEffect> buffs   = new List<StatusEffect>();
    private readonly List<StatusEffect> debuffs = new List<StatusEffect>();
    private readonly List<StatusEffect> neutral = new List<StatusEffect>();
    private readonly List<StatusEffect> primers = new List<StatusEffect>();
    private readonly StringBuilder sb = new StringBuilder(1024);

    // what the panel last showed: refresh runs every frame while the info panel is open, so the
    // text, the canvas update and the resize only run again when the text, the screen height or
    // the text's measured height changes. null text means the next refresh always does all of it
    private string shownText;
    private int shownScreenHeight;
    private float shownPreferredHeight;
    private RectTransform panelRect;

    private void Awake()
    {
        // Tommy's flower frame (2026-11-26)
        TooltipFrameLook.Apply(GetComponent<UnityEngine.UI.Image>());
        gameObject.SetActive(false);
    }

    public void Show(Entity entity)
    {
        trackedEntity = entity;
        shownText = null;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        trackedEntity = null;
        shownText = null;
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

        // one pass, same order as the list, instead of four new lists
        buffs.Clear();
        debuffs.Clear();
        neutral.Clear();
        primers.Clear();
        List<StatusEffect> effects = trackedEntity.activeEffects;
        for (int i = 0; i < effects.Count; i++)
        {
            StatusEffect ef = effects[i];
            if (!ef.visible) continue;
            switch (ef.effectType)
            {
                case StatusEffect.Type.positive: buffs.Add(ef);   break;
                case StatusEffect.Type.negative: debuffs.Add(ef); break;
                case StatusEffect.Type.neutral:  neutral.Add(ef); break;
                case StatusEffect.Type.primer:   primers.Add(ef); break;
            }
        }

        bool hasAny = buffs.Count > 0 || debuffs.Count > 0 || neutral.Count > 0 || primers.Count > 0;
        // a panel that was hidden and comes back always gets the full update
        if (!gameObject.activeSelf) shownText = null;
        gameObject.SetActive(hasAny);
        if (!hasAny) return;

        bool isInsect = trackedEntity is Insect;

        sb.Clear();
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

        // same as TrimEnd on the finished string
        int length = sb.Length;
        while (length > 0 && char.IsWhiteSpace(sb[length - 1])) length--;
        sb.Length = length;

        // nothing the panel shows changed: skip the text set, the canvas update and the resize.
        // the measured height is cached by the text and only measured again after the text, its
        // width or its active state changed, so it catches a panel being laid out again
        if (shownText != null && Screen.height == shownScreenHeight && Matches(sb, shownText)
            && effectsText.preferredHeight == shownPreferredHeight)
            return;

        shownText = sb.ToString();
        effectsText.text = shownText;
        Canvas.ForceUpdateCanvases();
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        shownScreenHeight = Screen.height;
        shownPreferredHeight = effectsText.preferredHeight;
        float maxHeight = shownScreenHeight * 0.85f;
        float height = Mathf.Min(shownPreferredHeight + 40f, maxHeight);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    // compares the builder with the shown string without making a new string
    private static bool Matches(StringBuilder builder, string text)
    {
        if (builder.Length != text.Length) return false;
        for (int i = 0; i < text.Length; i++)
            if (builder[i] != text[i]) return false;
        return true;
    }

    private static void AppendSection(StringBuilder sb, string title, List<StatusEffect> effects, string color)
    {
        if (effects.Count == 0) return;
        sb.AppendLine();
        sb.Append("<size=+4>").Append(color).Append("<b><u>").Append(title).Append("</u></b>").Append(End).AppendLine("</size>");
        foreach (var ef in effects)
        {
            sb.Append("<size=-2>(");
            AppendDuration(sb, ef.duration);
            sb.Append(") ").Append(color).Append("<b>").Append(ef.GetName()).Append("</b>").Append(End)
              .Append(" [").Append(color).Append("<b>").Append(ef.level).Append("</b>").Append(End).AppendLine("]</size>");
            string desc = ef.GetDescription();
            if (!string.IsNullOrEmpty(desc))
                sb.Append("<size=-2>").Append(desc).AppendLine("</size>");
            string sourceName = ef.source is Plant p ? p.GetName() : ef.source is Insect ins ? ins.GetName() : null;
            if (!string.IsNullOrEmpty(sourceName))
            {
                sb.AppendLine();
                sb.Append("<size=-2><color=#888888>Source: [</color>").Append(sourceName).AppendLine("<color=#888888>]</color></size>");
            }
            sb.AppendLine();
        }
    }

    // minutes and seconds, or the no end marker for an effect that never runs out
    private static void AppendDuration(StringBuilder sb, float seconds)
    {
        if (seconds >= float.MaxValue / 2f) { sb.Append("--:--"); return; }
        int total = Mathf.FloorToInt(seconds);
        AppendTwoDigits(sb, total / 60);
        sb.Append(':');
        AppendTwoDigits(sb, total % 60);
    }

    // the same text as value.ToString("D2"), without making a string for 0 to 99
    private static void AppendTwoDigits(StringBuilder sb, int value)
    {
        if (value >= 0 && value < 100)
            sb.Append((char)('0' + value / 10)).Append((char)('0' + value % 10));
        else
            sb.Append(value.ToString("D2"));
    }
}
