using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class FertilizerSelectionUI : MonoBehaviour
{
    public static FertilizerSelectionUI instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private FertilizerCard cardPrefab;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private FertilizerData[] fertilizerPool;

    private List<FertilizerCard> activeCards = new List<FertilizerCard>();

    // true while showing the mid-level (procedurally generated) 3-choice flow instead of the
    // pre-level full-pool pick - changes what closing the panel does (resume vs. stay paused)
    // and what Escape does (just close vs. back to loadout)
    private bool isMidLevelMode;
    private float _timeScaleBeforeMidLevel = 1f;

    void Awake()
    {
        if (instance != null) return;
        instance = this;
        panel.SetActive(false);
    }

    public void Configure(FertilizerData[] pool)
    {
        fertilizerPool = pool;
    }

    public bool IsOpen => panel.activeSelf;

    public void Show()
    {
        Debug.Log("[Restart] Fertilizer window opened");
        isMidLevelMode = false;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        GameManager.instance.SetPause(true);

        ClearCards();

        List<FertilizerData> picks = PickRandom(fertilizerPool, fertilizerPool.Length);
        for (int i = 0; i < picks.Count; i++)
        {
            FertilizerCard card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(picks[i], this);
            activeCards.Add(card);
        }
    }

    // mid-level flow: 3 randomly rolled choices for the oldest queued fertilizer grant (see
    // FertilizerManager.GenerateChoicesForOldest). unlike Show(), this pauses via a plain
    // Time.timeScale save/restore rather than GameManager.SetPause, since the level is actively
    // running here (not sitting at wave 0) and closing should resume play immediately
    public void ShowMidLevel()
    {
        GeneratedFertilizer[] choices = FertilizerManager.instance?.GenerateChoicesForOldest();
        if (choices == null) return;

        isMidLevelMode = true;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        _timeScaleBeforeMidLevel = Time.timeScale;
        Time.timeScale = 0f;

        ClearCards();

        foreach (GeneratedFertilizer choice in choices)
        {
            FertilizerCard card = Instantiate(cardPrefab, cardContainer);
            card.InitializeGenerated(choice, this);
            activeCards.Add(card);
        }
    }

    private void ClearCards()
    {
        foreach (var card in activeCards)
            Destroy(card.gameObject);
        activeCards.Clear();
    }

    void Update()
    {
        if (!IsOpen || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (isMidLevelMode) CloseMidLevel();
        else BackToLoadout();
    }

    private void BackToLoadout()
    {
        panel.SetActive(false);
        ClearCards();
        LoadoutSelectionUI.instance?.ShowWithCurrentSelection();
    }

    // backing out of the mid-level picker without choosing costs nothing - the queued grant
    // isn't consumed until a card is actually picked (see FertilizerManager.CommitGenerated)
    private void CloseMidLevel()
    {
        Time.timeScale = _timeScaleBeforeMidLevel;
        panel.SetActive(false);
        ClearCards();
        FertilizerQueueButton.instance?.Refresh();
    }

    // leaves the game paused after a PRE-LEVEL fertilizer selection - wave 0 now waits for the
    // player to press Space or the Pause/Resume button (labelled START while
    // GameManager.HasStarted is false) instead of unpausing automatically the moment a
    // fertilizer is picked. the mid-level flow instead resumes immediately, since the level is
    // already running and there's no equivalent "waiting to start" state to fall back into
    public void CloseAfterSelect()
    {
        panel.SetActive(false);
        ClearCards();
        if (isMidLevelMode)
        {
            Time.timeScale = _timeScaleBeforeMidLevel;
            FertilizerQueueButton.instance?.Refresh();
        }
    }

    private List<FertilizerData> PickRandom(FertilizerData[] pool, int count)
    {
        List<FertilizerData> ordered = new List<FertilizerData>(pool);
        count = Mathf.Min(count, ordered.Count);
        return ordered.GetRange(0, count);
    }
}
