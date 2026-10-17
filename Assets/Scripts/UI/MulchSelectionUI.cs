using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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

    void Awake()
    {
        if (instance != null) return;
        instance = this;
        panel.SetActive(false);
    }

    public bool IsOpen => panel.activeSelf;

    public void Show()
    {
        MulchData[] choices = MulchManager.instance?.GenerateChoicesForOldest();
        if (choices == null) return;

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        _timeScaleBeforeShow = Time.timeScale;
        Time.timeScale = 0f;

        ClearCards();

        foreach (MulchData choice in choices)
        {
            MulchCard card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(choice, this);
            activeCards.Add(card);
        }
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
        Close();
    }

    // backing out without choosing costs nothing - the queued grant isn't consumed until a card
    // is actually picked (see MulchManager.CommitChosen)
    private void Close()
    {
        Time.timeScale = _timeScaleBeforeShow;
        panel.SetActive(false);
        ClearCards();
        MulchQueueButton.instance?.Refresh();
    }

    public void CloseAfterSelect()
    {
        Time.timeScale = _timeScaleBeforeShow;
        panel.SetActive(false);
        ClearCards();
        MulchQueueButton.instance?.Refresh();
    }

    // cleanup used when leaving to the Main Menu rather than from an actual pick - same shape as
    // FertilizerSelectionUI.CloseForSceneExit
    public void CloseForSceneExit()
    {
        Time.timeScale = _timeScaleBeforeShow;
        panel.SetActive(false);
        ClearCards();
    }
}
