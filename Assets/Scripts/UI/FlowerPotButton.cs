using UnityEngine;
using TMPro;
using FontStyles = TMPro.FontStyles;

public class FlowerPotButton : MonoBehaviour
{
    [SerializeField] private TMP_Text sunCostText;
    // the cost the text shows, so it is only turned into text again when it changes
    private int shownCost = int.MinValue;

    void Update()
    {
        if (sunCostText == null || GameManager.instance == null) return;
        if (FlowerPot.SunCost != shownCost)
        {
            shownCost = FlowerPot.SunCost;
            sunCostText.text = shownCost.ToString();
        }
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
