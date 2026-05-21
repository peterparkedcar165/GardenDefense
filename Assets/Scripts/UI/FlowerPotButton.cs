using UnityEngine;
using TMPro;
using FontStyles = TMPro.FontStyles;

public class FlowerPotButton : MonoBehaviour
{
    [SerializeField] private TMP_Text sunCostText;

    void Update()
    {
        if (sunCostText == null || GameManager.instance == null) return;
        sunCostText.text = FlowerPot.SunCost.ToString();
        if (GameManager.instance.SunCount >= FlowerPot.SunCost)
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
