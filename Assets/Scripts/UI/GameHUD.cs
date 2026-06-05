using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    public static GameHUD instance;

    [SerializeField] public TextMeshProUGUI sunText, healthText, waveCountText, nextWaveTimerText;
    [SerializeField] public TMP_Text pauseButtonText, speedButtonText;

    void Awake()
    {
        if (instance != null) return;
        instance = this;
    }

    public void SetSun(int count)            { if (sunText)           sunText.text           = $"Sun: {count}"; }
    public void SetHealth(int hp)            { if (healthText)        healthText.text        = $"Health: {hp}"; }
    public void SetWaveCount(int w, int max) { if (waveCountText)     waveCountText.text     = $"Wave: {w}/{max}"; }
    // seconds until the next wave (set every frame by the level); +inf before the level starts ticking
    public float NextWaveCountdown { get; private set; } = float.PositiveInfinity;
    public void SetNextWaveTimer(float t)    { NextWaveCountdown = t; if (nextWaveTimerText) nextWaveTimerText.text = t < 0f ? "Final Wave" : $"Next wave in {Mathf.CeilToInt(Mathf.Max(0, t))}s"; }
    public void SetPauseButton(bool paused)  { if (pauseButtonText)   pauseButtonText.text   = paused ? "Resume" : "Pause"; }
    public void SetSpeedButton(float speed)  { if (speedButtonText)   speedButtonText.text   = speed + "x"; }
}
