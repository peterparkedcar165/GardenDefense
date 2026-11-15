using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

// mulch counterpart to FertilizerSelectionUI - mulch has no pre-level pick, only the mid-level
// drafted-queue flow, so this is just that half of FertilizerSelectionUI (ShowMidLevel/
// CloseMidLevel), pausing the same way via a plain Time.timeScale save/restore
public class MulchSelectionUI : MonoBehaviour
{
    public static MulchSelectionUI instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private MulchCard cardPrefab;
    [SerializeField] private Transform cardContainer;

    private readonly List<MulchCard> activeCards = new List<MulchCard>();
    private float _timeScaleBeforeShow = 1f;

    // seed swap and seed sell's list of owned mulches, built in code on top of the open draft (see
    // ShowTargetPicker). null while it isn't showing
    private GameObject targetPicker;
    private const float PickerWidth = 760f;

    void Awake()
    {
        if (instance != null) return;
        instance = this;
        panel.SetActive(false);
    }

    public bool IsOpen => panel.activeSelf;
    public bool IsTargetPickerOpen => targetPicker != null;

    public void Show()
    {
        MulchData[] choices = MulchManager.instance?.GenerateChoicesForOldest();
        if (choices == null) return;

        // seed swap opens its replacement draft while the panel is still open, so the time scale
        // saved when the panel first opened must be kept, not overwritten with the paused 0
        if (!panel.activeSelf)
        {
            _timeScaleBeforeShow = Time.timeScale;
            Time.timeScale = 0f;
        }
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        HideTargetPicker();
        ClearCards();

        foreach (MulchData choice in choices)
        {
            MulchCard card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(choice, this);
            activeCards.Add(card);
        }

        // fewer eligible mulches than a full draft (already-picked ones excluded, conditions not
        // met, etc.) - pad the remaining slots with a placeholder instead of just showing fewer
        for (int i = choices.Length; i < MulchManager.DraftChoiceCount; i++)
        {
            MulchCard card = Instantiate(cardPrefab, cardContainer);
            card.InitializeEmpty();
            activeCards.Add(card);
        }
    }

    // every mulch currently showing on another card in this draft - fed into RerollSingle so a
    // reroll can never land on a duplicate of what's already displayed elsewhere on screen, even
    // though that mulch is still otherwise eligible (it just hasn't been picked or rerolled away)
    public List<MulchData> GetDisplayedMulches(MulchCard excluding)
    {
        List<MulchData> result = new List<MulchData>();
        foreach (MulchCard card in activeCards)
        {
            if (card == null || card == excluding) continue;
            if (card.CurrentData != null) result.Add(card.CurrentData);
        }
        return result;
    }

    private void ClearCards()
    {
        foreach (MulchCard card in activeCards)
            Destroy(card.gameObject);
        activeCards.Clear();
    }

    void Update()
    {
        if (!IsOpen || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame || TutorialManager.BlocksEscape) return;
        // escape backs out of the owned mulch list first, back to the draft it came from
        if (IsTargetPickerOpen) HideTargetPicker();
        else Close();
    }

    // backing out without choosing costs nothing - the queued grant isn't consumed until a card
    // is actually picked (see MulchManager.CommitChosen)
    private void Close()
    {
        Time.timeScale = _timeScaleBeforeShow;
        HideTargetPicker();
        panel.SetActive(false);
        ClearCards();
        MulchQueueButton.instance?.Refresh();
    }

    public void CloseAfterSelect()
    {
        Time.timeScale = _timeScaleBeforeShow;
        HideTargetPicker();
        panel.SetActive(false);
        ClearCards();
        MulchQueueButton.instance?.Refresh();
    }

    // cleanup used when leaving to the Main Menu rather than from an actual pick - same shape as
    // FertilizerSelectionUI.CloseForSceneExit
    public void CloseForSceneExit()
    {
        Time.timeScale = _timeScaleBeforeShow;
        HideTargetPicker();
        panel.SetActive(false);
        ClearCards();
    }

    // seed swap and seed sell: a dimmed overlay on the draft with one row per owned mulch the tool
    // may take away. picking a row spends the tool on it; cancel (or escape) returns to the draft
    // with nothing spent
    public void ShowTargetPicker(MulchData tool)
    {
        if (tool == null || MulchManager.instance == null) return;
        HideTargetPicker();
        TMP_FontAsset font = cardPrefab != null ? cardPrefab.Font : null;

        targetPicker = NewUIObject("MulchTargetPicker", panel.transform);
        Stretch((RectTransform)targetPicker.transform);
        targetPicker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject window = NewUIObject("Window", targetPicker.transform);
        RectTransform windowRect = (RectTransform)window.transform;
        windowRect.anchorMin = windowRect.anchorMax = windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(PickerWidth, 0f);
        window.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.96f);
        VerticalLayoutGroup layout = window.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        window.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddText(window.transform, font, $"{tool.mulchName}\n<size=65%>{tool.TargetPrompt}</size>", 40f, TextAlignmentOptions.Center);

        foreach (MulchData owned in MulchManager.instance.GetRemovableMulches())
        {
            MulchData target = owned;
            Color tierColor = MulchTierColors.Get(target.tier);
            string text = $"{target.mulchName}  <size=70%>{MulchData.TierLabel(target.tier)}</size>\n" +
                $"<size=65%>{target.GetDescription()}</size>\n<size=75%>{tool.DescribeTargetUse(target)}</size>";
            AddButton(window.transform, font, text, TextAlignmentOptions.MidlineLeft,
                Color.Lerp(tierColor, Color.black, 0.7f), Color.Lerp(tierColor, Color.black, 0.5f),
                () => OnTargetPicked(tool, target));
        }

        AddButton(window.transform, font, "Cancel", TextAlignmentOptions.Center,
            new Color(0.22f, 0.22f, 0.22f), new Color(0.35f, 0.35f, 0.35f), HideTargetPicker);
    }

    public void HideTargetPicker()
    {
        if (targetPicker != null) Destroy(targetPicker);
        targetPicker = null;
    }

    private void OnTargetPicked(MulchData tool, MulchData target)
    {
        HideTargetPicker();
        if (MulchManager.instance == null || !MulchManager.instance.CommitChosenOnTarget(tool, target)) return;
        TutorialManager.instance?.NotifyEvent(TutorialEvents.SelectRandomMulch);

        // seed swap: its replacement draft now sits at the front of the queue
        if (tool.OpensDraftAfterUse && MulchManager.instance.PendingMulchCount > 0) Show();
        else CloseAfterSelect();
    }

    private static GameObject NewUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddText(Transform parent, TMP_FontAsset font, string text, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = NewUIObject("Text", parent).AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.fontSize = size;
        label.richText = true;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = text;
    }

    // a row that sizes itself to its text. the image stays white and the button's color tint
    // carries the actual colors, so hovering a row brightens it
    private static void AddButton(Transform parent, TMP_FontAsset font, string text, TextAlignmentOptions alignment,
        Color normal, Color highlighted, UnityAction onClick)
    {
        GameObject row = NewUIObject("Row", parent);
        Image image = row.AddComponent<Image>();
        image.color = Color.white;

        Button button = row.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = normal;
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.3f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.05f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(onClick);

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(18, 18, 12, 12);
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        AddText(row.transform, font, text, 30f, alignment);
    }
}
