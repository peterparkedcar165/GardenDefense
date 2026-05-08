using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void DisableAllButtons()
    {
        foreach (Button b in buttons)
        {
            b.interactable = false;
        }
    }
    public void OnPlay()
    {
        Debug.Log("Play clicked");
        DisableAllButtons();
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("LevelSelector"));
    }

    public void OnSettings()
    {
        Debug.Log("Settings clicked");
        DisableAllButtons();
        SettingsManager.previousScene = "MainMenu";
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Settings"));
    }

    public void OnShop()
    {
        Debug.Log("Shop clicked");
        DisableAllButtons();
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Shop"));
    }

    public void OnQuit()
    {
        Debug.Log("Quit clicked");
        Application.Quit();
    }
}
