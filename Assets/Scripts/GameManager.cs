using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;
    public int SunCount;
    public int playerHealth, playerMaxHealth; // originally 20
    public float BonusSunGain;
    public AudioSource audioSource;
    public AudioClip buttonClick, plantPlace, plantSelect;
    private bool paused = false;
    public float gameSpeed = 1f;
    private float[] speeds = { 0.5f, 1f, 2f, 4f };
    private int speedIndex = 1; // starts at 1x
    public int currentWave = 0;

    [SerializeField] private TMP_Text pauseButtonText;
    [SerializeField] private TMP_Text speedButtonText;
    
    [SerializeField] private TextMeshProUGUI sunText, healthText;

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

    public void UpdateSun()
    {
        if (sunText != null)
        {
            sunText.text = "Sun: " + SunCount;
        }
    }

    public void UpdateHealth()
    {
        if (healthText != null)
        {
            if (playerHealth < 0)
            {
                playerHealth = 0;
            }

            healthText.text = "Health: " + playerHealth;
        }
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
            TogglePause();
        }
    }

    public void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f: gameSpeed;
        if (pauseButtonText != null)
        {
            pauseButtonText.text = paused ? "Resume" : "Pause";
        }
    }

    public void ToggleSpeed()
    {
        speedIndex = (speedIndex + 1) % speeds.Length;
        gameSpeed = speeds[speedIndex];
        if (!paused) Time.timeScale = gameSpeed;
        if (speedButtonText != null)
        speedButtonText.text = gameSpeed + "x";
    }

    public void InitiateLevel(int sunCount, int health)
    {
        SunCount = sunCount;
        playerMaxHealth = health;
        playerHealth = playerMaxHealth;
        UpdateSun();
        UpdateHealth();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        paused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
