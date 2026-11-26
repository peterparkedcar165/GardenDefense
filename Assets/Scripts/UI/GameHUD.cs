using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    public static GameHUD instance;

    [SerializeField] public TextMeshProUGUI sunText, healthText, waveCountText, nextWaveTimerText;
    [SerializeField] public TMP_Text pauseButtonText, speedButtonText;

    [Header("Game controls (Downloads\\gd_game_controls)")]
    [Tooltip("start, pause and resume: its colours, icon and word. without it only pauseButtonText changes")]
    [SerializeField] private GameControlButton pauseButton;
    [Tooltip("its colours, icon and label. without it only speedButtonText changes")]
    [SerializeField] private GameControlButton speedButton;
    [Tooltip("the countdown bar's fill and its shine, inside the bar's 1 pixel frame. they shrink from the left " +
        "end as the next wave nears")]
    [SerializeField] private Image nextWaveFill, nextWaveFillShine;

    // canvas units per art pixel of the game controls
    private const float Px = 2f;

    [System.Serializable]
    public struct BiomeWood
    {
        public Biome biome;
        [Tooltip("the right panel's wood tint (its greyscale wood times this)")]
        public Color wood;
        public BiomeWood(Biome biome, string hex)
        {
            this.biome = biome;
            wood = ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }
    }

    [Header("The right panel's wood colour per biome (Tommy, 2026-11-25)")]
    [Tooltip("the right panel's wood (UIScreen's Image); found by name next to this when empty")]
    [SerializeField] private Image rightPanel;
    [Tooltip("Tommy's colours by name; the exact shades, desert night and underwater are proposals")]
    [SerializeField] private BiomeWood[] biomeWood =
    {
        new BiomeWood(Biome.Daytime, "#557D46"),      // green (the panel spec's leaf green)
        new BiomeWood(Biome.Nighttime, "#3E5285"),    // dark blue
        new BiomeWood(Biome.Desert, "#C8A878"),       // beige
        new BiomeWood(Biome.DesertNight, "#8A7455"),  // dark beige
        new BiomeWood(Biome.Cave, "#965C3C"),         // brown (the spec's own wood colour)
        new BiomeWood(Biome.Snow, "#E8EEF2"),         // white
        new BiomeWood(Biome.Underwater, "#506EB4"),   // the spec's crystal blue
    };

    // the right panel's wood colour for a level's biome; false when the biome has none
    public bool TryGetWood(LevelConfig config, out Color wood)
    {
        wood = Color.white;
        if (config == null || biomeWood == null) return false;
        Biome biome = config.CurrentBiome;
        foreach (BiomeWood entry in biomeWood)
            if (entry.biome == biome) { wood = entry.wood; return true; }
        return false;
    }

    // called by the level once it's set up (ProceduralLevel.Start): the right panel's wood takes the
    // level's biome colour. a biome without a colour leaves the panel as the prefab has it
    public void TintRightPanel(LevelConfig config)
    {
        if (config == null) return;
        if (rightPanel == null && transform.parent != null)
        {
            Transform found = transform.parent.Find("UIScreen");
            if (found != null) rightPanel = found.GetComponent<Image>();
        }
        if (rightPanel == null || biomeWood == null) return;
        Biome biome = config.CurrentBiome;
        foreach (BiomeWood entry in biomeWood)
            if (entry.biome == biome) { rightPanel.color = entry.wood; return; }
    }

    // length of the countdown now running, taken from its first value. negative: none running
    private float countdownFull = -1f;
    private float lastCountdown;
    private string shownCountdownText;

    void Awake()
    {
        if (instance != null) return;
        instance = this;
        // the timer bar is also the button that calls the next wave early (WaveCallBar)
        if (nextWaveFill != null && nextWaveFill.rectTransform.parent is RectTransform bar)
            WaveCallBar.Attach(bar, nextWaveFill, waveCountText);
    }

    public void SetSun(int count)            { if (sunText)           sunText.text           = $"{count}"; }
    public void SetHealth(int hp)            { if (healthText)        healthText.text        = $"{hp}"; }
    // "Wave 3/8" at the timer bar's left end, its drawn pixels GameControlsStyle.timerTextInset in
    public void SetWaveCount(int w, int max)
    {
        if (!waveCountText) return;
        waveCountText.text = $"Wave {w}/{max}";
        PixelLayout.PlaceInkLeft(waveCountText, Px, GameControlsStyle.Instance.timerTextInset);
    }
    // seconds until the next wave (set every frame by the level); +inf before the level starts ticking
    public float NextWaveCountdown { get; private set; } = float.PositiveInfinity;
    public void SetNextWaveTimer(float t)
    {
        NextWaveCountdown = t;
        // before the first wave there's no countdown yet: the bar waits full and says nothing
        if (float.IsPositiveInfinity(t)) { countdownFull = -1f; ShowCountdown("", 1f); return; }
        if (t < 0f) { countdownFull = -1f; ShowCountdown("Final wave", 0f); return; }
        ShowCountdown(CountdownText(false, Mathf.CeilToInt(t)), CountdownLeft(t));
    }
    // final wave has no next wave to count down to, so this counts down to its last insect spawn instead
    public void SetFinalWaveTimer(float t)
    {
        NextWaveCountdown = t;
        if (t <= 0f) { countdownFull = -1f; ShowCountdown("", 0f); return; }
        ShowCountdown(CountdownText(true, Mathf.CeilToInt(t)), CountdownLeft(t));
    }

    // the countdown text last built. these timers are set every frame, so the text is only made
    // again when its whole seconds (or which countdown it is) change
    private bool builtLastSpawn;
    private int builtSeconds;
    private string builtCountdownText;

    private string CountdownText(bool lastSpawn, int seconds)
    {
        if (builtCountdownText == null || lastSpawn != builtLastSpawn || seconds != builtSeconds)
        {
            builtLastSpawn = lastSpawn;
            builtSeconds = seconds;
            builtCountdownText = lastSpawn ? $"Last spawn {seconds}s" : $"Next wave {seconds}s";
        }
        return builtCountdownText;
    }

    public void SetPauseButton(bool paused, bool hasStarted)
    {
        string label = !hasStarted ? "Start" : paused ? "Resume" : "Pause";
        if (pauseButton != null)
        {
            GameControlsStyle style = GameControlsStyle.Instance;
            pauseButton.Set(!hasStarted ? style.start : paused ? style.resume : style.pause,
                paused ? style.playIcon : style.pauseIcon, paused ? style.playIconTop : style.pauseIconTop, label);
        }
        else if (pauseButtonText) pauseButtonText.text = label;
    }

    // space held down shows the start / pause / resume button pressed (GameManager presses it on release)
    public void HoldPauseButton(bool held)
    {
        if (pauseButton != null) pauseButton.SetKeyHeld(held);
    }

    public void SetSpeedButton(float speed)
    {
        string label = speed + "x";
        if (speedButton != null)
        {
            GameControlsStyle style = GameControlsStyle.Instance;
            speedButton.Set(style.speed, style.speedIcon, style.speedIconTop, label);
        }
        else if (speedButtonText) speedButtonText.text = label;
    }

    // share of the running countdown still left. a countdown starts when the seconds jump up (a new
    // wave set them), and its first value is its full length, so 24 of 60 seconds left is 40%
    private float CountdownLeft(float t)
    {
        if (countdownFull <= 0f || t > lastCountdown + 0.05f) countdownFull = t;
        lastCountdown = t;
        return countdownFull > 0f ? Mathf.Clamp01(t / countdownFull) : 0f;
    }

    private void ShowCountdown(string text, float fraction)
    {
        SetFill(nextWaveFill, fraction);
        SetFill(nextWaveFillShine, fraction);
        if (!nextWaveTimerText || text == shownCountdownText) return;
        shownCountdownText = text;
        nextWaveTimerText.text = text;
        // at the bar's right end, its drawn pixels the same inset in
        PixelLayout.PlaceInkRight(nextWaveTimerText, Px, GameControlsStyle.Instance.timerTextInset);
    }

    // the fill is anchored at the bar's left inner edge: whole art pixels wide out of the bar's inner width
    private static void SetFill(Image fill, float fraction)
    {
        if (fill == null) return;
        RectTransform rect = fill.rectTransform;
        RectTransform bar = rect.parent as RectTransform;
        if (bar == null) return;
        float inner = Mathf.Max(0f, Mathf.Round(bar.rect.width / Px) - 2f);
        float width = Mathf.Floor(inner * Mathf.Clamp01(fraction)) * Px;
        if (!Mathf.Approximately(rect.sizeDelta.x, width)) rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        bool show = width > 0f;
        if (fill.enabled != show) fill.enabled = show;
    }
}
