using UnityEngine;
using TMPro;
using FontStyles = TMPro.FontStyles;

public class WaterPotButton : MonoBehaviour
{
    [SerializeField] private TMP_Text sunCostText;
    // the cost the text shows, so it is only turned into text again when it changes
    private int shownCost = int.MinValue;

    void Update()
    {
        if (sunCostText == null || GameManager.instance == null) return;
        if (WaterPot.SunCost != shownCost)
        {
            shownCost = WaterPot.SunCost;
            sunCostText.text = shownCost.ToString();
        }
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
