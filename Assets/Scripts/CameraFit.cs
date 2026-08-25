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

    [Header("Pan To Target")]
    public float panMaxSpeed = 60f;
    public float panSmoothTime = 0.4f;

    [Header("UI")]
    [Range(0f, 0.5f)] public float rightPanelFraction = 0.241f;

    // the ground tilemap rectangle of the current level, used by displacement clamping
    public static Bounds MapBounds { get; private set; }

    public static CameraFit instance;

    private Camera _cam;
    private Bounds _mapBounds;
    private float _maxZoom;
    private float _targetZoom;
    private Vector2 _camVelocity;

    private bool _isPanningToTarget;
    private Vector3 _panWorldPosition; // raw target (e.g. a plant's position), independent of zoom
    private Vector3 _panVelocity;

    void Awake()
    {
        instance = this;
    }

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

        if (_isPanningToTarget)
        {
            UpdatePanToTarget(blocked);
            return;
        }

        // fully zoomed out already shows the whole map, panning has nothing left to reveal
        bool atMaxZoom = _cam.orthographicSize >= _maxZoom - 0.01f;
        HandleMovement(blocked || atMaxZoom);
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

    // starts a fast-but-smooth pan to center on a world position (e.g. an F-key plant
    // selection). the raw target is stored unmodified; the actual camera-space target (panel
    // offset + bounds clamp) is recomputed every frame from the current zoom, so zooming
    // in/out mid-pan doesn't leave the camera aimed at a stale, now-invalid position
    public void CenterOn(Vector3 worldPosition)
    {
        if (_cam == null) return;
        _panWorldPosition = worldPosition;
        _panVelocity = Vector3.zero;
        _isPanningToTarget = true;
    }

    // offsets for the right-side UI panel so the target lands in the middle of the
    // actually-visible gameplay area, not the middle of the full (partially occluded)
    // viewport — same offset CenterCamera() uses for the initial view — then clamps to bounds
    // at the current zoom level
    private Vector3 ComputePanTarget()
    {
        float halfW = _cam.orthographicSize * _cam.aspect;
        Vector3 desired = new Vector3(_panWorldPosition.x + halfW * rightPanelFraction, _panWorldPosition.y, transform.position.z);
        return ClampToMapBounds(desired);
    }

    private void UpdatePanToTarget(bool blocked)
    {
        // manual input takes back control immediately instead of fighting the pan
        bool manualInput = !blocked && (Keyboard.current.wKey.isPressed || Keyboard.current.sKey.isPressed ||
                                         Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed);
        if (manualInput)
        {
            _isPanningToTarget = false;
            return;
        }

        Vector3 target = ComputePanTarget();
        transform.position = Vector3.SmoothDamp(
            transform.position, target, ref _panVelocity, panSmoothTime, panMaxSpeed, Time.unscaledDeltaTime);

        // safety clamp every frame: if the player zooms out mid-pan, this keeps the camera
        // from drifting past the map edge before the next SmoothDamp step corrects course
        transform.position = ClampToMapBounds(transform.position);

        // let velocity settle too, not just distance — stopping on distance alone can cut off
        // a still-moving camera and read as a snap once normal movement handling resumes
        if (Vector3.Distance(transform.position, target) < 0.02f && _panVelocity.sqrMagnitude < 0.01f)
            _isPanningToTarget = false;
    }

    private void ClampPosition() => transform.position = ClampToMapBounds(transform.position);

    private Vector3 ClampToMapBounds(Vector3 position)
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
            : Mathf.Clamp(position.x, xMin, xMax);

        float y = _mapBounds.size.y > halfH * 2f
            ? Mathf.Clamp(position.y, _mapBounds.min.y + halfH, _mapBounds.max.y - halfH)
            : _mapBounds.center.y;

        return new Vector3(x, y, position.z);
    }
}
