using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LevelSelector : MonoBehaviour
{
    [SerializeField] private Button level1, level2, level3, level4, level5, level6, level7, level8, level9, level10, level11, level12, level13, level14, level15, previousPage, nextPage;
    [SerializeField] private Button level16, level17, level18, level19, level20, level21, level22, level23, level24, level25;
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
        //previousPage.interactable = true;
       // nextPage.interactable = true;
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !SceneTransition.IsTransitioning)
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
        //previousPage.interactable = false;
        //nextPage.interactable = false;
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
