using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CursorIcon : MonoBehaviour
{
    public static CursorIcon instance;
    private Plant cachedPlant;
    private SpriteRenderer cachedRenderer;
    private SpriteRenderer cachedPotRenderer;
    private SpriteRenderer cachedWaterPotRenderer;
    // the plant being transplanted and its renderer, for the transplant preview
    private Plant cachedTransplant;
    private SpriteRenderer cachedTransplantRenderer;

    [SerializeField] private Image icon;
    [SerializeField] private Sprite shovelSprite;
    [SerializeField] private float alpha = 0.5f;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject rangePreviewPrefab;
    private Transform rangePreviewCircle;
    private Transform darkRangePreviewCircle;
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

            // same gold recolor Plant.cs uses for its own placed-plant dark circle, so the
            // placement preview reads as the same "reduced range at night" indicator
            GameObject darkObj = Instantiate(rangePreviewPrefab);
            DontDestroyOnLoad(darkObj);
            darkRangePreviewCircle = darkObj.transform;
            SpriteRenderer darkSr = darkRangePreviewCircle.GetComponentInChildren<SpriteRenderer>();
            if (darkSr != null) darkSr.color = new Color(1f, 0.85f, 0f, darkSr.color.a);
            darkRangePreviewCircle.gameObject.SetActive(false);
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
        cachedPotRenderer      = null;
        cachedWaterPotRenderer = null;
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

            bool tutorialBlocked = tile != null && cachedPlant != null && cachedPlant.data != null && cachedPlant.data.plantName == "AcornKnight"
                && TutorialManager.instance != null && TutorialManager.instance.BlocksAcornKnightPlacement(tile.tileType);

            if (tile != null && cachedPlant != null && cachedRenderer != null && !tutorialBlocked && System.Array.IndexOf(cachedPlant.allowedTiles, tile.tileType) != -1)
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
        else if (PlantSelector.instance.transplantMode)
        {
            // same preview as placing a brand-new plant (ghost sprite + range circle), just
            // sourced from the live instance being relocated instead of a loadout-bar prefab
            Plant transplanting = PlantSelector.instance.transplantingPlant;
            // looked up again only when the plant being moved changes (or its renderer went away)
            if (transplanting != cachedTransplant || cachedTransplantRenderer == null
                || !cachedTransplantRenderer.gameObject.activeInHierarchy)
            {
                cachedTransplant = transplanting;
                cachedTransplantRenderer = transplanting != null ? transplanting.GetComponentInChildren<SpriteRenderer>() : null;
            }
            SpriteRenderer transplantRenderer = cachedTransplantRenderer;

            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            Tile tile = hit != null ? hit.GetComponent<Tile>() : null;

            if (tile != null && transplanting != null && transplantRenderer != null && System.Array.IndexOf(transplanting.allowedTiles, tile.tileType) != -1)
            {
                Vector2 visualOffset = transplantRenderer.bounds.center - transplanting.transform.position;
                Vector2 tileScreenPos = cam.WorldToScreenPoint((Vector2)tile.transform.position + visualOffset);
                Show(transplantRenderer.sprite, tileScreenPos);
                MatchSizeToRenderer(transplantRenderer);
                cachedPlant = transplanting;
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

            if (tile != null && tile.tileType != TileType.Water && tile.tileType != TileType.Path && tile.tileType != TileType.Potted && tile.tileType != TileType.Obstacle && tile.tileType != TileType.Heat && cachedPotRenderer != null)
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
        else if (PlantSelector.instance.waterPotMode)
        {
            if (cachedWaterPotRenderer == null && PlantSelector.instance.waterPotPrefab != null)
                cachedWaterPotRenderer = PlantSelector.instance.waterPotPrefab.GetComponentInChildren<SpriteRenderer>();

            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            Tile tile = hit != null ? hit.GetComponent<Tile>() : null;

            if (tile != null && tile.tileType != TileType.Water && tile.tileType != TileType.Path && tile.tileType != TileType.Potted && tile.tileType != TileType.WaterPotted && tile.tileType != TileType.Obstacle && tile.tileType != TileType.Heat && cachedWaterPotRenderer != null)
            {
                Vector2 tileScreenPos = cam.WorldToScreenPoint(tile.transform.position);
                Show(cachedWaterPotRenderer.sprite, tileScreenPos);
                MatchSizeToRenderer(cachedWaterPotRenderer);
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
                MatchSizeToSprite(shovelSprite);
            }
            else
            {
                // unity null check, not ??: in the editor a missing GetComponent is a fake null
                // object that ?? treats as found, so the parent was never checked there
                Tile tile = null;
                if (hit != null)
                {
                    tile = hit.GetComponent<Tile>();
                    if (tile == null) tile = hit.GetComponentInParent<Tile>();
                }
                if (tile != null && (tile.flowerPot != null || tile.waterPot != null))
                {
                    Vector2 tileScreenPos = cam.WorldToScreenPoint(tile.transform.position);
                    Show(shovelSprite, tileScreenPos);
                    MatchSizeToSprite(shovelSprite);
                }
                else
                {
                    icon.enabled = false;
                }
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
        if (rangePreviewCircle == null || cachedPlant == null || cachedPlant.data == null) return;
        if (!cachedPlant.ShowRangeCircle) { HideRangePreview(); return; }
        // the plant predicts its own placed range: skill tree, both kinds of fertilizer, highground
        // and the melee clamp, with the same formula its real stats use
        float range = cachedPlant.PreviewAttackRange(tile.isHighground);
        rangePreviewCircle.gameObject.SetActive(true);
        rangePreviewCircle.position = new Vector3(tile.transform.position.x, tile.transform.position.y, rangePreviewCircle.position.z);
        rangePreviewCircle.localScale = new Vector3(range * 2f, range * 2f, 1f);
        ShowPreviewSight(tile, range);

        // same half-range darkness indicator placed plants show while selected (see Plant.cs's
        // own circleRadius/darkCircleRadius pair) - otherwise the player only discovers the
        // range cut after the plant is already down
        bool showDark = darkRangePreviewCircle != null && cachedPlant.ShowDarkCircle
            && DarknessManager.instance != null && DarknessManager.instance.isDark;
        if (showDark)
        {
            darkRangePreviewCircle.gameObject.SetActive(true);
            darkRangePreviewCircle.position = rangePreviewCircle.position;
            darkRangePreviewCircle.localScale = new Vector3(range, range, 1f);
        }
        else if (darkRangePreviewCircle != null)
        {
            darkRangePreviewCircle.gameObject.SetActive(false);
        }
    }

    // the red part of the preview circle: where the plant couldn't target from this tile (line of sight,
    // Tommy 2026-11-27). the lines (RayFan, on the same grid as the placed plant's) are cast once per tile hovered,
    // one a pixel apart round the range: plenty for the circle's picture and quick enough to follow the cursor
    private RayFan _previewFan;
    private Tile _previewRaysTile;
    private int _previewRaysVersion;
    private SightShade _previewShade;

    private void ShowPreviewSight(Tile tile, float range)
    {
        if (_previewShade == null) _previewShade = SightShade.On(rangePreviewCircle);
        if (!cachedPlant.NeedsLineOfSight) { _previewShade.Hide(); return; }
        if (_previewFan == null || _previewRaysTile != tile || !_previewFan.Fits(tile.transform.position, tile.isHighground, range, false))
        {
            _previewFan = LineOfSight.Cast(tile.transform.position, tile.isHighground, range + 0.5f, _previewFan, 1f);
            _previewRaysTile = tile;
            _previewRaysVersion++;
        }
        _previewShade.Show(_previewFan, _previewRaysVersion, range);
#if UNITY_EDITOR
        // F8 while placing or moving a plant: the preview's line of sight readout in the console
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame)
        {
            LineOfSight.LogReadout($"preview of {cachedPlant.name}", tile.transform.position, tile.isHighground, range, _previewFan, tile);
            Debug.Log($"[LineOfSight] preview: version {_previewRaysVersion}; " + _previewShade.Describe());
        }
#endif
    }

    private void HideRangePreview()
    {
        if (rangePreviewCircle != null) rangePreviewCircle.gameObject.SetActive(false);
        if (darkRangePreviewCircle != null) darkRangePreviewCircle.gameObject.SetActive(false);
    }

    private void Show(Sprite sprite, Vector2 screenPos)
    {
        icon.enabled = true;
        icon.sprite = sprite;
        icon.color = new Color(1f, 1f, 1f, alpha);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out Vector3 worldPos);
        rectTransform.position = worldPos;
    }

    private void MatchSizeToRenderer(SpriteRenderer sr) => MatchSizeToWorld(sr.bounds.size);

    // the shovel (Tommy's 64 px pixel art, 2026-11-27) at its own size in the world: one tile, the same pixels as the map
    private void MatchSizeToSprite(Sprite sprite)
    {
        if (sprite != null) MatchSizeToWorld(sprite.bounds.size);
    }

    private void MatchSizeToWorld(Vector2 worldSize)
    {
        Vector2 screenP1 = cam.WorldToScreenPoint(Vector3.zero);
        Vector2 screenP2 = cam.WorldToScreenPoint(new Vector3(worldSize.x, worldSize.y, 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenP1, canvas.worldCamera, out Vector2 cp1);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenP2, canvas.worldCamera, out Vector2 cp2);
        rectTransform.sizeDelta = cp2 - cp1;
    }
}
