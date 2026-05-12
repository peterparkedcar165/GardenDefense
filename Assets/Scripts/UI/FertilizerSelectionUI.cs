using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class FertilizerSelectionUI : MonoBehaviour
{
    public static FertilizerSelectionUI instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private FertilizerCard cardPrefab;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text rerollCountText;
    [SerializeField] private FertilizerData[] fertilizerPool;
    [SerializeField] private int cardsToShow = 3;
    public int bonusRerolls;

    private int sharedRerolls;
    private FertilizerCard selectedCard;
    private List<FertilizerCard> activeCards = new List<FertilizerCard>();

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    public bool IsOpen => panel.activeSelf;

    public void Show()
    {
        panel.SetActive(true);
        GameManager.instance.SetPause(true);
        sharedRerolls = 3 + bonusRerolls;
        selectedCard = null;
        confirmButton.interactable = false;
        UpdateRerollText();

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

    public void SelectCard(FertilizerCard card)
    {
        selectedCard = card;
        foreach (var c in activeCards)
            c.SetHighlight(c == card);
        confirmButton.interactable = true;
    }

    public bool TryReroll(FertilizerCard card)
    {
        if (sharedRerolls <= 0) return false;
        sharedRerolls--;
        UpdateRerollText();
        return true;
    }

    public void Confirm()
    {
        if (selectedCard == null) return;
        selectedCard.Commit();
        Hide();
    }

    private void Hide()
    {
        panel.SetActive(false);
        GameManager.instance.SetPause(false);
        foreach (var card in activeCards)
            Destroy(card.gameObject);
        activeCards.Clear();
    }

    private void UpdateRerollText()
    {
        if (rerollCountText != null)
            rerollCountText.text = $"Rerolls: {sharedRerolls}";
    }

    private List<FertilizerData> PickRandom(FertilizerData[] pool, int count)
    {
        List<FertilizerData> ordered = new List<FertilizerData>(pool);
        count = Mathf.Min(count, ordered.Count);
        return ordered.GetRange(0, count);
    }
}
