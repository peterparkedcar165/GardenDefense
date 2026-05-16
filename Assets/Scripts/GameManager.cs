using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;
    public int SunCount;
    public int playerHealth;
    [System.NonSerialized] public int playerMaxHealth = 200;
    public float BonusSunGain;
    public AudioSource audioSource;
    public AudioClip buttonClick, plantPlace, plantSelect;
    private bool paused = false;
    public float gameSpeed = 1f;
    private float[] speeds = { 0.5f, 1f, 2f, 4f };
    private int speedIndex = 1; // starts at 1x
    public int currentWave = 0;

    protected void Awake()
    {
        instance = this;
    }

    public void AddSun(int amount)
    {
        SunCount += Mathf.RoundToInt(amount * (1 + BonusSunGain));
        UpdateSun();
    }

    public bool SpendSun(int amount)
    {
        if (SunCount >= amount)
        {
            SunCount -= amount;
            Debug.Log("Successfully spent: " + amount + " of suns");
            UpdateSun();
            return true;
        }
        Debug.Log("Not enough sun!");
        return false;
    }

    public void UpdateSun() => GameHUD.instance?.SetSun(SunCount);

    public void UpdateHealth()
    {
        if (playerHealth < 0) playerHealth = 0;
        GameHUD.instance?.SetHealth(playerHealth);
    }

    public void Damage(int damage)
    {
        playerHealth -= damage;
        Debug.Log("Player health: " + playerHealth);
        UpdateHealth();
        if (playerHealth <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        Debug.Log("Game Over");
        Time.timeScale = 0; // pauses game
        // show game over screen later
    }

    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (SettingsManager.instance == null || !SettingsManager.instance.IsOpen)
                if (FertilizerSelectionUI.instance == null || !FertilizerSelectionUI.instance.IsOpen)
                    TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPause(!paused);
    }

    public void SetPause(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : gameSpeed;
        GameHUD.instance?.SetPauseButton(paused);
    }

    public void ToggleSpeed()
    {
        speedIndex = (speedIndex + 1) % speeds.Length;
        gameSpeed = speeds[speedIndex];
        if (!paused) Time.timeScale = gameSpeed;
        GameHUD.instance?.SetSpeedButton(gameSpeed);
    }

    public bool IsGameActive { get; private set; }

    public void InitiateLevel(int sunCount, int health)
    {
        IsGameActive = true;
        SunCount = sunCount;
        playerMaxHealth = health;
        playerHealth = playerMaxHealth;
        UpdateSun();
        UpdateHealth();
        PersistentCanvas.instance?.Show();
        FertilizerSelectionUI.instance.Show();
    }

    public void Restart()
    {
        Debug.Log($"[Restart] Sun: {SunCount} | Health: {playerHealth}");
        Time.timeScale = 1f;
        paused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        paused = false;
        SceneManager.LoadScene("MainMenu");
    }

}
