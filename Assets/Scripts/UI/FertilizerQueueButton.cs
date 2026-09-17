using UnityEngine;
using UnityEngine.UI;
using TMPro;

// canvas button showing how many mid-level fertilizers are queued - blacked out at 0, lit up
// with a green count once the player has at least one. clicking opens FertilizerChoicePopup
public class FertilizerQueueButton : MonoBehaviour
{
    public static FertilizerQueueButton instance;

    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button button;
    [SerializeField] private Color litColor = Color.white;
    [SerializeField] private Color blackedOutColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        int count = FertilizerManager.instance != null ? FertilizerManager.instance.PendingFertilizerCount : 0;
        bool hasAny = count > 0;

        if (icon != null) icon.color = hasAny ? litColor : blackedOutColor;
        if (countText != null)
        {
            countText.gameObject.SetActive(hasAny);
            countText.text = count.ToString();
            countText.color = Color.green;
        }
        if (button != null) button.interactable = hasAny;
    }

    public void OnClicked()
    {
        FertilizerSelectionUI.instance?.ShowMidLevel();
    }
}
