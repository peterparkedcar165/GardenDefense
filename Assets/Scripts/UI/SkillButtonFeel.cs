using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// the plant info panel's skill square feels like a button once the skill is unlocked (Tommy,
// 2026-11-26): the pointer over it lights it up, pressing it drops it 2 art pixels like the plank
// buttons, and casting with Q plays the same press for a moment. only while the skill can be cast (its
// Button interactable); a locked skill's overlay takes the pointer itself. the square's pieces (the
// wood, the gold trim, the icon and the cooldown sweep) are gathered under one SkillVisual so they move
// together; the light is the wood's own shape in white, over them
public class SkillButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float Px = 2f;
    // how much the light shows, and how long Q's press lasts (unscaled seconds)
    private const float LightAlpha = 0.22f;
    private const float FlashSeconds = 0.14f;

    private Button button;
    private RectTransform visual;
    private Image glow;
    private bool hovered, pressed;
    private float flashUntil = -1f;
    private bool shownDown, shownLit;

    // gathers the pieces named here (siblings of the button) under SkillVisual and puts this on the button
    public static SkillButtonFeel Attach(Button button, Image wood, params Graphic[] pieces)
    {
        if (button == null) return null;
        SkillButtonFeel feel = button.GetComponent<SkillButtonFeel>();
        if (feel == null) feel = button.gameObject.AddComponent<SkillButtonFeel>();
        feel.button = button;
        feel.Build(wood, pieces);
        return feel;
    }

    private void Build(Image wood, Graphic[] pieces)
    {
        Transform container = transform.parent;
        if (container == null || visual != null) return;
        GameObject go = new GameObject("SkillVisual", typeof(RectTransform));
        go.layer = gameObject.layer;
        visual = (RectTransform)go.transform;
        visual.SetParent(container, false);
        // the same rect as the pieces' parent, so each keeps its place
        visual.anchorMin = Vector2.zero;
        visual.anchorMax = Vector2.one;
        visual.offsetMin = visual.offsetMax = Vector2.zero;
        int first = int.MaxValue;
        if (wood != null) first = Mathf.Min(first, wood.transform.GetSiblingIndex());
        foreach (Graphic piece in pieces)
            if (piece != null && piece.transform.parent == container) first = Mathf.Min(first, piece.transform.GetSiblingIndex());
        visual.SetSiblingIndex(first == int.MaxValue ? 0 : first);
        if (wood != null) wood.transform.SetParent(visual, false);
        foreach (Graphic piece in pieces)
            if (piece != null && piece.transform.parent == container) piece.transform.SetParent(visual, false);

        // the light: the wood's own shape, white, over every piece
        if (wood != null && wood.sprite != null)
        {
            GameObject lightGO = new GameObject("Light", typeof(RectTransform), typeof(Image));
            lightGO.layer = gameObject.layer;
            RectTransform r = (RectTransform)lightGO.transform;
            r.SetParent(visual, false);
            RectTransform w = wood.rectTransform;
            r.anchorMin = w.anchorMin;
            r.anchorMax = w.anchorMax;
            r.pivot = w.pivot;
            r.anchoredPosition = w.anchoredPosition;
            r.sizeDelta = w.sizeDelta;
            glow = lightGO.GetComponent<Image>();
            glow.sprite = wood.sprite;
            glow.type = wood.type;
            glow.preserveAspect = wood.preserveAspect;
            glow.raycastTarget = false;
            glow.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    // Q cast the skill: the press, a moment
    public void Flash() => flashUntil = Time.unscaledTime + FlashSeconds;

    private bool Usable => button != null && button.interactable && button.isActiveAndEnabled;

    private void Update()
    {
        bool usable = Usable;
        bool flashing = Time.unscaledTime < flashUntil;
        bool down = flashing || (usable && pressed);
        bool lit = flashing || (usable && hovered);
        if (down != shownDown && visual != null)
        {
            shownDown = down;
            visual.anchoredPosition = new Vector2(0f, down ? -2f * Px : 0f);
        }
        if (lit != shownLit && glow != null)
        {
            shownLit = lit;
            glow.color = new Color(1f, 1f, 1f, lit ? LightAlpha : 0f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && Usable) pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) pressed = false;
    }

    private void OnDisable()
    {
        hovered = pressed = false;
        flashUntil = -1f;
        if (visual != null) visual.anchoredPosition = Vector2.zero;
        if (glow != null) glow.color = new Color(1f, 1f, 1f, 0f);
        shownDown = shownLit = false;
    }
}
