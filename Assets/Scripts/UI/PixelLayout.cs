using UnityEngine;
using TMPro;

// helpers for the pixel art ui, where every position is a whole art pixel. the texts they place are
// anchored to their parent's top left; pixel is canvas units per art pixel
public static class PixelLayout
{
    // a text left aligned in a rect as wide as it reads, placed so it sits centred on its parent on
    // whole art pixels (TMP's own centring lands odd widths on half pixels)
    public static void CentreOnPixels(TMP_Text text, float pixel)
    {
        if (text == null) return;
        RectTransform rect = text.rectTransform;
        RectTransform parent = rect.parent as RectTransform;
        if (parent == null) return;
        text.horizontalAlignment = HorizontalAlignmentOptions.Left;
        float width = string.IsNullOrEmpty(text.text) ? 0f : Mathf.Ceil(text.GetPreferredValues(text.text).x / pixel) * pixel;
        float x = Mathf.Round((parent.rect.width - width) / 2f / pixel) * pixel;
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
    }

    // the pixels a text actually draws, which is what the pixel art layouts measure (Downloads\
    // gd_game_controls): their left edge from the text rect's left edge and their width, in whole
    // art pixels. the advance width TMP reports also counts the spacing after the last letter, so
    // placing by it puts short texts 1 or 2 pixels off. left aligns the text and sizes its rect to
    // what it reads first. false for an empty text or before the font has drawn it
    public static bool Ink(TMP_Text text, float pixel, out float left, out float width)
    {
        left = 0f;
        width = 0f;
        if (text == null || string.IsNullOrEmpty(text.text)) return false;
        RectTransform rect = text.rectTransform;
        text.horizontalAlignment = HorizontalAlignmentOptions.Left;
        float advance = Mathf.Ceil(text.GetPreferredValues(text.text).x / pixel) * pixel;
        rect.sizeDelta = new Vector2(advance, rect.sizeDelta.y);
        text.ForceMeshUpdate(true, true);
        TMP_TextInfo info = text.textInfo;
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) continue;
            min = Mathf.Min(min, c.bottomLeft.x);
            max = Mathf.Max(max, c.topRight.x);
        }
        if (float.IsInfinity(min)) return false;
        left = Mathf.Round((min - rect.rect.xMin) / pixel);
        width = Mathf.Round((max - min) / pixel);
        return true;
    }

    // the drawn pixels centred on the parent: their left edge on (parent width - their width) / 2,
    // rounded down. falls back to CentreOnPixels before the font has drawn it
    public static void CentreByInk(TMP_Text text, float pixel)
    {
        RectTransform parent = text != null ? text.rectTransform.parent as RectTransform : null;
        if (parent == null) return;
        if (!Ink(text, pixel, out float inkLeft, out float inkWidth)) { CentreOnPixels(text, pixel); return; }
        float left = Mathf.Floor((Mathf.Round(parent.rect.width / pixel) - inkWidth) / 2f);
        SetInkLeft(text, pixel, inkLeft, left);
    }

    // the drawn pixels' left edge inset art pixels in from the parent's left edge
    public static void PlaceInkLeft(TMP_Text text, float pixel, float inset)
    {
        if (!Ink(text, pixel, out float inkLeft, out _)) return;
        SetInkLeft(text, pixel, inkLeft, inset);
    }

    // the drawn pixels' right edge inset art pixels in from the parent's right edge
    public static void PlaceInkRight(TMP_Text text, float pixel, float inset)
    {
        RectTransform parent = text != null ? text.rectTransform.parent as RectTransform : null;
        if (parent == null) return;
        if (!Ink(text, pixel, out float inkLeft, out float inkWidth)) return;
        SetInkLeft(text, pixel, inkLeft, Mathf.Round(parent.rect.width / pixel) - inset - inkWidth);
    }

    private static void SetInkLeft(TMP_Text text, float pixel, float inkLeft, float x)
    {
        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = new Vector2((x - inkLeft) * pixel, rect.anchoredPosition.y);
    }
}
