using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class EncyclopediaManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject plantsPanel;
    [SerializeField] private GameObject insectsPanel;
    [SerializeField] private GameObject miscPanel;

    [Header("Sub-panels")]
    [SerializeField] private PlantsPanelUI plantsPanelUI;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;

    private GameObject currentPanel;

    void Start()
    {
        ShowPanel(mainPanel, "Encyclopedia");
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !SceneTransition.IsTransitioning)
            OnBack();
    }

    public void OnPlants()
    {
        ShowPanel(plantsPanel, "Plants");
    }

    public void OnInsects()
    {
        ShowPanel(insectsPanel, "Insects");
    }

    public void OnMiscellaneous()
    {
        ShowPanel(miscPanel, "Miscellaneous");
    }

    public void OnBack()
    {
        if (plantsPanelUI != null && plantsPanelUI.TryUnpin())
            return;

        if (currentPanel != mainPanel)
            ShowPanel(mainPanel, "Encyclopedia");
        else
            GoToMainMenu();
    }

    private void ShowPanel(GameObject panel, string title = "")
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        currentPanel = panel;
        currentPanel.SetActive(true);

        if (titleText)
            titleText.text = title;
    }

    private void GoToMainMenu()
    {
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        if (transition != null)
            transition.StartCoroutine(transition.FadeToScene("MainMenu"));
    }
}
