using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class SkillTargetingManager : MonoBehaviour
{
    public static SkillTargetingManager instance;

    [SerializeField] private GameObject targetIndicatorPrefab;

    private bool isTargeting = false;
    private bool cancelledThisFrame = false;
    public bool IsTargeting => isTargeting;
    public bool WasCancelledThisFrame => cancelledThisFrame;
    private Action<Vector3> onConfirm;
    private GameObject indicatorInstance;

    // optional: clamp the aim point (indicator + confirm) to within a radius of a center
    private bool clampAim;
    private Vector3 clampCenter;
    private float clampRadius;

    private bool isPlantTargeting = false;
    private Action<Plant> onPlantConfirm;
    public bool IsPlantTargeting => isPlantTargeting;
    private bool cancelledPlantThisFrame = false;
    public bool WasPlantCancelledThisFrame => cancelledPlantThisFrame;
    public Plant PlantTargetingSource { get; private set; }

    private bool isDeadTileTargeting = false;
    private Action<Tile> onDeadTileConfirm;
    public bool IsDeadTileTargeting => isDeadTileTargeting;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (isPlantTargeting)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelPlantTargeting();
                return;
            }
        }

        if (isDeadTileTargeting)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelDeadTileTargeting();
                return;
            }
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    Vector3 tileClickPos = GetMouseWorldPosition();
                    Vector2Int key = Tile.TileKey(tileClickPos);
                    if (Tile.allTiles.TryGetValue(key, out Tile clickedTile) && clickedTile.deadPlant != null)
                        ConfirmDeadTile(clickedTile);
                }
            }
        }

        if (!isTargeting) return;

        Vector3 mouseWorld = GetMouseWorldPosition();

        if (clampAim)
        {
            Vector3 off = mouseWorld - clampCenter;
            if (off.sqrMagnitude > clampRadius * clampRadius)
                mouseWorld = clampCenter + off.normalized * clampRadius;
        }

        if (indicatorInstance != null)
            indicatorInstance.transform.position = mouseWorld;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                Cancel();
            else
                Confirm(mouseWorld);
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            Cancel();
    }

    public void CancelAll()
    {
        if (isTargeting) Cancel();
        if (isPlantTargeting) CancelPlantTargeting();
        if (isDeadTileTargeting) CancelDeadTileTargeting();
    }

    public void BeginTargeting(float radius, Action<Vector3> onConfirm, Vector3? clampCenter = null, float clampRadius = 0f)
    {
        PlantSelector.instance?.ClearAll();
        if (isTargeting) Cancel();

        this.onConfirm = onConfirm;
        isTargeting = true;

        clampAim = clampCenter.HasValue;
        this.clampCenter = clampCenter ?? Vector3.zero;
        this.clampRadius = clampRadius;

        if (targetIndicatorPrefab != null)
        {
            indicatorInstance = Instantiate(targetIndicatorPrefab);
            indicatorInstance.transform.localScale = Vector3.one * radius * 2f;
        }
    }

    public void BeginPlantTargeting(Action<Plant> onConfirm, Plant source = null)
    {
        PlantSelector.instance?.ClearAll();
        if (isTargeting) Cancel();
        if (isPlantTargeting) CancelPlantTargeting();
        this.onPlantConfirm = onConfirm;
        PlantTargetingSource = source;
        isPlantTargeting = true;
    }

    public void ConfirmPlantTarget(Plant plant)
    {
        isPlantTargeting = false;
        PlantTargetingSource = null;
        var cb = onPlantConfirm;
        onPlantConfirm = null;
        cb?.Invoke(plant);
    }

    public void CancelPlantTargeting()
    {
        isPlantTargeting = false;
        PlantTargetingSource = null;
        onPlantConfirm = null;
        cancelledPlantThisFrame = true;
    }

    public void BeginDeadTileTargeting(Action<Tile> onConfirm)
    {
        PlantSelector.instance?.ClearAll();
        if (isTargeting) Cancel();
        if (isPlantTargeting) CancelPlantTargeting();
        if (isDeadTileTargeting) CancelDeadTileTargeting();
        Plant.ShowDeadPlantGhosts();
        onDeadTileConfirm   = onConfirm;
        isDeadTileTargeting = true;
    }

    private void ConfirmDeadTile(Tile tile)
    {
        isDeadTileTargeting = false;
        Plant.HideDeadPlantGhosts();
        var cb = onDeadTileConfirm;
        onDeadTileConfirm = null;
        cb?.Invoke(tile);
    }

    public void CancelDeadTileTargeting()
    {
        isDeadTileTargeting = false;
        Plant.HideDeadPlantGhosts();
        onDeadTileConfirm = null;
    }

    private void Confirm(Vector3 worldPosition)
    {
        isTargeting = false;
        Destroy(indicatorInstance);
        indicatorInstance = null;
        onConfirm?.Invoke(worldPosition);
        onConfirm = null;
    }

    private void Cancel()
    {
        isTargeting = false;
        cancelledThisFrame = true;
        Destroy(indicatorInstance);
        indicatorInstance = null;
        onConfirm = null;
    }

    private void LateUpdate()
    {
        cancelledThisFrame = false;
        cancelledPlantThisFrame = false;
    }

    public Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        worldPos.z = 0f;
        return worldPos;
    }
}
