using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class SceneTransition : MonoBehaviour
{

    [SerializeField] private CanvasGroup canvasGroup;
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
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        fadeSpeed = 4f;
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
        
        canvasGroup.blocksRaycasts = false;
    }
}
