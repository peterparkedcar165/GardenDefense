using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (Button b in buttons)
            b.interactable = true;
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

    public void OnSettings()
    {
        if (TryStartTransition("Settings"))
            SettingsManager.previousScene = "MainMenu";
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
