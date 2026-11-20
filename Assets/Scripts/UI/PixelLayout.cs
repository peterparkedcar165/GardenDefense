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
}
