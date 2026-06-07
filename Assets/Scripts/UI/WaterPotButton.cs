using UnityEngine;
using TMPro;
using FontStyles = TMPro.FontStyles;

public class WaterPotButton : MonoBehaviour
{
    [SerializeField] private TMP_Text sunCostText;

    void Update()
    {
        if (sunCostText == null || GameManager.instance == null) return;
        sunCostText.text = WaterPot.SunCost.ToString();
        if (GameManager.instance.SunCount >= WaterPot.SunCost)
        {
            sunCostText.fontStyle = FontStyles.Bold;
            sunCostText.color = Color.green;
        }
        else
        {
            sunCostText.fontStyle = FontStyles.Normal;
            sunCostText.color = Color.red;
        }
    }
}
