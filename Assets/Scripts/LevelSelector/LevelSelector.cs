using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LevelSelector : MonoBehaviour
{
    [SerializeField] private Button level1, level2, level3, level4, level5, level6, level7, level8, level9, level10, level11, level12, level13, level14, level15, previousPage, nextPage;
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
        //previousPage.interactable = false;
        //nextPage.interactable = false;
    }

    public void OnLevelSelected(int level)
    {
        DisableAllButtons();
        SaveManager.instance.selectedLevel = level;
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Level" + level));
    }
}
