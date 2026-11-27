using UnityEngine;
using UnityEngine.Tilemaps;

// one biome's cliff art for CliffBuilder (Tommy, 2026-11-27, gd_cliffs_border): the tiles in the order the
// art's lookup tables count them, left to right and top to bottom on each sheet
[CreateAssetMenu(fileName = "CliffSet", menuName = "Scriptable Objects/Cliff Set")]
public class CliffSet : ScriptableObject
{
    [Tooltip("the 77 cliff top pieces (cliff_tops.png): they go on raised cells")]
    public TileBase[] topTiles;
    [Tooltip("the 17 rock face pieces (cliff_faces.png): they go on low cells under a cliff")]
    public TileBase[] faceTiles;
}
