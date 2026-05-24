using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;


public class SettingsManager : MonoBehaviour
{
    public static SettingsManager instance;
    public static string previousScene;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider, musicSlider, gameSlider;
    [SerializeField] private TMP_Text masterText, musicText, gameText;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject mainMenuButton;

    public bool IsOpen => settingsPanel.activeSelf;

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Canvas rootCanvas = settingsPanel.GetComponentInParent<Canvas>(true);
        if (rootCanvas != null) rootCanvas.sortingOrder = 100;
        settingsPanel.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.SetActive(false);
    }

    private void Update()
    {
        if (restartButton != null)
            restartButton.SetActive(GameManager.instance != null && GameManager.instance.IsGameActive);
        if (mainMenuButton != null)
            mainMenuButton.SetActive(GameManager.instance != null && GameManager.instance.IsGameActive);

        bool skillCancelled = SkillTargetingManager.instance != null && SkillTargetingManager.instance.WasCancelledThisFrame;
        bool loadoutOpen = LoadoutSelectionUI.instance != null && LoadoutSelectionUI.instance.IsOpen;
        bool fertilizerOpen = FertilizerSelectionUI.instance != null && FertilizerSelectionUI.instance.IsOpen;
        bool inEncyclopedia = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Encyclopedia";
        bool inLevelSelector = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "LevelSelector";
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !skillCancelled && !loadoutOpen && !fertilizerOpen && !inEncyclopedia && !inLevelSelector && !SceneTransition.IsTransitioning)
        {
            bool isOpen = !settingsPanel.activeSelf;
            settingsPanel.SetActive(isOpen);
            if (GameManager.instance != null)
                GameManager.instance.SetPause(isOpen);
        }
    }

    public void Open()
    {
        settingsPanel.SetActive(true);
        if (GameManager.instance != null)
            GameManager.instance.SetPause(true);
    }

    public void Close()
    {
        settingsPanel.SetActive(false);
        // Don't touch pause state here — caller is responsible
        // (e.g. loadout/fertilizer screens manage pause themselves)
    }
    public void Restart()
    {
        Time.timeScale = 1f;
        settingsPanel.SetActive(false);
        if (GameManager.instance != null)
            GameManager.instance.Restart();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        settingsPanel.SetActive(false);
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        if (transition != null)
            transition.StartCoroutine(transition.FadeToScene("MainMenu"));
        else
            SceneManager.LoadScene("MainMenu");
    }

    public void GoBack()
    {
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene(previousScene));
    }
    private void Start()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.1f);
        float game = PlayerPrefs.GetFloat("GameVolume", 1f);

        masterSlider.value = master;
        musicSlider.value = music;
        gameSlider.value = game;

        ApplyVolume("MasterVolume", master);
        ApplyVolume("MusicVolume", music);
        ApplyVolume("GameVolume", game);

        masterText.text = $"Master: {Mathf.RoundToInt(master * 100)}%";
        musicText.text  = $"Music: {Mathf.RoundToInt(music * 100)}%";
        gameText.text   = $"Game: {Mathf.RoundToInt(game * 100)}%";
    }

    public void OnMasterChanged(float value)
    {
        ApplyVolume("MasterVolume", value);
        PlayerPrefs.SetFloat("MasterVolume", value);
        masterText.text = $"Master: {Mathf.RoundToInt(value * 100)}%";
    }

    public void OnMusicChanged(float value)
    {
        ApplyVolume("MusicVolume", value);
        PlayerPrefs.SetFloat("MusicVolume", value);
        musicText.text = $"Music: {Mathf.RoundToInt(value * 100)}%";
    }

    public void OnGameChanged(float value)
    {
        ApplyVolume("GameVolume", value);
        PlayerPrefs.SetFloat("GameVolume", value);
        gameText.text = $"Game: {Mathf.RoundToInt(value * 100)}%";
    }

    private void ApplyVolume(string parameter, float value)
    {
        audioMixer.SetFloat(parameter, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }
}
