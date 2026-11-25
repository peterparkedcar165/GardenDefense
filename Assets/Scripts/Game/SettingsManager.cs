using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;


public class SettingsManager : MonoBehaviour
{
    public static SettingsManager instance;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider, musicSlider, gameSlider, ambientSlider;
    [SerializeField] private TMP_Text masterText, musicText, gameText, ambientText;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject mainMenuButton;
    [SerializeField] private Toggle damageIndicatorsToggle, healingIndicatorsToggle, statusEffectIndicatorsToggle;

    public bool IsOpen => settingsPanel.activeSelf;

    // read statically by DamageIndicator/HealIndicator/StatusIndicator.Spawn, so they don't need
    // a SettingsManager reference of their own - default true (indicators on) if no save exists
    // yet, or if this runs before SettingsManager.Start for any reason
    public static bool ShowDamageIndicators = true;
    public static bool ShowHealingIndicators = true;
    public static bool ShowStatusEffectIndicators = true;

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Canvas rootCanvas = settingsPanel.GetComponentInParent<Canvas>(true);
        if (rootCanvas != null) rootCanvas.sortingOrder = 100;
        // Tommy's pixel kit on the menu's bars, toggles and buttons (SettingsSkin), while it's all still
        // active so the planks can measure their words
        foreach (Slider slider in new[] { masterSlider, musicSlider, gameSlider, ambientSlider })
            SettingsSkin.Slider(slider);
        foreach (Toggle toggle in new[] { damageIndicatorsToggle, healingIndicatorsToggle, statusEffectIndicatorsToggle })
            SettingsSkin.Toggle(toggle);
        SettingsSkin.Plank(restartButton, GameControlsStyle.Instance.speed);
        SettingsSkin.Plank(mainMenuButton, GameControlsStyle.Instance.speed);
        // bigger and the same size (Tommy, 2026-11-25): 1.5 times (60 tall, 1 art px = 3 units), as wide
        // as Main Menu needs; they stand 100 apart, so 40 between them
        SettingsSkin.SameSize(1.5f, restartButton, mainMenuButton);
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

        // everything below only matters on an escape press, so the checks (the scene name ones
        // make a new string each) only run on that frame
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // true if targeting was active (about to be cancelled) or was already cancelled this frame.
        // covers both orderings of Update() between this script and SkillTargetingManager, so an
        // escape press that closes a skill's targeting never also opens the settings panel
        bool skillCancelled = SkillTargetingManager.instance != null &&
            (SkillTargetingManager.instance.IsTargeting || SkillTargetingManager.instance.WasCancelledThisFrame ||
             SkillTargetingManager.instance.IsPlantTargeting || SkillTargetingManager.instance.WasPlantCancelledThisFrame ||
             SkillTargetingManager.instance.IsDeadTileTargeting);
        bool loadoutOpen = LoadoutSelectionUI.instance != null && LoadoutSelectionUI.instance.IsOpen;
        bool fertilizerOpen = FertilizerSelectionUI.instance != null && FertilizerSelectionUI.instance.IsOpen;
        bool mulchOpen = MulchDebugMenu.instance != null && MulchDebugMenu.instance.IsOpen;
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool inEncyclopedia = sceneName == "Encyclopedia";
        bool inLevelSelector = sceneName == "LevelSelector";
        bool inShop = sceneName == "Shop";
        bool inSkillTree = sceneName == "SkillTree";
        if (!skillCancelled && !loadoutOpen && !fertilizerOpen && !mulchOpen && !inEncyclopedia && !inLevelSelector && !inShop && !inSkillTree && !SceneTransition.IsTransitioning && !TutorialManager.BlocksEscape)
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
        // Don't touch pause state here , caller is responsible
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
        LoadoutSelectionUI.instance?.Hide();
        FertilizerSelectionUI.instance?.CloseForSceneExit();
        MulchDebugMenu.instance?.CloseForSceneExit();
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        if (transition != null)
            transition.StartCoroutine(transition.FadeToScene("MainMenu"));
        else
            SceneManager.LoadScene("MainMenu");
    }

    private void Start()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.1f);
        float game = PlayerPrefs.GetFloat("GameVolume", 1f);
        float ambient = PlayerPrefs.GetFloat("AmbientVolume", 1f);

        masterSlider.value = master;
        musicSlider.value = music;
        gameSlider.value = game;
        if (ambientSlider != null) ambientSlider.value = ambient;

        ApplyVolume("MasterVolume", master);
        ApplyVolume("MusicVolume", music);
        ApplyVolume("GameVolume", game);
        ApplyVolume("AmbientVolume", ambient);

        masterText.text = $"Master: {Mathf.RoundToInt(master * 100)}%";
        musicText.text  = $"Music: {Mathf.RoundToInt(music * 100)}%";
        gameText.text   = $"Game: {Mathf.RoundToInt(game * 100)}%";
        if (ambientText != null) ambientText.text = $"Ambient: {Mathf.RoundToInt(ambient * 100)}%";

        ShowDamageIndicators      = PlayerPrefs.GetInt("ShowDamageIndicators", 1) == 1;
        ShowHealingIndicators     = PlayerPrefs.GetInt("ShowHealingIndicators", 1) == 1;
        ShowStatusEffectIndicators = PlayerPrefs.GetInt("ShowStatusEffectIndicators", 1) == 1;
        if (damageIndicatorsToggle != null)
        {
            damageIndicatorsToggle.isOn = ShowDamageIndicators;
            damageIndicatorsToggle.onValueChanged.AddListener(OnDamageIndicatorsChanged);
        }
        if (healingIndicatorsToggle != null)
        {
            healingIndicatorsToggle.isOn = ShowHealingIndicators;
            healingIndicatorsToggle.onValueChanged.AddListener(OnHealingIndicatorsChanged);
        }
        if (statusEffectIndicatorsToggle != null)
        {
            statusEffectIndicatorsToggle.isOn = ShowStatusEffectIndicators;
            statusEffectIndicatorsToggle.onValueChanged.AddListener(OnStatusEffectIndicatorsChanged);
        }
    }

    public void OnDamageIndicatorsChanged(bool value)
    {
        ShowDamageIndicators = value;
        PlayerPrefs.SetInt("ShowDamageIndicators", value ? 1 : 0);
    }

    public void OnHealingIndicatorsChanged(bool value)
    {
        ShowHealingIndicators = value;
        PlayerPrefs.SetInt("ShowHealingIndicators", value ? 1 : 0);
    }

    public void OnStatusEffectIndicatorsChanged(bool value)
    {
        ShowStatusEffectIndicators = value;
        PlayerPrefs.SetInt("ShowStatusEffectIndicators", value ? 1 : 0);
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

    public void OnAmbientChanged(float value)
    {
        ApplyVolume("AmbientVolume", value);
        PlayerPrefs.SetFloat("AmbientVolume", value);
        if (ambientText != null) ambientText.text = $"Ambient: {Mathf.RoundToInt(value * 100)}%";
    }

    private void ApplyVolume(string parameter, float value)
    {
        audioMixer.SetFloat(parameter, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }
}
