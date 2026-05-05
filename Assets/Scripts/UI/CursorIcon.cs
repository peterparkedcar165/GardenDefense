using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CursorIcon : MonoBehaviour
{
    public static CursorIcon instance;

    [SerializeField] private Image icon;
    [SerializeField] private Sprite shovelSprite;
    [SerializeField] private float alpha = 0.5f;
    [SerializeField] private Canvas canvas;

    private RectTransform rectTransform;

    void Awake()
    {
        instance = this;
        rectTransform = icon.GetComponent<RectTransform>();
        icon.enabled = false;
    }

    void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        if (PlantSelector.instance.SelectedPlant != null)
        {
            Plant selectedPlant = PlantSelector.instance.SelectedPlant.GetComponent<Plant>();
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            Debug.Log(hit != null ? hit.gameObject.name : "nothing hit");

            Tile tile = hit != null ? hit.GetComponent<Tile>() : null;

            if (tile != null && System.Array.IndexOf(selectedPlant.allowedTiles, tile.tileType) != -1)
            {
                Vector2 tileScreenPos = Camera.main.WorldToScreenPoint(tile.transform.position);
                Show(selectedPlant.GetComponent<SpriteRenderer>().sprite, tileScreenPos);
            } else
            {
                icon.enabled = false;
            }
        } else if (PlantSelector.instance.uprootMode)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);

            if (hit != null && hit.GetComponent<Plant>() != null)
            {
                Vector2 plantScreenPos = Camera.main.WorldToScreenPoint(hit.transform.position);
                Show(shovelSprite, plantScreenPos);
            } else
            {
                icon.enabled = false;
            }
        }
        else
        {
            icon.enabled = false;
        }
    }

    private void Show(Sprite sprite, Vector2 screenPos)
    {
        icon.enabled = true;
        icon.sprite = sprite;
        icon.color = new Color(1f,1f,1f, alpha);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.GetComponent<RectTransform>(), screenPos, canvas.worldCamera, out Vector3 worldPos);

        rectTransform.position = worldPos;
    }
}
