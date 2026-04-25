using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class SceneTransition : MonoBehaviour
{

    CanvasGroup canvasGroup;
    float fadeSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        canvasGroup = FindAnyObjectByType<CanvasGroup>();
        canvasGroup.alpha = 0;
        fadeSpeed = 1.5f;
    }

    public IEnumerator FadeToScene(string sceneName)
    {
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += fadeSpeed * Time.deltaTime;
            yield return null;
        }

        SceneManager.LoadScene(sceneName);

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= fadeSpeed * Time.deltaTime;
            yield return null;   
        }
    }
}
