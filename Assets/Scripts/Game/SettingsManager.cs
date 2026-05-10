using UnityEngine;
using UnityEngine.Audio;
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

    public bool IsOpen => settingsPanel.activeSelf;

    private void Awake()
    {
        instance = this;
        settingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !SkillTargetingManager.instance.WasCancelledThisFrame)
        {
            bool isOpen = !settingsPanel.activeSelf;
            settingsPanel.SetActive(isOpen);
            GameManager.instance.SetPause(isOpen);
        }
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
