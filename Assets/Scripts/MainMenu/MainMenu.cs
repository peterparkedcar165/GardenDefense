using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPlay()
    {
        Debug.Log("Play clicked");
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("LevelSelector"));
    }

    public void OnSettings()
    {
        Debug.Log("Settings clicked");
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Settings"));
    }

    public void OnShop()
    {
        Debug.Log("Shop clicked");
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Shop"));
    }

    public void OnQuit()
    {
        Debug.Log("Quit clicked");
        Application.Quit();
    }
}
