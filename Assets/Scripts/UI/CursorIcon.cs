using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CursorIcon : MonoBehaviour
{
    public static CursorIcon instance;
    private Plant cachedPlant;
    private SpriteRenderer cachedRenderer;

    [SerializeField] private Image icon;
    [SerializeField] private Sprite shovelSprite;
    [SerializeField] private float alpha = 0.5f;
    [SerializeField] private Canvas canvas;
    private Camera  cam;

    private RectTransform rectTransform;
    private RectTransform canvasRect;

    void Awake()
    {
        instance = this;
        rectTransform = icon.GetComponent<RectTransform>();
        canvasRect = canvas.GetComponent<RectTransform>();
        icon.enabled = false;
        cam = Camera.main;
    }

    public void OnSelectionChanged()
    {
        if (PlantSelector.instance.SelectedPlant != null)
        {
            cachedPlant = PlantSelector.instance.SelectedPlant.GetComponent<Plant>();
            cachedRenderer = cachedPlant.GetComponent<SpriteRenderer>();
        }
        else
        {
            cachedPlant = null;
            cachedRenderer = null;
        }
    }

    void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = cam.ScreenToWorldPoint(mousePosition);

        if (PlantSelector.instance.SelectedPlant != null)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            Tile tile = hit != null ? hit.GetComponent<Tile>() : null;

            if (tile != null && cachedPlant != null && System.Array.IndexOf(cachedPlant.allowedTiles, tile.tileType) != -1)
            {
                Vector2 tileScreenPos = cam.WorldToScreenPoint(tile.transform.position);
                Show(cachedRenderer.sprite, tileScreenPos);
            }
            else
            {
                icon.enabled = false;
            }
        }
        else if (PlantSelector.instance.uprootMode)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);

            if (hit != null && hit.GetComponent<Plant>() != null)
            {
                Vector2 plantScreenPos = cam.WorldToScreenPoint(hit.transform.position);
                Show(shovelSprite, plantScreenPos);
            }
            else
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
        icon.color = new Color(1f, 1f, 1f, alpha);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out Vector3 worldPos);
        rectTransform.position = worldPos;
    }
}
