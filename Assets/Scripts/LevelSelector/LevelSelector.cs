using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LevelSelector : MonoBehaviour
{
    [SerializeField] private Button level1, level2, level3, level4, level5, level6, level7, level8, level9, level10, level11, level12, level13, level14, level15;
    [SerializeField] private Button level16, level17, level18, level19, level20, level21, level22, level23, level24, level25;
    [SerializeField] private Button level26, level27, level28, level29, level30;

    // ---- the look (Tommy, 2026-11-25): every level button is the game controls' plank, in the colour of
    // its group of five levels; a locked level's plank is faded ----
    [Header("Plank colours, one per five levels (1 to 5, 6 to 10, ...)")]
    [SerializeField] private GameControlsStyle.ButtonColors[] groupColors =
    {
        // 1 to 5 green: the game controls' Start colours
        Plank("#62B83C", "#94DC5A", "#42902A", "#F4FFE6", "#1E4A14"),
        // 6 to 10 purple
        Plank("#8E62C4", "#B08AE0", "#6E48A0", "#F6EEFF", "#2E1A48"),
        // 11 to 15 sand
        Plank("#D8B868", "#ECD48C", "#B89848", "#FFF8E6", "#5A4418"),
        // 16 to 20 brown
        Plank("#8A5A32", "#A8784C", "#6C4424", "#FFF1E0", "#2E1A0C"),
        // 21 to 25 white: dark words, so they read on it
        Plank("#E6EEF2", "#FFFFFF", "#C4D0D8", "#2E3E4A", "#FFFFFF"),
        // 26 to 30: no colour asked for yet, the game controls' blue (proposal)
        Plank("#4A9EDC", "#7CC8F4", "#3478B8", "#F0FAFF", "#183A66"),
    };

    // the planks 1.5 times their size (Tommy, 2026-11-25: bigger; 1 art px = 3 canvas units, whole pixels
    // at 1080). canvas units: the space between two planks in a column, and the extra between its two
    // groups of five
    private const float PlankScale = 1.5f;
    private const float RowGap = 12f;
    private const float GroupGap = 40f;

    private static GameControlsStyle.ButtonColors Plank(string face, string hover, string pressed, string text, string shadow) =>
        new GameControlsStyle.ButtonColors(Hex(face), Hex(hover), Hex(pressed), Hex(text), Hex(shadow));

    private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

    private Button[] Levels => new[]
    {
        level1, level2, level3, level4, level5, level6, level7, level8, level9, level10, level11, level12, level13, level14, level15,
        level16, level17, level18, level19, level20, level21, level22, level23, level24, level25, level26, level27, level28, level29, level30,
    };

    // each level button becomes a plank in place (PlankButton.Adopt keeps its OnLevelSelected wiring),
    // as wide as it was, drawn PlankScale times; the columns spread evenly across the screen (Tommy,
    // 2026-11-25), each in the middle of its equal share of the width, and each column's ten planks
    // spaced evenly, a wider gap between its two groups of five, centred where the column was. running it again changes nothing, so the editor's bake
    // (Tools > Garden Defense > Bake Planks in Open Scene) and the scene starting agree
    public void SkinButtons()
    {
        TMPro.TMP_FontAsset font = InsectShowcaseStyle.Instance.font;
        Button[] levels = Levels;
        var columns = new System.Collections.Generic.List<System.Collections.Generic.List<RectTransform>>();
        for (int i = 0; i < levels.Length; i++)
        {
            Button b = levels[i];
            if (b == null) continue;
            RectTransform rect = (RectTransform)b.transform;
            float width = Mathf.Round(rect.sizeDelta.x / 2f) * 2f;
            GameControlsStyle.ButtonColors colors = groupColors != null && groupColors.Length > 0
                ? groupColors[Mathf.Min(i / 5, groupColors.Length - 1)] : GameControlsStyle.Instance.start;
            PlankButton plank = PlankButton.Adopt(b, colors, font);
            rect.sizeDelta = new Vector2(Mathf.Max(plank.Rect.sizeDelta.x, width), plank.Rect.sizeDelta.y);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one * PlankScale;

            // the column it stands in: the one whose x it shares (within 20 units)
            var column = columns.Find(c => Mathf.Abs(c[0].anchoredPosition.x - rect.anchoredPosition.x) < 20f);
            if (column == null) columns.Add(column = new System.Collections.Generic.List<RectTransform>());
            column.Add(rect);
        }

        // the columns left to right, each in the middle of its equal share of the screen's width
        columns.Sort((a, b) => a[0].anchoredPosition.x.CompareTo(b[0].anchoredPosition.x));
        RectTransform screen = columns.Count > 0 ? columns[0][0].parent as RectTransform : null;
        float screenWidth = screen != null && screen.rect.width > 0f ? screen.rect.width : 1920f;
        float share = columns.Count > 0 ? screenWidth / columns.Count : 0f;

        for (int c = 0; c < columns.Count; c++)
        {
            var column = columns[c];
            column.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
            float top = float.MinValue, bottom = float.MaxValue, height = column[0].sizeDelta.y * PlankScale;
            foreach (RectTransform r in column)
            {
                top = Mathf.Max(top, r.anchoredPosition.y);
                bottom = Mathf.Min(bottom, r.anchoredPosition.y);
            }
            float x = Mathf.Round((c + 0.5f - columns.Count * 0.5f) * share);
            int groups = (column.Count - 1) / 5;
            float span = (column.Count - 1) * (height + RowGap) + groups * GroupGap;
            float y = Mathf.Round((top + bottom) * 0.5f + span * 0.5f);
            for (int i = 0; i < column.Count; i++)
            {
                if (i > 0) y -= height + RowGap + (i % 5 == 0 ? GroupGap : 0f);
                column[i].anchoredPosition = new Vector2(x, y);
            }
        }
    }

    void Start()
    {
        // saved in the scene by the bake; this runs anyway (it changes nothing on an up to date scene), so
        // a scene baked with older buttons still shows the current ones
        SkinButtons();
        int highestLevelUnlocked = SaveManager.instance.saveData.highestLevelUnlocked;
        level1.interactable = true;
        level2.interactable = highestLevelUnlocked >= 1;
        level3.interactable = highestLevelUnlocked >= 2;
        level4.interactable = highestLevelUnlocked >= 3;
        level5.interactable = highestLevelUnlocked >= 4;
        level6.interactable = highestLevelUnlocked >= 5;
        level7.interactable = highestLevelUnlocked >= 6;
        if (level8  != null) level8.interactable  = highestLevelUnlocked >= 7;
        if (level9  != null) level9.interactable  = highestLevelUnlocked >= 8;
        if (level10 != null) level10.interactable = highestLevelUnlocked >= 9;
        if (level11 != null) level11.interactable = highestLevelUnlocked >= 10;
        if (level12 != null) level12.interactable = highestLevelUnlocked >= 11;
        if (level13 != null) level13.interactable = highestLevelUnlocked >= 12;
        if (level14 != null) level14.interactable = highestLevelUnlocked >= 13;
        if (level15 != null) level15.interactable = highestLevelUnlocked >= 14;
        if (level16 != null) level16.interactable = highestLevelUnlocked >= 15;
        if (level17 != null) level17.interactable = highestLevelUnlocked >= 16;
        if (level18 != null) level18.interactable = highestLevelUnlocked >= 17;
        if (level19 != null) level19.interactable = highestLevelUnlocked >= 18;
        if (level20 != null) level20.interactable = highestLevelUnlocked >= 19;
        if (level21 != null) level21.interactable = highestLevelUnlocked >= 20;
        if (level22 != null) level22.interactable = highestLevelUnlocked >= 21;
        if (level23 != null) level23.interactable = highestLevelUnlocked >= 22;
        if (level24 != null) level24.interactable = highestLevelUnlocked >= 23;
        if (level25 != null) level25.interactable = highestLevelUnlocked >= 24;
        if (level26 != null) level26.interactable = highestLevelUnlocked >= 25;
        if (level27 != null) level27.interactable = highestLevelUnlocked >= 26;
        if (level28 != null) level28.interactable = highestLevelUnlocked >= 27;
        if (level29 != null) level29.interactable = highestLevelUnlocked >= 28;
        if (level30 != null) level30.interactable = highestLevelUnlocked >= 29;
#if UNITY_EDITOR
        AddTestLevels();
#endif
    }

    // ---- test copies of levels (editor only, never in a build): a plank beside the level it copies, always
    // open, that plays it in its test scene (Tommy, 2026-11-27: the border cliffs in Level7_CliffTest) ----
#if UNITY_EDITOR
    private Button _cliffTestButton;
    private const string CliffTestScene = "Assets/Scenes/Level7_CliffTest.unity";
    private const float TestPlankGap = 24f;

    private void AddTestLevels()
    {
        if (level7 == null || UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(CliffTestScene) == null) return;
        Button test = Instantiate(level7, level7.transform.parent);
        test.name = "Level 7 Cliff Test";
        // the copy keeps level 7's own click (OnLevelSelected(7) from the inspector): switched off for it
        for (int i = 0; i < test.onClick.GetPersistentEventCount(); i++)
            test.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        test.onClick.AddListener(() => OnTestLevelSelected(7, CliffTestScene));
        test.interactable = true;
        PlankButton plank = test.GetComponent<PlankButton>();
        if (plank != null) plank.SetLabel("2.2 Cliffs");

        // just right of level 7's plank, on its row
        RectTransform source = (RectTransform)level7.transform, rect = (RectTransform)test.transform;
        float x = source.anchoredPosition.x + (source.sizeDelta.x + rect.sizeDelta.x) * 0.5f * PlankScale + TestPlankGap;
        rect.anchoredPosition = new Vector2(x, source.anchoredPosition.y);
        _cliffTestButton = test;
    }

    // plays the level (its waves, loadout and unlocks are the scene's, so the copy counts as that level)
    // in its test scene, which isn't in the build settings, so it's loaded by its path
    private void OnTestLevelSelected(int level, string scenePath)
    {
        DisableAllButtons();
        SaveManager.instance.selectedLevel = level;
        LoadoutSelectionUI.instance?.ResetUnlockedScroll();
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScenePath(scenePath));
    }
#endif

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !SceneTransition.IsTransitioning && !TutorialManager.BlocksEscape)
            GoToMainMenu();
    }

    private void GoToMainMenu()
    {
        DisableAllButtons();
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("MainMenu"));
    }

    private void DisableAllButtons()
    {
        level1.interactable = false;
        level2.interactable = false;
        level3.interactable = false;
        level4.interactable = false;
        level5.interactable = false;
        level6.interactable = false;
        level7.interactable = false;
        if (level8  != null) level8.interactable  = false;
        if (level9  != null) level9.interactable  = false;
        if (level10 != null) level10.interactable = false;
        if (level11 != null) level11.interactable = false;
        if (level12 != null) level12.interactable = false;
        if (level13 != null) level13.interactable = false;
        if (level14 != null) level14.interactable = false;
        if (level15 != null) level15.interactable = false;
        if (level16 != null) level16.interactable = false;
        if (level17 != null) level17.interactable = false;
        if (level18 != null) level18.interactable = false;
        if (level19 != null) level19.interactable = false;
        if (level20 != null) level20.interactable = false;
        if (level21 != null) level21.interactable = false;
        if (level22 != null) level22.interactable = false;
        if (level23 != null) level23.interactable = false;
        if (level24 != null) level24.interactable = false;
        if (level25 != null) level25.interactable = false;
        if (level26 != null) level26.interactable = false;
        if (level27 != null) level27.interactable = false;
        if (level28 != null) level28.interactable = false;
        if (level29 != null) level29.interactable = false;
        if (level30 != null) level30.interactable = false;
#if UNITY_EDITOR
        if (_cliffTestButton != null) _cliffTestButton.interactable = false;
#endif
    }

    public void OnLevelSelected(int level)
    {
        DisableAllButtons();
        SaveManager.instance.selectedLevel = level;
        LoadoutSelectionUI.instance?.ResetUnlockedScroll();
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Level" + level));
    }
}
