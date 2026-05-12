using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelector : MonoBehaviour
{
    [SerializeField] private Button level1, level2, level3, level4, level5, previousPage, nextPage;
    void Start()
    {
        int highestLevelUnlocked = SaveManager.instance.saveData.highestLevelUnlocked;
        level1.interactable = highestLevelUnlocked >= 1;
        level2.interactable = highestLevelUnlocked >= 1; // change to 2 and respective numbers
        level3.interactable = highestLevelUnlocked >= 3;
        level4.interactable = highestLevelUnlocked >= 4;
        level5.interactable = highestLevelUnlocked >= 5;
        //previousPage.interactable = true;
       // nextPage.interactable = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void DisableAllButtons()
    {
        level1.interactable = false;
        level2.interactable = false;
        level3.interactable = false;
        level4.interactable = false;
        level5.interactable = false;
        //previousPage.interactable = false;
        //nextPage.interactable = false;
    }

    public void OnLevelSelected(int level)
    {
        DisableAllButtons();
        LoadoutSelectionUI.instance.Show(level);
        // LoadLevel(level);
        //Debug.Log("Loading Level " + level);
    }

    private void LoadLevel(int level)
    {
        SaveManager.instance.selectedLevel = level;
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Level"+level));
        Debug.Log("Loaded Level " + level);
    }
}
