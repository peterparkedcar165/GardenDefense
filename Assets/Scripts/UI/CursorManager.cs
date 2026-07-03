using UnityEngine;

public enum CursorState { Default, Uproot }

public class CursorManager : MonoBehaviour
{
    public static CursorManager instance;

    [Header("Cursors")]
    public Texture2D defaultCursor;
    public Texture2D uprootCursor;

    [Header("Hotspots")]
    public Vector2 defaultHotspot = Vector2.zero;
    public Vector2 uprootHotspot  = Vector2.zero;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        ApplyCursor(CursorState.Default);
    }

    public void SetMode(CursorState state)
    {
        ApplyCursor(state);
    }

    private void ApplyCursor(CursorState state)
    {
        switch (state)
        {
            case CursorState.Default: Cursor.SetCursor(defaultCursor, defaultHotspot, CursorMode.Auto); break;
            case CursorState.Uproot:  Cursor.SetCursor(uprootCursor,  uprootHotspot,  CursorMode.Auto); break;
        }
    }
}
