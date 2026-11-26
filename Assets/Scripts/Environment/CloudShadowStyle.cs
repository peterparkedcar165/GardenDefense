using UnityEngine;

// the cloud shadows' look and numbers (Tommy, 2026-11-26): his pitch black cloud shapes drifting over the
// map on levels with sun, made with the same particle system as the rain, snow and sandstorm (CloudShadows).
// lives at Resources/CloudShadowStyle.asset; every number changes live while playing
[CreateAssetMenu(fileName = "CloudShadowStyle", menuName = "Scriptable Objects/Cloud Shadow Style")]
public class CloudShadowStyle : ScriptableObject
{
    private static CloudShadowStyle _instance;
    public static CloudShadowStyle Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<CloudShadowStyle>("CloudShadowStyle");
            if (_instance == null) _instance = CreateInstance<CloudShadowStyle>();
            return _instance;
        }
    }

    // one size of shadow: its shapes packed in a sheet of equal cells, drawn at exactly their own pixels
    [System.Serializable]
    public class CloudSize
    {
        [Tooltip("the shapes of this size packed in one sheet (Sprites/CloudShadowsBig/cloud_shadow_sheet_*), pitch black")]
        public Texture2D sheet;
        [Min(1)] public int columns = 1;
        [Min(1)] public int rows = 1;
        [Tooltip("a cell's width in pixels: drawn at the map's 64 a tile, so a 512 px cloud covers exactly 512 game pixels " +
            "(8 tiles), never scaled (Tommy: no mixels)")]
        [Min(1)] public int cellPixels = 256;
        [Tooltip("about how many of this size are over the zoomed out view at once")]
        [Min(0f)] public float count = 4f;
    }

    [Tooltip("the weather particle prefab the shadows are made from (Tommy: the same as the rain, snow and sandstorm; " +
        "the sandstorm's), one copy per size, its numbers changed in code")]
    public GameObject particlePrefab;
    [Tooltip("Custom/CloudShadow (Shaders/CloudShadow): black shapes at the opacity, where two shadows overlap the " +
        "pixel is darkened once, not twice (Tommy). empty: the weather particles' own material, which adds them up")]
    public Shader shader;
    [Tooltip("Tommy's big cloud shadows (Downloads/gd_cloud_shadows_big, 2026-11-26), one entry per size: 256, 512, 1024 px")]
    public CloudSize[] sizes;

    [Header("Look")]
    [Tooltip("how dark a shadow is: the pitch black shapes' alpha")]
    [Range(0f, 1f)] public float opacity = 0.5f;
    [Tooltip("above everything in the world (the Weather layer, over the rain, snow and sandstorm at 0 to 2) and under the " +
        "indicators and the ui; under the mulch drops too (Weather 60 and 61, MulchDrop; Tommy: drops are never covered)")]
    public string sortingLayer = "Weather";
    public int sortingOrder = 50;

    [Header("Movement (every number changes live while playing)")]
    [Tooltip("tiles a real second; the same whatever the game speed, and on while paused, like the rain")]
    [Min(0.01f)] public float speed = 0.3f;
    [Tooltip("the direction they drift, in degrees up from straight right (Tommy: 50, up and right)")]
    [Range(0f, 90f)] public float angle = 50f;
    [Tooltip("real seconds to fade in or out when the sun comes or goes, or rain, snow or a sandstorm starts or ends")]
    [Min(0.01f)] public float fadeSeconds = 2f;
}
