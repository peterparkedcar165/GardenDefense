using UnityEngine;
using UnityEngine.UI;
using TMPro;

// canvas button showing how many mulches are queued - blacked out at 0, lit up with a green
// count once the player has at least one. clicking opens the mulch draft. same behavior as
// FertilizerQueueButton
public class MulchQueueButton : MonoBehaviour
{
    public static MulchQueueButton instance;

    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button button;
    [SerializeField] private Color litColor = Color.white;
    [SerializeField] private Color blackedOutColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private float flashSpeed = 3f;
    [SerializeField, Range(0f, 1f)] private float minFlashAlpha = 0.25f;

    void Awake()
    {
        // lives on the persistent Canvas (see PersistentCanvas) - every level scene also carries
        // its own throwaway copy whose Awake still runs once before PersistentCanvas destroys it,
        // so this guard keeps it from stealing the static reference from the real, surviving one
        if (instance != null) return;
        instance = this;
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        int count = MulchManager.instance != null ? MulchManager.instance.PendingMulchCount : 0;
        bool hasAny = count > 0;

        if (icon != null)
        {
            if (hasAny)
            {
                // pulses alpha rather than tinting hue, same as FertilizerQueueButton. unscaled
                // so it keeps flashing even while the game is paused behind the picker itself
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
        TutorialManager.instance?.NotifyEvent(TutorialEvents.ClickMulchQueueButton);
        MulchSelectionUI.instance?.Show();
    }
}
