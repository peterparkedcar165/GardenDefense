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
    
    static SceneTransition instance;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        fadeSpeed = 2.5f;
    }

    public static bool IsTransitioning { get; private set; }
    private bool isTransitioning = false;

    public IEnumerator FadeToScene(string sceneName) => FadeAndLoad(() => SceneManager.LoadScene(sceneName));

#if UNITY_EDITOR
    // a scene that isn't in the build settings (an editor test copy, e.g. Level7_CliffTest), by its asset
    // path: editor only
    public IEnumerator FadeToScenePath(string path) => FadeAndLoad(() =>
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single)));
#endif

    // reloads the scene being played (restart). a test copy missing from the build settings can't be
    // loaded by name, so in the editor it's loaded by its path
    public static void ReloadActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        if (SceneUtility.GetBuildIndexByScenePath(scene.path) < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(scene.name);
    }

    private IEnumerator FadeAndLoad(System.Action load)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        IsTransitioning = true;
        canvasGroup.blocksRaycasts = true;

        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += fadeSpeed * Time.unscaledDeltaTime;
            yield return null;
        }

        load();

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= fadeSpeed * Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
        IsTransitioning = false;
    }
}
