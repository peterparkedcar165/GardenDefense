using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CursorIcon : MonoBehaviour
{
    public static CursorIcon instance;
    private Plant cachedPlant;
    private SpriteRenderer cachedRenderer;
    private SpriteRenderer cachedPotRenderer;

    [SerializeField] private Image icon;
    [SerializeField] private Sprite shovelSprite;
    [SerializeField] private float alpha = 0.5f;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject rangePreviewPrefab;
    private Transform rangePreviewCircle;
    private Camera  cam;

    private RectTransform rectTransform;
    private RectTransform canvasRect;

    void Awake()
    {
        if (instance != null) return;
        instance = this;
        rectTransform = icon.GetComponent<RectTransform>();
        canvasRect = canvas.GetComponent<RectTransform>();
        icon.enabled = false;
        cam = Camera.main;
        if (rangePreviewPrefab != null)
        {
            GameObject obj = Instantiate(rangePreviewPrefab);
            DontDestroyOnLoad(obj);
            rangePreviewCircle = obj.transform;
            rangePreviewCircle.gameObject.SetActive(false);
        }
    }

    public void OnSelectionChanged()
    {
        if (PlantSelector.instance.SelectedPlant != null)
        {
            cachedPlant = PlantSelector.instance.SelectedPlant.GetComponent<Plant>();
            cachedRenderer = cachedPlant.GetComponentInChildren<SpriteRenderer>();
        }
        else
        {
            cachedPlant = null;
            cachedRenderer = null;
        }
        cachedPotRenderer = null;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = cam.ScreenToWorldPoint(mousePosition);

        if (PlantSelector.instance.SelectedPlant != null)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            Tile tile = hit != null ? hit.GetComponent<Tile>() : null;

            if (tile != null && cachedPlant != null && cachedRenderer != null && System.Array.IndexOf(cachedPlant.allowedTiles, tile.tileType) != -1)
            {
                Vector2 visualOffset = cachedRenderer.bounds.center - cachedPlant.transform.position;
                Vector2 tileScreenPos = cam.WorldToScreenPoint((Vector2)tile.transform.position + visualOffset);
                Show(cachedRenderer.sprite, tileScreenPos);
                MatchSizeToRenderer(cachedRenderer);
                ShowRangePreview(tile);
            }
            else
            {
                icon.enabled = false;
                HideRangePreview();
            }
        }
        else if (PlantSelector.instance.flowerPotMode)
        {
            if (cachedPotRenderer == null && PlantSelector.instance.flowerPotPrefab != null)
                cachedPotRenderer = PlantSelector.instance.flowerPotPrefab.GetComponentInChildren<SpriteRenderer>();

            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            Tile tile = hit != null ? hit.GetComponent<Tile>() : null;

            if (tile != null && tile.tileType != TileType.Water && tile.tileType != TileType.Path && tile.tileType != TileType.Potted && cachedPotRenderer != null)
            {
                Vector2 tileScreenPos = cam.WorldToScreenPoint(tile.transform.position);
                Show(cachedPotRenderer.sprite, tileScreenPos);
                MatchSizeToRenderer(cachedPotRenderer);
            }
            else
            {
                icon.enabled = false;
            }
            HideRangePreview();
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
            HideRangePreview();
        }
        else
        {
            icon.enabled = false;
            HideRangePreview();
        }
    }

    private void ShowRangePreview(Tile tile)
    {
        if (rangePreviewCircle == null || cachedPlant == null) return;
        float range = cachedPlant.baseAttackRange;
        if (tile.isHighground) range *= 2f;
        rangePreviewCircle.gameObject.SetActive(true);
        rangePreviewCircle.position = new Vector3(tile.transform.position.x, tile.transform.position.y, rangePreviewCircle.position.z);
        rangePreviewCircle.localScale = new Vector3(range * 2f, range * 2f, 1f);
    }

    private void HideRangePreview()
    {
        if (rangePreviewCircle != null) rangePreviewCircle.gameObject.SetActive(false);
    }

    private void Show(Sprite sprite, Vector2 screenPos)
    {
        icon.enabled = true;
        icon.sprite = sprite;
        icon.color = new Color(1f, 1f, 1f, alpha);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out Vector3 worldPos);
        rectTransform.position = worldPos;
    }

    private void MatchSizeToRenderer(SpriteRenderer sr)
    {
        Vector2 worldSize = sr.bounds.size;
        Vector2 screenP1 = cam.WorldToScreenPoint(Vector3.zero);
        Vector2 screenP2 = cam.WorldToScreenPoint(new Vector3(worldSize.x, worldSize.y, 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenP1, canvas.worldCamera, out Vector2 cp1);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenP2, canvas.worldCamera, out Vector2 cp2);
        rectTransform.sizeDelta = cp2 - cp1;
    }
}
