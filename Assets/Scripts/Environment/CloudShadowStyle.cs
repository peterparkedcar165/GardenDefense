using UnityEngine;

// the cloud shadows' look and numbers (Tommy, 2026-11-26): his black cloud shapes drifting over the map
// on levels with sun, made with the same particle system as the rain, snow and sandstorm (CloudShadows).
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

    [Tooltip("the weather particle prefab the shadows are made from (Tommy: the same as the rain, snow and sandstorm; " +
        "the sandstorm's, which is alpha blended), its numbers changed in code")]
    public GameObject particlePrefab;
    [Tooltip("Tommy's 36 cloud shapes packed in one sheet (Sprites/CloudShadows/cloud_shadow_sheet): sheetColumns by " +
        "sheetRows cells of 64 px, pure black; each shadow shows one cell at random")]
    public Texture2D sheet;
    [Min(1)] public int sheetColumns = 6;
    [Min(1)] public int sheetRows = 6;
    [Tooltip("unused now (the sprites the sheet was made from), kept for reference")]
    public Sprite[] shapes;
    [Tooltip("unused now: the particle system brings its own material")]
    public Material material;
    [Tooltip("Custom/CloudShadow (Shaders/CloudShadow): black shapes at the opacity, where two shadows overlap the " +
        "pixel is darkened once, not twice (Tommy). empty: the weather particles' own material, which adds them up")]
    public Shader shader;

    [Header("Look")]
    [Tooltip("how dark a shadow is: the black shapes' alpha")]
    [Range(0f, 1f)] public float opacity = 0.3f;
    [Tooltip("above everything in the world (the Weather layer, over rain and snow) and under the indicators and the ui")]
    public string sortingLayer = "Weather";
    public int sortingOrder = 1000;

    [Header("Numbers (Tommy tunes these; every one changes live while playing)")]
    [Tooltip("about how many shadows are over the camera's view at once")]
    [Min(0f)] public float count = 8f;
    [Tooltip("a shadow's size in tiles: one 64 px cell of the sheet drawn this many tiles across. whole numbers keep " +
        "every pixel the same size")]
    [Min(0.1f)] public float scale = 3f;
    [Tooltip("tiles a real second; the same whatever the game speed, and on while paused, like the rain")]
    [Min(0.01f)] public float speed = 0.4f;
    [Tooltip("the direction they drift, in degrees up from straight right (Tommy: 50, up and right)")]
    [Range(0f, 90f)] public float angle = 50f;
    [Tooltip("real seconds to fade in or out when the sun comes or goes, or rain, snow or a sandstorm starts or ends")]
    [Min(0.01f)] public float fadeSeconds = 2f;
}
