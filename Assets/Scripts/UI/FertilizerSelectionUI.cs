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
        if (header is RectTransform headerRect)
        {
            headerRect.anchoredPosition = new Vector2(headerRect.anchoredPosition.x, HeaderY);
            TitleOnPlank(headerRect);
            if (title == null) title = headerRect;
            titleHome = title.anchoredPosition;
        }
        panel.SetActive(false);
    }

    private const float BackdropAlpha = 0.85f;
    private const float HeaderY = 360f;
    // the title plank at 2 times its art (4 canvas units an art pixel, whole pixels at 1080)
    private const float TitleScale = 2f;

    // the title on the main menu's large leafy plank (Tommy, 2026-11-26), in the yellow of the menu's
    // other buttons, centred where the title was; the old text hides
    private void TitleOnPlank(RectTransform headerRect)
    {
        TMPro.TMP_Text title = headerRect.GetComponent<TMPro.TMP_Text>();
        GameControlsStyle look = GameControlsStyle.Instance;
        if (title == null || look.largeButton == null) return;
        PlankButton plank = PlankButton.CreateTitle((RectTransform)panel.transform, title.text, look.pause, InsectShowcaseStyle.Instance.font);
        RectTransform r = plank.Rect;
        r.anchorMin = headerRect.anchorMin;
        r.anchorMax = headerRect.anchorMax;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = headerRect.anchoredPosition;
        r.localScale = Vector3.one * TitleScale;
        r.SetSiblingIndex(headerRect.GetSiblingIndex());
        title.gameObject.SetActive(false);
        this.title = r;
    }

    // the title (its plank, or the old text without one) and where it rests
    private RectTransform title;
    private Vector2 titleHome;

    // the title falls into place with the cards (Tommy, 2026-11-26): the same distance, time and easing as
    // FertilizerCard.AnimateIn, starting the same frame later the cards do
    private System.Collections.IEnumerator DropTitle()
    {
        if (title == null) yield break;
        Vector2 start = titleHome + Vector2.up * Screen.height;
        title.anchoredPosition = start;
        yield return null;
        for (float elapsed = 0f; elapsed < FertilizerCard.FallSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed / FertilizerCard.FallSeconds;
            t = 1f - Mathf.Pow(1f - t, 3f);
            title.anchoredPosition = Vector2.Lerp(start, titleHome, t);
            yield return null;
        }
        title.anchoredPosition = titleHome;
    }

    // the cards fall face down; once every one has landed, a moment later they all turn over together
    // (Tommy, 2026-11-26)
    private const float RevealPause = 0.15f;

    private System.Collections.IEnumerator RevealWhenLanded()
    {
        float waited = 0f;
        while (activeCards.Exists(c => c != null && !c.Landed) && waited < 3f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(RevealPause);
        foreach (FertilizerCard card in activeCards)
            if (card != null) card.Flip();
    }

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
        StartCoroutine(DropTitle());
        StartCoroutine(RevealWhenLanded());
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
