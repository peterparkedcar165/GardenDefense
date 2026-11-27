using UnityEngine;

// the light that comes down on a plant Rhodiola's skill revives (Tommy, 2026-11-27: Resources/RhodiolaReviveBeam,
// his beam, 14 frames of 64x64 at 65 ms, played once, drawn at 2.5x). its pattern repeats upward, so it's tiled from
// the bottom of the plant's tile to past the top of the camera, as if it came down from the sky. drawn over plants,
// insects and tree tops (below the weather), unlit so it glows at night, on real time (game speed doesn't change
// it) but frozen while paused
public class RhodiolaReviveBeam : MonoBehaviour
{
    private const float FrameSeconds = 0.065f;
    private const float Scale = 2.5f;
    private const string SortingLayer = "Non-Colliders_Air";
    private const int SortingOrder = 100;

    private static Sprite[] _frames;
    private static Material _unlit;

    private SpriteRenderer _renderer;
    private float _age;

    // tileCenter: the middle of the revived plant's tile
    public static void Play(Vector3 tileCenter)
    {
        if (_frames == null)
        {
            _frames = Resources.LoadAll<Sprite>("RhodiolaReviveBeam");
            System.Array.Sort(_frames, (a, b) => FrameIndex(a).CompareTo(FrameIndex(b)));
        }
        if (_frames.Length == 0) return;
        if (_unlit == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null) _unlit = new Material(shader);
        }

        // from the bottom of the tile up to a tile past the top of the camera
        float bottom = tileCenter.y - 0.5f;
        Camera cam = Camera.main;
        float top = cam != null ? cam.transform.position.y + cam.orthographicSize + 1f : bottom + 20f;
        float height = Mathf.Max(1f, top - bottom);

        GameObject go = new GameObject("RhodiolaReviveBeam");
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        go.transform.position = new Vector3(tileCenter.x, bottom, tileCenter.z);
        go.transform.localScale = Vector3.one * Scale;
        RhodiolaReviveBeam beam = go.AddComponent<RhodiolaReviveBeam>();
        beam._renderer = go.AddComponent<SpriteRenderer>();
        beam._renderer.sprite = _frames[0];
        if (_unlit != null) beam._renderer.sharedMaterial = _unlit;
        beam._renderer.sortingLayerName = SortingLayer;
        beam._renderer.sortingOrder = SortingOrder;
        // the frame repeats upward: one frame is a unit across before the 2.5x scale
        beam._renderer.drawMode = SpriteDrawMode.Tiled;
        beam._renderer.tileMode = SpriteTileMode.Continuous;
        beam._renderer.size = new Vector2(_frames[0].bounds.size.x, height / Scale);
    }

    private static int FrameIndex(Sprite s)
    {
        int cut = s.name.LastIndexOf('_');
        return cut >= 0 && int.TryParse(s.name.Substring(cut + 1), out int i) ? i : 0;
    }

    private void Update()
    {
        // real time, so a game speed up doesn't hurry it, but frozen while the game is paused (Tommy)
        _age += Time.timeScale > 0f ? Time.unscaledDeltaTime : 0f;
        int frame = Mathf.FloorToInt(_age / FrameSeconds);
        if (frame >= _frames.Length) { Destroy(gameObject); return; }
        _renderer.sprite = _frames[frame];
    }
}
