using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InsectInfoUI : MonoBehaviour
{
    public static InsectInfoUI instance;

    [Header("Panel")]

    [SerializeField] private GameObject panel;

    [Header("Header")]
    [SerializeField] private Image insectIcon;
    [SerializeField] private TMP_Text insectNameText;

    [Header("Stats")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text movementSpeedText;
    [SerializeField] private TMP_Text attackDamageText;
    [SerializeField] private TMP_Text sunYieldText;

    private Insect selectedInsect;
    private SpriteRenderer cachedRenderer;

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame && selectedInsect != null)
        {
            HidePanel();
        }

        if (selectedInsect != null)
        {
            RefreshStats();
        }
    }

    public void ShowPanel(Insect insect)
    {
        PlantUpgradeUI.instance.HidePanel();
        selectedInsect = insect;
        cachedRenderer = insect.GetComponent<SpriteRenderer>();
        panel.SetActive(true);
        insectNameText.text = insect.GetName();
        insectIcon.sprite = cachedRenderer.sprite;
        RefreshStats();
    }

    public void HidePanel()
    {
        selectedInsect = null;
        cachedRenderer = null;
        panel.SetActive(false);
        PlantUpgradeUI.instance.HideTooltip();
    }

    public Insect GetSelectedInsect() {
        return selectedInsect;
    }

    private void RefreshStats()
    {
        healthText.text       = $"HP:  {selectedInsect.health:F0} / {selectedInsect.maxHealth:F0}";
        movementSpeedText.text = $"SPD: {selectedInsect.movementSpeed:F2}";
        attackDamageText.text  = $"ATK: {selectedInsect.attackDamage:F0}";
        sunYieldText.text      = $"SUN: {selectedInsect.sunDrop}";
    }

}
