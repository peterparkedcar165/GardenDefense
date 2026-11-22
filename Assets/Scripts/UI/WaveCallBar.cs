using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// the wave timer bar under the game controls, made a button (Tommy, 2026-11-21): clicking it calls the
// next wave early (ProceduralLevel.CallNextWave), the way Kingdom Rush does, and the wave already running
// keeps playing. the call opens at half time or once the field is clear; while it's open the bar's fill
// pulses, and it lights up under the pointer. hovering shows what calling pays, or when it opens.
// GameHUD attaches it at runtime to the bar holding the countdown fill
public class WaveCallBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private Image[] fills;
    private Color[] fillColors;
    private RectTransform tip;
    private TextMeshProUGUI tipText;
    private bool hovered, pressed;
    // after a call, the tooltip says what it paid for a moment (real time)
    private string paidText;
    private float paidUntil;

    public static WaveCallBar Attach(RectTransform bar, Image fill, Image shine, TMP_Text styleFrom)
    {
        if (bar == null) return null;
        WaveCallBar call = bar.GetComponent<WaveCallBar>();
        if (call == null) call = bar.gameObject.AddComponent<WaveCallBar>();
        call.Build(fill, shine, styleFrom);
        return call;
    }

    private void Build(Image fill, Image shine, TMP_Text styleFrom)
    {
        // the bar's frame catches the pointer; the texts and fill inside it pass it on
        Image frame = GetComponent<Image>();
        if (frame != null) frame.raycastTarget = true;
        fills = new[] { fill, shine };
        fillColors = new Color[fills.Length];
        for (int i = 0; i < fills.Length; i++)
            if (fills[i] != null) fillColors[i] = fills[i].color;

        if (tip != null) return;
        GameObject tipGO = new GameObject("WaveCallTip", typeof(RectTransform));
        tipGO.transform.SetParent(transform, false);
        tip = (RectTransform)tipGO.transform;
        tip.anchorMin = tip.anchorMax = new Vector2(0.5f, 1f);
        tip.pivot = new Vector2(0.5f, 0f);
        tip.anchoredPosition = new Vector2(0f, 8f);
        Image back = tipGO.AddComponent<Image>();
        back.color = new Color(0.08f, 0.09f, 0.08f, 0.92f);
        back.raycastTarget = false;
        HorizontalLayoutGroup pad = tipGO.AddComponent<HorizontalLayoutGroup>();
        pad.padding = new RectOffset(12, 12, 8, 8);
        pad.childControlWidth = pad.childControlHeight = true;
        pad.childForceExpandWidth = pad.childForceExpandHeight = false;
        ContentSizeFitter fit = tipGO.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(tip, false);
        tipText = textGO.AddComponent<TextMeshProUGUI>();
        if (styleFrom != null)
        {
            tipText.font = styleFrom.font;
            tipText.fontSize = styleFrom.fontSize;
        }
        tipText.color = Color.white;
        tipText.alignment = TextAlignmentOptions.Center;
        tipText.textWrappingMode = TextWrappingModes.NoWrap;
        tipText.raycastTarget = false;
        tipGO.SetActive(false);
    }

    private void OnDisable()
    {
        hovered = pressed = false;
        if (tip != null) tip.gameObject.SetActive(false);
        Tint(0f);
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) pressed = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        ProceduralLevel level = ProceduralLevel.Instance;
        if (level == null) return;
        int sun = level.CallSunReward;
        float cut = level.CallCooldownCut;
        int calling = level.NextWaveNumber;
        if (!level.CallNextWave()) return;
        if (GameManager.instance != null && GameManager.instance.plantSelect != null)
            GameManager.instance.PlaySound(GameManager.instance.plantSelect);
        paidText = $"<b>Wave {calling} called</b>\n<color=#FFD700>+{sun} Sun</color>, skills {Mathf.FloorToInt(cut)}s faster";
        paidUntil = Time.unscaledTime + 1.6f;
    }

    private void Update()
    {
        ProceduralLevel level = ProceduralLevel.Instance;
        bool open = level != null && level.CanCallNextWave;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        Tint(!open ? 0f : pressed ? 0.5f : hovered ? 0.35f : 0.1f + 0.15f * pulse);

        string text = TipText(level, open);
        bool show = text != null;
        if (tip != null && tip.gameObject.activeSelf != show) tip.gameObject.SetActive(show);
        if (show && tipText.text != text) tipText.text = text;
    }

    private string TipText(ProceduralLevel level, bool open)
    {
        if (paidText != null && Time.unscaledTime < paidUntil) return paidText;
        paidText = null;
        if (!hovered || level == null || !level.HasNextWave) return null;
        if (open)
            return $"<b>Call wave {level.NextWaveNumber} now</b>\n<color=#FFD700>+{level.CallSunReward} Sun</color>, " +
                $"skills {Mathf.FloorToInt(level.CallCooldownCut)}s faster";
        return $"<b>Call wave {level.NextWaveNumber} early</b>\nOpens in {Mathf.CeilToInt(level.SecondsToHalfTime)}s, " +
            "or once the field is clear";
    }

    // lightens the fill toward white by this much (0 is its own colour)
    private void Tint(float amount)
    {
        if (fills == null) return;
        for (int i = 0; i < fills.Length; i++)
        {
            if (fills[i] == null) continue;
            Color c = Color.Lerp(fillColors[i], Color.white, amount);
            c.a = fillColors[i].a;
            if (fills[i].color != c) fills[i].color = c;
        }
    }
}
