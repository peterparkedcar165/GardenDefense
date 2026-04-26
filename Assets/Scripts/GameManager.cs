using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;
    public int SunCount;
    public int playerHealth, playerMaxHealth = 20;
    public float BonusSunGain;
    
    public TextMeshProUGUI sunText;

    protected void Awake()
    {
        if (instance == null)
        {
            instance = this;
            SunCount = 125;
            UpdateSun();
            DontDestroyOnLoad(gameObject);
            playerHealth = playerMaxHealth;
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

    private void UpdateSun()
    {
        if (sunText != null)
        {
            sunText.text = "Sun: " + SunCount;
        }
    }

    public void Damage(int damage)
    {
        playerHealth -= damage;
        Debug.Log("Player health: " + playerHealth);
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
}
