using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // the planks are saved in the scene once Tools > Garden Defense > Bake Planks in Open Scene has run;
        // until then (or for a button added since) they're made here when the scene starts
        if (!PlanksBaked()) SkinButtons();
        foreach (Button b in buttons)
            b.interactable = true;
    }

    // the menu's buttons as big planks (Tommy, 2026-11-25): each button becomes the game controls'
    // plank in place (PlankButton.Adopt, so its own onClick wiring stays) at 1.5 times its size (1 art
    // px = 3 canvas units, still whole pixels at 1080p), all as wide as the widest, in one evenly spaced
    // column centred where the column was, Play green, Exit red, the rest yellow; the NOT IMPLEMENTED
    // notes sit beside their buttons. running it again changes nothing, so the editor's bake and the
    // scene starting agree
    private const float PlankScale = 1.5f;
    private const float PlankGap = 14f;

    private Transform MenuCanvas => buttons != null && buttons.Length > 0 && buttons[0] != null ? buttons[0].transform.parent : null;

    private bool PlanksBaked()
    {
        Transform canvas = MenuCanvas;
        if (canvas == null) return true;
        foreach (Transform child in canvas)
            if (child.GetComponent<Button>() != null && child.GetComponent<PlankButton>() == null) return false;
        return true;
    }

    public void SkinButtons()
    {
        Transform canvas = MenuCanvas;
        if (canvas == null) return;
        GameControlsStyle look = GameControlsStyle.Instance;
        TMPro.TMP_FontAsset font = InsectShowcaseStyle.Instance.font;

        // every button on the menu's canvas, top to bottom as the scene has them
        var rows = new System.Collections.Generic.List<(RectTransform rect, float y, PlankButton plank)>();
        foreach (Transform child in canvas)
        {
            Button button = child.GetComponent<Button>();
            if (button == null || !(child is RectTransform rect)) continue;
            GameControlsStyle.ButtonColors colors = child.name == "PlayButton" ? look.start : child.name == "QuitButton" ? look.danger : look.pause;
            rows.Add((rect, rect.anchoredPosition.y, PlankButton.Adopt(button, colors, font)));
        }
        if (rows.Count == 0) return;
        rows.Sort((a, b) => b.y.CompareTo(a.y));

        float width = 0f, height = 0f, top = float.MinValue, bottom = float.MaxValue;
        foreach (var row in rows)
        {
            width = Mathf.Max(width, row.plank.Rect.sizeDelta.x);
            height = row.plank.Rect.sizeDelta.y;
            top = Mathf.Max(top, row.y);
            bottom = Mathf.Min(bottom, row.y);
        }
        float pitch = height * PlankScale + PlankGap;
        float firstY = Mathf.Round((top + bottom) * 0.5f + (rows.Count - 1) * pitch * 0.5f);
        for (int i = 0; i < rows.Count; i++)
        {
            RectTransform r = rows[i].rect;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(width, height);
            r.localScale = Vector3.one * PlankScale;
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, firstY - i * pitch);
        }

        // a NOT IMPLEMENTED note goes right of the button it stood beside
        foreach (Transform child in canvas)
        {
            if (!child.name.StartsWith("NotImplementedText") || !(child is RectTransform note)) continue;
            var nearest = rows[0];
            foreach (var row in rows)
                if (Mathf.Abs(row.y - note.anchoredPosition.y) < Mathf.Abs(nearest.y - note.anchoredPosition.y)) nearest = row;
            note.pivot = new Vector2(0f, 0.5f);
            note.anchoredPosition = new Vector2(nearest.rect.anchoredPosition.x + width * PlankScale * 0.5f + 24f, nearest.rect.anchoredPosition.y);
            TMPro.TMP_Text label = note.GetComponent<TMPro.TMP_Text>();
            if (label != null) label.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Left;
        }
    }

    private void DisableAllButtons()
    {
        foreach (Button b in buttons)
            b.interactable = false;
    }

    private bool TryStartTransition(string scene)
    {
        if (SceneTransition.IsTransitioning) return false;
        DisableAllButtons();
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene(scene));
        return true;
    }

    public void OnPlay()
    {
        TryStartTransition("LevelSelector");
    }

    public void OnShop()
    {
        TryStartTransition("Shop");
    }

    public void OnEncyclopedia()
    {
        TryStartTransition("Encyclopedia");
    }

    public void OnSkillTree()
    {
        TryStartTransition("SkillTree");
    }

    public void OnQuit()
    {
        Debug.Log("Quit clicked");
        Application.Quit();
    }
}
