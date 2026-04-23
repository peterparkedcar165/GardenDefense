using UnityEngine;
using TMPro;

public class PlantButton : MonoBehaviour
{
    
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private TMP_Text costText;
    GameManager gameManager;
    Plant plant;

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        plant = plantPrefab.GetComponent<Plant>();
    }

    // Update is called once per frame
    void Update()
    {
        costText.text = plant.sunCost.ToString();
        if (gameManager.SunCount >= plant.sunCost)
        {
            costText.fontStyle = FontStyles.Bold;
            costText.color = Color.black;
        }
        else
        {
            costText.fontStyle = FontStyles.Normal;
            costText.color = Color.red;
        }
    }
}
