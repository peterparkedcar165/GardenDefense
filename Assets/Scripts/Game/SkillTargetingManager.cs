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

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
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

    public void BeginTargeting(float radius, Action<Vector3> onConfirm)
    {
        if (isTargeting) Cancel();

        this.onConfirm = onConfirm;
        isTargeting = true;

        if (targetIndicatorPrefab != null)
        {
            indicatorInstance = Instantiate(targetIndicatorPrefab);
            indicatorInstance.transform.localScale = Vector3.one * radius * 2f;
        }
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
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        worldPos.z = 0f;
        return worldPos;
    }
}
