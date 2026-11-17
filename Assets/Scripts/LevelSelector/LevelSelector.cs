using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class LevelSelector : MonoBehaviour
{
    [SerializeField] private Button level1, level2, level3, level4, level5, level6, level7, level8, level9, level10, level11, level12, level13, level14, level15;
    [SerializeField] private Button level16, level17, level18, level19, level20, level21, level22, level23, level24, level25;
    [SerializeField] private Button level26, level27, level28, level29, level30;

    // test copies of levels 6 and 7 (Level6Test, Level7Test): night levels on the day sized map
    // with the real level's waves and spawns. a small button beside the real one, always open
    [Tooltip("adds a Test button beside levels 6 and 7 that loads Level6Test and Level7Test")]
    [SerializeField] private bool showTestLevels = true;
    private readonly List<Button> testButtons = new List<Button>();

    void Start()
    {
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

        if (showTestLevels)
        {
            AddTestButton(level6, 6);
            AddTestButton(level7, 7);
        }
    }

    // a copy of the real level's button, narrower, placed just left of it
    private void AddTestButton(Button source, int level)
    {
        if (source == null) return;
        const float width = 100f, gap = 6f;
        Button test = Instantiate(source, source.transform.parent);
        test.name = $"Level{level}TestButton";
        test.onClick = new Button.ButtonClickedEvent(); // drops the copied OnLevelSelected call
        test.onClick.AddListener(() => OnTestLevelSelected(level));
        test.interactable = true;

        RectTransform sourceRect = (RectTransform)source.transform;
        RectTransform rect = (RectTransform)test.transform;
        float sourceLeft = sourceRect.anchoredPosition.x - sourceRect.sizeDelta.x * sourceRect.pivot.x;
        rect.sizeDelta = new Vector2(width, sourceRect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(sourceLeft - gap - width * (1f - rect.pivot.x), sourceRect.anchoredPosition.y);

        TMP_Text label = test.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = label.text + " Test";
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = 10f;
        }
        testButtons.Add(test);
    }

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
        foreach (Button test in testButtons) test.interactable = false;
    }

    public void OnLevelSelected(int level) => LoadLevel(level, "Level" + level);

    // selectedLevel stays the real level number, so anything keyed on it treats the test like level 6 or 7
    public void OnTestLevelSelected(int level) => LoadLevel(level, "Level" + level + "Test");

    private void LoadLevel(int level, string sceneName)
    {
        DisableAllButtons();
        SaveManager.instance.selectedLevel = level;
        LoadoutSelectionUI.instance?.ResetUnlockedScroll();
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene(sceneName));
    }
}
