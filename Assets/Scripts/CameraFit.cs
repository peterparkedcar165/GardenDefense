using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class CameraFit : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;

    [Header("Zoom")]
    public float minZoom = 3f;
    public float zoomSpeed = 0.5f;
    public float zoomSmoothing = 8f;

    [Header("Movement")]
    public float moveSpeed = 24f;
    public float moveSmoothing = 10f;

    [Header("UI")]
    [Range(0f, 0.5f)] public float rightPanelFraction = 0.236f;

    // the ground tilemap rectangle of the current level, used by displacement clamping
    public static Bounds MapBounds { get; private set; }

    private Camera _cam;
    private Bounds _mapBounds;
    private float _maxZoom;
    private float _targetZoom;
    private Vector2 _camVelocity;

    void Start()
    {
        _cam = GetComponent<Camera>();
        CacheMapBounds();
        CenterCamera();
    }

    void OnEnable()
    {
        if (_cam == null) return;
        CacheMapBounds();
        CenterCamera();
    }

    private void CacheMapBounds()
    {
        groundTilemap.CompressBounds();
        _mapBounds = groundTilemap.GetComponent<TilemapRenderer>().bounds;
        _maxZoom = _mapBounds.size.y / 2f;
        MapBounds = _mapBounds;
    }

    private void CenterCamera()
    {
        _cam.orthographicSize = _maxZoom;
        _targetZoom = _maxZoom;
        float halfW = _maxZoom * _cam.aspect;
        float panelW = rightPanelFraction * halfW * 2f;
        float gameplayW = halfW * 2f - panelW;

        // center map in the gameplay area (screen minus right panel)
        float startX = _mapBounds.size.x <= gameplayW
            ? _mapBounds.center.x + rightPanelFraction * halfW
            : _mapBounds.min.x + halfW;

        transform.position = new Vector3(startX, _mapBounds.center.y, transform.position.z);
    }

    void Update()
    {
        bool blocked = InputBlocked();
        if (!blocked) HandleZoom();
        HandleMovement(blocked);
        ClampPosition();
    }

    private bool InputBlocked()
    {
        if (GameManager.instance == null || !GameManager.instance.IsGameActive) return true;
        if (SettingsManager.instance != null && SettingsManager.instance.IsOpen) return true;
        if (FertilizerSelectionUI.instance != null && FertilizerSelectionUI.instance.IsOpen) return true;
        if (LoadoutSelectionUI.instance != null && LoadoutSelectionUI.instance.IsOpen) return true;
        if (Mouse.current.position.ReadValue().x > Screen.width * (1f - rightPanelFraction)) return true;
        return false;
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0f)
            _targetZoom = Mathf.Clamp(_targetZoom - Mathf.Sign(scroll) * zoomSpeed, minZoom, _maxZoom);

        if (Mathf.Approximately(_cam.orthographicSize, _targetZoom)) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));

        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, zoomSmoothing * Time.unscaledDeltaTime);

        Vector3 mouseWorldAfter = _cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        transform.position += mouseWorld - mouseWorldAfter;
    }

    private void HandleMovement(bool decelOnly)
    {
        float x = 0f, y = 0f;
        if (!decelOnly)
        {
            if (Keyboard.current.wKey.isPressed) y =  1f;
            if (Keyboard.current.sKey.isPressed) y = -1f;
            if (Keyboard.current.aKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed) x =  1f;
        }

        float speed = moveSpeed * _cam.orthographicSize / _maxZoom;
        Vector2 target = new Vector2(x, y) * speed;

        _camVelocity = Vector2.Lerp(_camVelocity, target, moveSmoothing * Time.unscaledDeltaTime);

        if (_camVelocity.sqrMagnitude > 0.001f)
            transform.position += new Vector3(_camVelocity.x, _camVelocity.y, 0f) * Time.unscaledDeltaTime;
    }

    private void ClampPosition()
    {
        float halfH = _cam.orthographicSize;
        float halfW = _cam.orthographicSize * _cam.aspect;
        float panelW = rightPanelFraction * halfW * 2f;
        float gameplayW = halfW * 2f - panelW;

        // left clamp: camera left edge at map left
        // right clamp: gameplay right edge at map right (camera can go further right to compensate for panel)
        float xMin = _mapBounds.min.x + halfW;
        float xMax = _mapBounds.max.x - halfW + panelW;

        float x = _mapBounds.size.x <= gameplayW
            ? _mapBounds.center.x + rightPanelFraction * halfW
            : Mathf.Clamp(transform.position.x, xMin, xMax);

        float y = _mapBounds.size.y > halfH * 2f
            ? Mathf.Clamp(transform.position.y, _mapBounds.min.y + halfH, _mapBounds.max.y - halfH)
            : _mapBounds.center.y;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
