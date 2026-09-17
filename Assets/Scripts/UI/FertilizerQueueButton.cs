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
    [SerializeField] private float flashSpeed = 3f;
    [SerializeField, Range(0f, 1f)] private float minFlashAlpha = 0.25f;

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

        if (icon != null)
        {
            if (hasAny)
            {
                // pulses alpha rather than tinting hue - a brightness blink reads as "flashing"
                // much more clearly than a subtle color swap would. unscaled so it keeps flashing
                // even while the game is paused (e.g. behind the mid-level fertilizer picker
                // itself, or the pre-level pause)
                float t = (Mathf.Sin(Time.unscaledTime * flashSpeed) + 1f) * 0.5f;
                Color c = litColor;
                c.a = Mathf.Lerp(minFlashAlpha, 1f, t);
                icon.color = c;
            }
            else
            {
                icon.color = blackedOutColor;
            }
        }
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
