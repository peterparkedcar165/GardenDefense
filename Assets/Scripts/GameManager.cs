using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{

    static GameManager instance;
    public int SunCount;
    public float BonusSunGain;
    
    public TextMeshProUGUI sunText;

    protected void Awake()
    {
        if (instance == null)
        {
            instance = this;
            SunCount = 200;
            UpdateSun();
            DontDestroyOnLoad(gameObject);
        } else
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
}
