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

    // length of the countdown now running, taken from its first value. negative: none running
    private float countdownFull = -1f;
    private float lastCountdown;
    private string shownCountdownText;

    void Awake()
    {
        if (instance != null) return;
        instance = this;
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
        ShowCountdown($"Next wave {Mathf.CeilToInt(t)}s", CountdownLeft(t));
    }
    // final wave has no next wave to count down to, so this counts down to its last insect spawn instead
    public void SetFinalWaveTimer(float t)
    {
        NextWaveCountdown = t;
        if (t <= 0f) { countdownFull = -1f; ShowCountdown("", 0f); return; }
        ShowCountdown($"Last spawn {Mathf.CeilToInt(t)}s", CountdownLeft(t));
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
