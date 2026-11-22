using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// the wave timer bar under the game controls, made a button like the start and speed buttons
// (Tommy, 2026-11-21): clicking it calls the next wave early (ProceduralLevel.CallNextWave), the way
// Kingdom Rush does, and the wave already running keeps playing.
// - "Skip" sits in the middle of the bar, between the wave number and the countdown: white while a
//   skip is open (half time, or a clear field), grey while it isn't, hidden when there's no next wave
// - while a skip is open the fill turns the start button's green, lighter under the pointer and
//   darker while pressed; otherwise it keeps its blue and ignores the pointer, like a disabled button
// - pressing drops the whole bar 2 art pixels and does nothing yet; letting go over it calls the wave
// GameHUD attaches it at runtime to the bar holding the countdown fill
public class WaveCallBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    // canvas units per art pixel
    private const float Px = 2f;
    private static readonly Color LabelOff = new Color(0.62f, 0.62f, 0.62f, 1f);

    private RectTransform rect;
    private Vector2 restPosition;
    private Image fill;
    private Color fillIdle;
    private TMP_Text label;
    private bool labelPlaced;
    private bool hovered, pressed;

    public static WaveCallBar Attach(RectTransform bar, Image fill, TMP_Text labelTemplate)
    {
        if (bar == null) return null;
        WaveCallBar call = bar.GetComponent<WaveCallBar>();
        if (call == null) call = bar.gameObject.AddComponent<WaveCallBar>();
        call.Build(fill, labelTemplate);
        return call;
    }

    private void Build(Image barFill, TMP_Text labelTemplate)
    {
        rect = (RectTransform)transform;
        restPosition = rect.anchoredPosition;
        // the bar's frame catches the pointer; the fill and texts inside it don't
        Image frame = GetComponent<Image>();
        if (frame != null) frame.raycastTarget = true;
        fill = barFill;
        if (fill != null) fillIdle = fill.color;

        // "Skip": a copy of the wave number text, so it has the same font, size and outline
        if (label == null && labelTemplate != null)
        {
            GameObject copy = Instantiate(labelTemplate.gameObject, transform, false);
            copy.name = "SkipLabel";
            label = copy.GetComponent<TMP_Text>();
            label.raycastTarget = false;
            label.text = "Skip";
            copy.SetActive(false);
        }
    }

    private void OnDisable()
    {
        hovered = pressed = false;
        if (rect != null) rect.anchoredPosition = restPosition;
        if (fill != null) fill.color = fillIdle;
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

    // Unity sends the click on release, only when the pointer is still over the bar it pressed
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        ProceduralLevel level = ProceduralLevel.Instance;
        if (level == null || !level.CallNextWave()) return;
        if (GameManager.instance != null && GameManager.instance.buttonClick != null)
            GameManager.instance.PlaySound(GameManager.instance.buttonClick);
    }

    private void Update()
    {
        ProceduralLevel level = ProceduralLevel.Instance;
        bool hasNext = level != null && level.HasNextWave;
        bool ready = hasNext && level.CanCallNextWave;
        bool down = pressed && ready;
        bool over = hovered && ready;

        if (fill != null)
        {
            GameControlsStyle.ButtonColors green = GameControlsStyle.Instance.start;
            Color c = !ready ? fillIdle : down ? green.facePressed : over ? green.faceHover : green.face;
            if (fill.color != c) fill.color = c;
        }
        Vector2 at = restPosition + (down ? new Vector2(0f, -2f * Px) : Vector2.zero);
        if (rect.anchoredPosition != at) rect.anchoredPosition = at;

        if (label == null) return;
        if (label.gameObject.activeSelf != hasNext) label.gameObject.SetActive(hasNext);
        if (!hasNext) return;
        Color text = ready ? Color.white : LabelOff;
        if (label.color != text) label.color = text;
        // centred by its drawn pixels, once the font has drawn it
        if (!labelPlaced && PixelLayout.Ink(label, Px, out _, out _))
        {
            PixelLayout.CentreByInk(label, Px);
            labelPlaced = true;
        }
    }
}
