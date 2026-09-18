using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

// top-level controller for the Skill Tree screen. every plant's own SkillTreePlantPanel shows
// or hides itself and manages its own nodes - this class only owns what's global: the spendable
// points readout, the Confirm/Reset buttons, and the shared tooltip box
public class SkillTreeUI : MonoBehaviour
{
    public static SkillTreeUI instance;

    [Header("Info")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Plant Panel Spawning")]
    [SerializeField] private PlantRegistry plantRegistry;
    [SerializeField] private SkillTreePlantPanel panelPrefab;
    [SerializeField] private Transform panelsContainer;

    private readonly List<SkillTreePlantPanel> panels = new List<SkillTreePlantPanel>();

    void Awake()
    {
        instance = this;
        SpawnPanels();
    }

    // one panel per plant that has an authored tree - each panel then decides for itself
    // (in its own Start) whether the plant is actually unlocked and worth showing. new plants
    // getting a tree, or a save unlocking a new plant, both need zero changes here
    private void SpawnPanels()
    {
        if (plantRegistry == null || panelPrefab == null || panelsContainer == null) return;
        foreach (PlantData data in plantRegistry.plants)
        {
            if (data == null || data.skillTree == null) continue;
            SkillTreePlantPanel panel = Instantiate(panelPrefab, panelsContainer);
            panel.Bind(data);
        }
    }

    void Start()
    {
        HideTooltip();
        RefreshAll();
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            OnBack();

        if (tooltipPanel != null && tooltipPanel.activeSelf)
            PositionTooltipAtCursor();
    }

    // called by each SkillTreePlantPanel once it decides it should be shown, so RefreshAll can
    // reach every visible panel without this controller needing to know the plant list itself
    public void RegisterPanel(SkillTreePlantPanel panel)
    {
        if (!panels.Contains(panel)) panels.Add(panel);
    }

    public void RefreshAll()
    {
        // skill points are per-plant now (see SaveData.PlantExpRecord) - this readout is just an
        // at-a-glance sum across every plant's own balance (net of anything currently staged),
        // not a spendable global pool. each panel shows its own plant's actual balance for the
        // number that matters to a purchase
        if (pointsText != null && SaveManager.instance != null)
        {
            int totalDisplay = 0;
            foreach (PlantExpRecord record in SaveManager.instance.saveData.plantExp)
                totalDisplay += SkillTreeSession.DisplaySkillPoints(record.plantName);
            pointsText.text = $"Total Unspent Skill Points: <b><color=green>{totalDisplay}</color></b>";
        }
        foreach (SkillTreePlantPanel panel in panels)
            if (panel != null) panel.Refresh();
    }

    // hooked to the Confirm button - commits every staged pick this visit into the real save
    public void OnConfirm()
    {
        SkillTreeSession.ConfirmAll();
    }

    // hooked to the Reset button - refunds everything ever spent and wipes every plant's tree,
    // staged picks included
    public void OnReset()
    {
        SkillTreeSession.ResetAll();
        RefreshAll();
    }

    // mirrors PlantUpgradeUI.ShowTooltip exactly: fixed width (set once on the panel's
    // RectTransform in the Editor, never touched here), auto-fit height only. deliberately not
    // using ContentSizeFitter/VerticalLayoutGroup - the in-level tooltip this is meant to match
    // doesn't use them either, and they fight anything that also tries to position the panel
    // (like following the cursor) or sits inside another layout-controlled container
    public void ShowTooltip(string text)
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(true);
        if (tooltipText == null) return;
        tooltipText.text = text;

        Canvas.ForceUpdateCanvases();
        RectTransform panelRect = tooltipPanel.GetComponent<RectTransform>();

        float maxHeight = Screen.height * 0.975f;
        float height = Mathf.Min(tooltipText.preferredHeight + 40f, maxHeight);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        Canvas.ForceUpdateCanvases();
        PositionTooltipAtCursor();
    }

    // sets world position directly (not anchoredPosition) so this doesn't care what the
    // parent's own pivot/anchor happens to be - anchoredPosition is measured relative to the
    // anchor point, and a mismatch between the panel's anchor and its parent's pivot produces a
    // constant, position-independent offset (this was the "always ~800px off" bug)
    //
    // the pivot itself is picked per-frame from which screen quadrant the cursor is in, so the
    // panel always grows back toward the center of the screen instead of off whichever edge the
    // cursor is near - top-left quadrant puts the cursor at the panel's top-left corner (grows
    // right/down), top-right puts it at the panel's top-right corner (grows left/down), and so
    // on for the bottom two quadrants. changing pivot via script keeps the rect's current screen
    // position, so this is safe to do every frame before repositioning
    //
    // no cursor->corner gap needed - Raycast Target is off on the tooltip's graphics, so it
    // never steals the hover off whatever node triggered it even while sitting right under the
    // cursor, which is what used to cause the show/hide flicker
    private void PositionTooltipAtCursor()
    {
        if (tooltipPanel == null || Mouse.current == null) return;
        RectTransform panelRect = tooltipPanel.GetComponent<RectTransform>();
        RectTransform parentRect = panelRect.parent as RectTransform;
        if (parentRect == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        bool right = mouseScreenPos.x >= Screen.width * 0.5f;
        bool top   = mouseScreenPos.y >= Screen.height * 0.5f;
        panelRect.pivot = new Vector2(right ? 1f : 0f, top ? 1f : 0f);

        Canvas canvas = panelRect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, mouseScreenPos, cam, out Vector3 worldPoint))
            panelRect.position = worldPoint;

        ClampToScreen(panelRect);
    }

    private void ClampToScreen(RectTransform panelRect)
    {
        Vector3[] corners = new Vector3[4];
        panelRect.GetWorldCorners(corners);
        float leftX   = corners[0].x;
        float bottomY = corners[0].y;
        float rightX  = corners[2].x;
        float topY    = corners[2].y;

        Vector3 correction = Vector3.zero;
        if (topY > Screen.height) correction.y = Screen.height - topY;
        else if (bottomY < 0f)    correction.y = -bottomY;

        if (rightX > Screen.width) correction.x = Screen.width - rightX;
        else if (leftX < 0f)       correction.x = -leftX;

        panelRect.position += correction;
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public void OnBack()
    {
        if (SkillTreeSession.HasPending)
            Debug.Log("Skill Tree: leaving with unconfirmed picks - they will be lost.");
        if (SceneTransition.IsTransitioning) return;
        SceneTransition t = FindAnyObjectByType<SceneTransition>();
        if (t != null) t.StartCoroutine(t.FadeToScene("MainMenu"));
    }
}
