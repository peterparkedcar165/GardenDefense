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

    void Awake()
    {
        if (instance != null) return;
        instance = this;
        // the backdrop darker (Tommy, 2026-11-26: 57% then 72% then 85%), and the title moved up clear of the cards
        UnityEngine.UI.Image backdrop = panel.GetComponent<UnityEngine.UI.Image>();
        if (backdrop != null) backdrop.color = new Color(0f, 0f, 0f, BackdropAlpha);
        Transform header = panel.transform.Find("HeaderText");
        if (header is RectTransform headerRect) headerRect.anchoredPosition = new Vector2(headerRect.anchoredPosition.x, HeaderY);
        panel.SetActive(false);
    }

    private const float BackdropAlpha = 0.85f;
    private const float HeaderY = 360f;

    public void Configure(FertilizerData[] pool)
    {
        fertilizerPool = pool;
    }

    public bool IsOpen => panel.activeSelf;

    // the pre level pick: one card per fertilizer in the level's list (LevelConfig.fertilizers)
    // that the loadout can use, in list order. picking one activates it for the whole level
    public void Show()
    {
        Debug.Log("[Restart] Fertilizer window opened");
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        GameManager.instance.SetPause(true);

        if (ProceduralLevel.CurrentConfig != null && ProceduralLevel.CurrentConfig.levelNumber == 1)
            TutorialManager.instance?.TryShow(TutorialIds.Level1FertilizerSelection, TutorialContent.Level1FertilizerSelection, TutorialSpeaker.AcornKnight);

        ClearCards();

        List<FertilizerData> offered = FertilizerManager.GetOffered(fertilizerPool);
        if (offered.Count == 0)
        {
            // nothing to pick in this level: straight on to the start, as if one was picked
            CloseAfterSelect();
            return;
        }
        foreach (FertilizerData fertilizer in offered)
        {
            FertilizerCard card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(fertilizer, this);
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
        if (!IsOpen || !Keyboard.current.escapeKey.wasPressedThisFrame || TutorialManager.BlocksEscape) return;
        BackToLoadout();
    }

    private void BackToLoadout()
    {
        panel.SetActive(false);
        ClearCards();
        LoadoutSelectionUI.instance?.ShowWithCurrentSelection();
    }

    // leaves the game paused after the fertilizer selection - wave 0 waits for the player to press
    // Space or the Pause/Resume button (labelled START while GameManager.HasStarted is false)
    // instead of unpausing automatically the moment a fertilizer is picked
    public void CloseAfterSelect()
    {
        panel.SetActive(false);
        ClearCards();
        if (ProceduralLevel.CurrentConfig != null && ProceduralLevel.CurrentConfig.levelNumber == 1)
        {
            TutorialManager.instance?.TryShow(TutorialIds.Level1StartLevel, TutorialContent.Level1StartLevel, TutorialSpeaker.AcornKnight);
        }
        else if (ProceduralLevel.CurrentConfig != null && ProceduralLevel.CurrentConfig.levelNumber == 2
                 && SaveManager.instance != null && SaveManager.instance.selectedLoadout.Contains("Sunflower"))
        {
            TutorialManager.instance?.TryShow(TutorialIds.Level2StartLevel, TutorialContent.Level2StartLevel, TutorialSpeaker.Sunflower);
        }
        else if (ProceduralLevel.CurrentConfig != null && ProceduralLevel.CurrentConfig.levelNumber == 3
                 && SaveManager.instance != null && SaveManager.instance.selectedLoadout.Contains("Waterlily"))
        {
            TutorialManager.instance?.TryShow(TutorialIds.Level3StartLevel, TutorialContent.Level3StartLevel, TutorialSpeaker.Waterlily);
        }
        else
        {
            // none of the above branches fired - log exactly why, so an intermittent "tutorial
            // didn't appear" report can be traced to the real cause instead of guessed at
            int levelNumber = ProceduralLevel.CurrentConfig != null ? ProceduralLevel.CurrentConfig.levelNumber : -1;
            string loadout = SaveManager.instance != null ? string.Join(",", SaveManager.instance.selectedLoadout) : "NULL";
            Debug.Log($"[Tutorial] CloseAfterSelect - no start tutorial branch matched. levelNumber={levelNumber}, selectedLoadout=[{loadout}]");
        }
    }

    // cleanup used when leaving to the Main Menu (Escape -> Settings -> Main Menu, or Game
    // Over) rather than from an actual fertilizer pick - closes the panel without
    // CloseAfterSelect's tutorial-trigger side effects, which should only ever fire from a
    // genuine selection at level start, not from bailing out to the main menu mid-pick
    public void CloseForSceneExit()
    {
        panel.SetActive(false);
        ClearCards();
    }
}
