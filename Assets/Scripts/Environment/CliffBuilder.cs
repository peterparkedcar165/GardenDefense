using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// cliffs on the borders between cells (Tommy, 2026-11-27: gd_cliffs_border, "option B"). a raised cell is
// one with a tile on any of raisedMaps (HighgroundGround, the map that makes a cell high ground; HighgroundWater
// was one too until 2026-11-28). every raised cell gets a cliff top piece on tops (its edge outline, rim tufts and the
// grass lip over the rocks; a cell raised all round gets none), and every low cell under or beside one gets a
// rock face piece on faces (the face fills its top 22 px, the rest stays open ground). each piece is picked
// from the cell's 8 neighbours with the lookup tables that came with the art. nobody paints cliffs by hand:
// in the editor they redraw whenever a raised map changes. outside boundsMap (the level's ground) a cell
// counts as the one beside it inside, so no cliff runs along the map's edge.
//
// in play it also lays a thin edge collider on every border between a raised and a low cell, on the
// Obstacle layer, so insects' chase sweeps, pushes and wind stop at a cliff like at an obstacle tile
[ExecuteAlways]
public class CliffBuilder : MonoBehaviour
{
    public CliffSet cliffSet;
    [Tooltip("a cell with a tile on any of these is raised")]
    public Tilemap[] raisedMaps;
    [Tooltip("the level's ground: the map's extent")]
    public Tilemap boundsMap;
    public Tilemap tops;
    public Tilemap faces;
    // off for a decoration tier of high ground (Tommy, 2026-11-28: HighgroundGround2 and up, made by Tools > Garden
    // Defense > Add Highground Tier): its cliffs are drawn but lay no walls, so nothing in play changes
    [Tooltip("lay the cliff walls in play (line of sight, insects); off for a decoration tier")]
    public bool buildEdges = true;

    // neighbour bits: 0 N, 1 NE, 2 E, 3 SE, 4 S, 5 SW, 6 W, 7 NW (y goes up)
    private static readonly Vector3Int[] Offsets =
    {
        new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, -1, 0),
        new Vector3Int(0, -1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, 1, 0)
    };

    // the art's lookup tables (cliff_lookup.json): the piece for each of the 256 neighbour patterns, -1 none
    private static readonly int[] TopLut =
    {
          0,   1,   0,   2,   3,   4,   3,   5,   0,   1,   0,   2,   3,   4,   3,   5,
          6,   7,   6,   8,   9,  10,   9,  11,   6,   7,   6,   8,  12,  13,  12,  14,
          0,   1,   0,   2,   3,   4,   3,   5,   0,   1,   0,   2,   3,   4,   3,   5,
          6,   7,   6,   8,   9,  10,   9,  11,   6,   7,   6,   8,  12,  13,  12,  14,
         15,  16,  15,  17,  18,  19,  18,  20,  15,  16,  15,  17,  18,  19,  18,  20,
         21,  22,  21,  23,  24,  25,  24,  26,  21,  22,  21,  23,  27,  28,  27,  29,
         15,  16,  15,  17,  18,  19,  18,  20,  15,  16,  15,  17,  18,  19,  18,  20,
         30,  31,  30,  32,  33,  34,  33,  35,  30,  31,  30,  32,  36,  37,  36,  38,
         39,  40,  39,  41,  42,  43,  42,  44,  39,  40,  39,  41,  42,  43,  42,  44,
         45,  46,  45,  47,  48,  49,  48,  50,  45,  46,  45,  47,  51,  52,  51,  53,
         39,  40,  39,  41,  42,  43,  42,  44,  39,  40,  39,  41,  42,  43,  42,  44,
         45,  46,  45,  47,  48,  49,  48,  50,  45,  46,  45,  47,  51,  52,  51,  53,
         54,  55,  54,  56,  57,  58,  57,  59,  54,  55,  54,  56,  57,  58,  57,  59,
         60,  61,  60,  62,  63,  64,  63,  65,  60,  61,  60,  62,  66,  67,  66,  68,
         54,  55,  54,  56,  57,  58,  57,  59,  54,  55,  54,  56,  57,  58,  57,  59,
         69,  70,  69,  71,  72,  73,  72,  74,  69,  70,  69,  71,  75,  76,  75,  -1
    };
    private static readonly int[] FaceLut =
    {
         -1,   0,  -1,   1,  -1,   0,  -1,   1,  -1,   0,  -1,   1,  -1,   0,  -1,   1,
         -1,   0,  -1,   1,  -1,   0,  -1,   1,  -1,   0,  -1,   1,  -1,   0,  -1,   1,
         -1,   0,  -1,   1,  -1,   0,  -1,   1,  -1,   0,  -1,   1,  -1,   0,  -1,   1,
         -1,   0,  -1,   1,  -1,   0,  -1,   1,  -1,   0,  -1,   1,  -1,   0,  -1,   1,
          2,   3,   2,   4,   2,   3,   2,   4,   2,   3,   2,   4,   2,   3,   2,   4,
          2,   3,   2,   4,   2,   3,   2,   4,   2,   3,   2,   4,   2,   3,   2,   4,
          5,   6,   5,   7,   5,   6,   5,   7,   5,   6,   5,   7,   5,   6,   5,   7,
          5,   6,   5,   7,   5,   6,   5,   7,   5,   6,   5,   7,   5,   6,   5,   7,
          8,   9,   8,  10,   8,   9,   8,  10,   8,   9,   8,  10,   8,   9,   8,  10,
          8,   9,   8,  10,   8,   9,   8,  10,   8,   9,   8,  10,   8,   9,   8,  10,
          8,   9,   8,  10,   8,   9,   8,  10,   8,   9,   8,  10,   8,   9,   8,  10,
          8,   9,   8,  10,   8,   9,   8,  10,   8,   9,   8,  10,   8,   9,   8,  10,
         11,  12,  11,  13,  11,  12,  11,  13,  11,  12,  11,  13,  11,  12,  11,  13,
         11,  12,  11,  13,  11,  12,  11,  13,  11,  12,  11,  13,  11,  12,  11,  13,
         14,  15,  14,  16,  14,  15,  14,  16,  14,  15,  14,  16,  14,  15,  14,  16,
         14,  15,  14,  16,  14,  15,  14,  16,  14,  15,  14,  16,  14,  15,  14,  16
    };

    // the map's cells (from boundsMap's tiles), or none
    private bool _hasBounds;
    private Vector2Int _min, _max;

    private void OnEnable()
    {
        Tilemap.tilemapTileChanged += OnTilesChanged;
        Rebuild();
        if (Application.isPlaying && buildEdges) BuildEdges();
    }

    private void OnDisable()
    {
        Tilemap.tilemapTileChanged -= OnTilesChanged;
    }

    // painting raised ground in the editor redraws the cliffs (the cliff maps changing doesn't come back here)
    private void OnTilesChanged(Tilemap map, Tilemap.SyncTile[] changes)
    {
        if (Application.isPlaying || raisedMaps == null) return;
        foreach (Tilemap raised in raisedMaps)
            if (raised == map) { Rebuild(); return; }
    }

    private void FindBounds()
    {
        _hasBounds = false;
        if (boundsMap == null) return;
        foreach (Vector3Int p in boundsMap.cellBounds.allPositionsWithin)
        {
            if (!boundsMap.HasTile(p)) continue;
            if (!_hasBounds) { _min = _max = (Vector2Int)p; _hasBounds = true; continue; }
            _min = Vector2Int.Min(_min, (Vector2Int)p);
            _max = Vector2Int.Max(_max, (Vector2Int)p);
        }
    }

    private bool Raised(Vector3Int p)
    {
        if (_hasBounds)
        {
            p.x = Mathf.Clamp(p.x, _min.x, _max.x);
            p.y = Mathf.Clamp(p.y, _min.y, _max.y);
        }
        foreach (Tilemap map in raisedMaps)
            if (map != null && map.HasTile(p)) return true;
        return false;
    }

    private int Neighbours(Vector3Int p)
    {
        int bits = 0;
        for (int b = 0; b < 8; b++) if (Raised(p + Offsets[b])) bits |= 1 << b;
        return bits;
    }

    // the cells whose cliffs are drawn: the map's, or (with no bounds map) every raised cell and the ring
    // around it
    private BoundsInt Area()
    {
        if (_hasBounds) return new BoundsInt(_min.x, _min.y, 0, _max.x - _min.x + 1, _max.y - _min.y + 1, 1);
        bool any = false;
        Vector3Int min = Vector3Int.zero, max = Vector3Int.zero;
        foreach (Tilemap map in raisedMaps)
        {
            if (map == null) continue;
            foreach (Vector3Int p in map.cellBounds.allPositionsWithin)
            {
                if (!map.HasTile(p)) continue;
                if (!any) { min = max = p; any = true; continue; }
                min = Vector3Int.Min(min, p);
                max = Vector3Int.Max(max, p);
            }
        }
        if (!any) return new BoundsInt();
        return new BoundsInt(min.x - 1, min.y - 1, 0, max.x - min.x + 3, max.y - min.y + 3, 1);
    }

    [ContextMenu("Rebuild cliffs")]
    public void Rebuild()
    {
        if (cliffSet == null || tops == null || faces == null || raisedMaps == null) return;
        TileBase[] topTiles = cliffSet.topTiles ?? new TileBase[0];
        TileBase[] faceTiles = cliffSet.faceTiles ?? new TileBase[0];
        FindBounds();

        var wantTops = new Dictionary<Vector3Int, TileBase>();
        var wantFaces = new Dictionary<Vector3Int, TileBase>();
        foreach (Vector3Int p in Area().allPositionsWithin)
        {
            int bits = Neighbours(p);
            if (Raised(p))
            {
                int i = TopLut[bits];
                if (i >= 0 && i < topTiles.Length && topTiles[i] != null) wantTops[p] = topTiles[i];
            }
            else
            {
                int i = FaceLut[bits];
                if (i >= 0 && i < faceTiles.Length && faceTiles[i] != null) wantFaces[p] = faceTiles[i];
            }
        }
        Apply(tops, wantTops);
        Apply(faces, wantFaces);
    }

    // only what differs is touched, so a scene whose cliffs are already right opens unchanged
    private static void Apply(Tilemap map, Dictionary<Vector3Int, TileBase> want)
    {
        var stale = new List<Vector3Int>();
        foreach (Vector3Int p in map.cellBounds.allPositionsWithin)
            if (map.HasTile(p) && !want.ContainsKey(p)) stale.Add(p);
        foreach (Vector3Int p in stale) map.SetTile(p, null);
        foreach (KeyValuePair<Vector3Int, TileBase> kv in want)
            if (map.GetTile(kv.Key) != kv.Value) map.SetTile(kv.Key, kv.Value);
    }

    // ---------- the thin walls (play only) ----------
    private GameObject _edges;

    // every border between a raised and a low cell inside the map, straight runs joined, as edge
    // colliders on a child on the Obstacle layer
    private void BuildEdges()
    {
        if (tops == null || raisedMaps == null) return;
        if (_edges != null) Destroy(_edges);
        FindBounds();
        _edges = new GameObject("CliffEdges");
        _edges.transform.SetParent(transform, false);
        _edges.layer = LayerMask.NameToLayer("Obstacle");

        BoundsInt area = Area();
        // borders across a row: between (x, y - 1) and (x, y), running along x
        for (int y = area.yMin + 1; y < area.yMax; y++)
        {
            int runStart = int.MinValue;
            for (int x = area.xMin; x <= area.xMax; x++)
            {
                bool border = x < area.xMax && Raised(new Vector3Int(x, y, 0)) != Raised(new Vector3Int(x, y - 1, 0));
                if (border && runStart == int.MinValue) runStart = x;
                if (!border && runStart != int.MinValue) { AddEdge(new Vector3Int(runStart, y, 0), new Vector3Int(x, y, 0)); runStart = int.MinValue; }
            }
        }
        // borders down a column: between (x - 1, y) and (x, y), running along y
        for (int x = area.xMin + 1; x < area.xMax; x++)
        {
            int runStart = int.MinValue;
            for (int y = area.yMin; y <= area.yMax; y++)
            {
                bool border = y < area.yMax && Raised(new Vector3Int(x, y, 0)) != Raised(new Vector3Int(x - 1, y, 0));
                if (border && runStart == int.MinValue) runStart = y;
                if (!border && runStart != int.MinValue) { AddEdge(new Vector3Int(x, runStart, 0), new Vector3Int(x, y, 0)); runStart = int.MinValue; }
            }
        }
        // in the physics world at once, so a plant's first line of sight cast already meets them
        Physics2D.SyncTransforms();
        LightMask.MarkDirty();
    }

    // a raised cell for the line of sight grid (LightMask): its world corner and size, and its cliff top piece (null
    // when it's raised all round), whose drawn outline is where the raised ground ends (Tommy, 2026-11-28: cliff edges
    // rugged as drawn)
    public struct RaisedCell
    {
        public Vector2 corner;
        public float size;
        public Sprite top;
    }

    // every raised cell of a tier that lays walls in play (a decoration tier, buildEdges off, adds none)
    public void CollectRaised(List<RaisedCell> into)
    {
        if (!Application.isPlaying || !buildEdges || _edges == null || tops == null || raisedMaps == null) return;
        BoundsInt area = Area();
        float size = tops.layoutGrid != null ? tops.layoutGrid.cellSize.x : 1f;
        foreach (Vector3Int p in area.allPositionsWithin)
        {
            if (!Raised(p)) continue;
            into.Add(new RaisedCell { corner = tops.CellToWorld(p), size = size, top = tops.GetSprite(p) });
        }
    }

    // a wall between two cell corners (a cell's corner is its CellToWorld point)
    private void AddEdge(Vector3Int fromCorner, Vector3Int toCorner)
    {
        EdgeCollider2D edge = _edges.AddComponent<EdgeCollider2D>();
        Vector3 a = _edges.transform.InverseTransformPoint(tops.CellToWorld(fromCorner));
        Vector3 b = _edges.transform.InverseTransformPoint(tops.CellToWorld(toCorner));
        edge.points = new[] { (Vector2)a, (Vector2)b };
    }
}
