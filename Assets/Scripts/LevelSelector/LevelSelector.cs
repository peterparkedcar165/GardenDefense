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
        level2.interactable = highestLevelUnlocked >= 2;
        level3.interactable = highestLevelUnlocked >= 3;
        level4.interactable = highestLevelUnlocked >= 4;
        level5.interactable = highestLevelUnlocked >= 5;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnLevelSelected(int level)
    {
        LoadLevel(level);
        Debug.Log("Loading Level " + level);
    }

    private void LoadLevel(int level)
    {
        SaveManager.instance.selectedLevel = level;
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Level"+level));
        Debug.Log("Loaded Level " + level);
    }
}
