using UnityEngine;
using System.Collections.Generic;

public class FertilizerSelectionUI : MonoBehaviour
{
    public static FertilizerSelectionUI instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private FertilizerCard cardPrefab;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private FertilizerData[] fertilizerPool;
    [SerializeField] private int cardsToShow = 3;

    private List<FertilizerCard> activeCards = new List<FertilizerCard>();

    void Awake()
    {
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
        panel.SetActive(true);
        GameManager.instance.SetPause(true);

        foreach (var card in activeCards)
            Destroy(card.gameObject);
        activeCards.Clear();

        List<FertilizerData> picks = PickRandom(fertilizerPool, cardsToShow);
        for (int i = 0; i < picks.Count; i++)
        {
            FertilizerCard card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(picks[i], this);
            activeCards.Add(card);
        }
    }

    public void CloseAfterSelect()
    {
        panel.SetActive(false);
        GameManager.instance.SetPause(false);
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
