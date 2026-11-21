using UnityEngine;
using TMPro;

// helpers for the pixel art ui, where every position is a whole art pixel
public static class PixelLayout
{
    // a text left aligned in a rect as wide as it reads, placed so it sits centred on its parent on
    // whole art pixels (TMP's own centring lands odd widths on half pixels). the rect is anchored to
    // the parent's top left. pixel: canvas units per art pixel
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

    // like CentreOnPixels, but by the pixels the text actually draws, not its advance width (which
    // counts the spacing after the last letter and puts short texts 1 or 2 pixels too far right):
    // the drawn pixels' left edge lands on (parent width - their width) / 2, rounded down
    // (Downloads\gd_game_controls README). falls back to CentreOnPixels before the font has drawn it
    public static void CentreByInk(TMP_Text text, float pixel)
    {
        if (text == null) return;
        RectTransform rect = text.rectTransform;
        RectTransform parent = rect.parent as RectTransform;
        if (parent == null) return;
        text.horizontalAlignment = HorizontalAlignmentOptions.Left;
        if (string.IsNullOrEmpty(text.text)) { CentreOnPixels(text, pixel); return; }
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
        if (float.IsInfinity(min)) { CentreOnPixels(text, pixel); return; }
        // the drawn pixels' left edge from the rect's left edge, and their width, in whole pixels
        float inkLeft = Mathf.Round((min - rect.rect.xMin) / pixel);
        float inkWidth = Mathf.Round((max - min) / pixel);
        float parentWidth = Mathf.Round(parent.rect.width / pixel);
        float left = Mathf.Floor((parentWidth - inkWidth) / 2f);
        rect.anchoredPosition = new Vector2((left - inkLeft) * pixel, rect.anchoredPosition.y);
    }
}
