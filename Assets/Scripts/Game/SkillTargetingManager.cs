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

    private bool isPlantTargeting = false;
    private Action<Plant> onPlantConfirm;
    public bool IsPlantTargeting => isPlantTargeting;
    private bool cancelledPlantThisFrame = false;
    public bool WasPlantCancelledThisFrame => cancelledPlantThisFrame;
    public Plant PlantTargetingSource { get; private set; }

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

        if (!isTargeting) return;

        Vector3 mouseWorld = GetMouseWorldPosition();

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
    }

    public void BeginTargeting(float radius, Action<Vector3> onConfirm)
    {
        PlantSelector.instance?.ClearAll();
        if (isTargeting) Cancel();

        this.onConfirm = onConfirm;
        isTargeting = true;

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
