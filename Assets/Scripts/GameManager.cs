using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

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

    [SerializeField] private TMP_Text pauseButtonText;
    [SerializeField] private TMP_Text speedButtonText;
    
    public TextMeshProUGUI sunText, healthText;

    protected void Awake()
    {
        if (instance == null)
        {
            instance = this;
            SunCount = 250; // originally 125
            UpdateSun();
            DontDestroyOnLoad(gameObject);
            playerMaxHealth = 999;
            playerHealth = playerMaxHealth;
            UpdateHealth();
        }
        else
        {
            Destroy(gameObject);
        }
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
            healthText.text = "Health: " + playerHealth;
        }
    }

    public void Damage(int damage)
    {
        playerHealth -= damage;
        Debug.Log("Player health: " + playerHealth);
        UpdateHealth();
        if (playerHealth < 0)
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
        gameSpeed = gameSpeed == 1f ? 2f : 1f;
        if (!paused)
        {
            Time.timeScale = gameSpeed;
        }
        if (speedButtonText != null)
        {
            speedButtonText.text = gameSpeed == 1f ? "1x" : "2x";
        }
    }

}
