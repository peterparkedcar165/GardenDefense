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
        panel.SetActive(true);
        GameManager.instance.SetPause(true);

        foreach (var card in activeCards)
            Destroy(card.gameObject);
        activeCards.Clear();

        List<FertilizerData> picks = PickRandom(fertilizerPool, fertilizerPool.Length);
        for (int i = 0; i < picks.Count; i++)
        {
            FertilizerCard card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(picks[i], this);
            activeCards.Add(card);
        }
    }

    void Update()
    {
        if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            BackToLoadout();
    }

    private void BackToLoadout()
    {
        panel.SetActive(false);
        foreach (var card in activeCards)
            Destroy(card.gameObject);
        activeCards.Clear();
        LoadoutSelectionUI.instance?.ShowWithCurrentSelection();
    }

    // leaves the game paused after fertilizer selection - wave 0 now waits for the player to
    // press Space or the Pause/Resume button (labelled START while GameManager.HasStarted is
    // false) instead of unpausing automatically the moment a fertilizer is picked
    public void CloseAfterSelect()
    {
        panel.SetActive(false);
        foreach (var card in activeCards)
            Destroy(card.gameObject);
        activeCards.Clear();
    }

    private List<FertilizerData> PickRandom(FertilizerData[] pool, int count)
    {
        List<FertilizerData> ordered = new List<FertilizerData>(pool);
        count = Mathf.Min(count, ordered.Count);
        return ordered.GetRange(0, count);
    }
}
