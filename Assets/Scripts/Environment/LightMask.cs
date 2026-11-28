using System.Collections.Generic;
using UnityEngine;

// the level's blockers at the art's own pixel size (Tommy, 2026-11-28: "really perfectly the very fine pixel"), one grid
// for light and sight alike: what stops a plant's light stops its line of sight, its shots and its skill lines, from
// the same middle (RayFan, LineOfSight, ExactLight).
// on it: every sight blocking obstacle cell's solid art pixels (PixelMask, the tile's sprite; trees by their drawn
// shape, Tommy's pick), with gaps under SealPixels * 2 across sealed (he chose narrow gaps blocked for both, about the
// width sight used to need), and the raised ground of every cliff (CliffBuilder) out to its drawn rugged edge: a raised
// cell's top piece outline and rim tufts count as raised, the bit outside the outline doesn't.
// from low ground raised ground and every obstacle stop a line; from high ground only obstacles standing on high
// ground (it sees down past cliffs and over everything low). a line from inside an obstacle's own cell (a lamp tile)
// isn't stopped by that cell. rebuilt when an obstacle or cliff changes (Version)
public static class LightMask
{
    public const int PixelsPerUnit = 64;
    // gaps narrower than twice this many pixels between obstacles are closed (Tommy, 2026-11-28: "both blocked")
    public const int SealPixels = 5;
    private const int MarginCells = 3;

    private static bool _dirty = true;
    private static int _version;
    // goes up whenever the blockers change, so lights and sight know to look again
    public static int Version { get { Ensure(); return _version; } }

    private static Vector2 _origin;   // world corner of pixel (0, 0), on a cell corner
    private static int _w, _h;
    private static ushort[] _owner;   // the obstacle a blocking pixel belongs to (a sealed pixel: the nearest one), 0 none
    private static bool[] _raised;    // raised ground out to its drawn edge
    private static readonly List<bool> _high = new List<bool> { false };
    private static bool[] _highOf = { false };
    private static readonly Dictionary<ObstacleZone, int> _ids = new Dictionary<ObstacleZone, int>();
    // which cells hold any raised ground, so a light far from every cliff can skip looking for one
    private static bool[] _raisedCell;
    private static int _cellsW, _cellsH;
    // the sealing's working space, kept between builds
    private static int[] _sealDist, _sealBack;
    private static ushort[] _sealNear;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _dirty = true;
        _owner = null;
        _raised = null;
        _raisedCell = null;
        _sealDist = _sealBack = null;
        _sealNear = null;
        _ids.Clear();
        _high.Clear();
        _high.Add(false);
        _highOf = new[] { false };
    }

    public static void MarkDirty() => _dirty = true;

    private static void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;
        _version++;
        Build();
    }

    private static void Build()
    {
        _ids.Clear();
        _high.Clear();
        _high.Add(false);
        // the area: every tile, with a margin
        bool any = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;
        foreach (Tile t in Tile.allTiles.Values)
        {
            if (t == null) continue;
            Vector2 p = t.transform.position;
            if (!any) { min = max = p; any = true; }
            else { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        }
        if (!any) { _w = _h = 0; _owner = null; _raised = null; _highOf = new[] { false }; return; }
        // tiles stand on cell middles, so a cell corner is half a cell off
        _origin = min - new Vector2(0.5f + MarginCells, 0.5f + MarginCells);
        int w = Mathf.CeilToInt((max.x - min.x + 1f + 2 * MarginCells) * PixelsPerUnit);
        int h = Mathf.CeilToInt((max.y - min.y + 1f + 2 * MarginCells) * PixelsPerUnit);
        if (_owner == null || _w != w || _h != h)
        {
            _w = w; _h = h;
            _owner = new ushort[w * h];
            _raised = new bool[w * h];
        }
        else
        {
            System.Array.Clear(_owner, 0, _owner.Length);
            System.Array.Clear(_raised, 0, _raised.Length);
        }

        foreach (ObstacleZone zone in Object.FindObjectsByType<ObstacleZone>(FindObjectsInactive.Exclude))
        {
            if (!zone.blocksSight || _high.Count >= ushort.MaxValue) continue;
            int id = _high.Count;
            Tile tile = zone.transform.parent != null ? zone.transform.parent.GetComponent<Tile>() : null;
            _high.Add(tile != null && tile.isHighground);
            _ids[zone] = id;
            if (zone.art != null) PaintArt(zone, id);
            else PaintCollider(zone, id);
        }
        _highOf = _high.ToArray();
        Seal();

        List<CliffBuilder.RaisedCell> cells = new List<CliffBuilder.RaisedCell>();
        foreach (CliffBuilder cliffs in Object.FindObjectsByType<CliffBuilder>(FindObjectsInactive.Exclude))
            cliffs.CollectRaised(cells);
        _cellsW = (_w + PixelsPerUnit - 1) / PixelsPerUnit;
        _cellsH = (_h + PixelsPerUnit - 1) / PixelsPerUnit;
        _raisedCell = new bool[_cellsW * _cellsH];
        foreach (CliffBuilder.RaisedCell cell in cells) PaintRaised(cell);
    }

    // an obstacle's solid pixels where the tilemap draws them: the cell's middle, through the tile's own matrix (as
    // Tile.ShapeObstacle); a sprite drawn at another pixel size covers each of our pixels whose middle it covers
    private static void PaintArt(ObstacleZone zone, int id)
    {
        PixelMask.Shape s = zone.art;
        Matrix4x4 toWorld = Matrix4x4.Translate(zone.transform.position) * zone.artMatrix;
        int sub = Mathf.Max(1, Mathf.CeilToInt(PixelsPerUnit / s.pixelsPerUnit));
        for (int y = 0; y < s.height; y++)
            for (int x = 0; x < s.width; x++)
            {
                if (!s.solid[y * s.width + x]) continue;
                for (int sy = 0; sy < sub; sy++)
                    for (int sx = 0; sx < sub; sx++)
                    {
                        Vector2 local = new Vector2((x + (sx + 0.5f) / sub - s.pivot.x) / s.pixelsPerUnit, (y + (sy + 0.5f) / sub - s.pivot.y) / s.pixelsPerUnit);
                        Vector2 world = toWorld.MultiplyPoint3x4(local);
                        int px = PixelX(world.x), py = PixelY(world.y);
                        if (Inside(px, py)) _owner[py * _w + px] = (ushort)id;
                    }
            }
    }

    // an obstacle without art (no tile sprite found): the pixels inside its collider
    private static void PaintCollider(ObstacleZone zone, int id)
    {
        if (!zone.TryGetComponent(out Collider2D col) || !col.enabled) return;
        Bounds b = col.bounds;
        int x0 = PixelX(b.min.x), x1 = PixelX(b.max.x), y0 = PixelY(b.min.y), y1 = PixelY(b.max.y);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (Inside(x, y) && col.OverlapPoint(PixelCentre(x, y))) _owner[y * _w + x] = (ushort)id;
    }

    // closes gaps narrower than twice SealPixels between obstacles (a morphological closing: grow every obstacle by
    // SealPixels, then shrink the result back by as much, so outer edges stay where they're drawn and only narrow
    // gaps and notches fill). distances are 3 4 chamfer, in thirds of a pixel. a filled pixel belongs to the nearest
    // obstacle, so a lamp's own cell and the high ground rule still know whose it is
    private static void Seal()
    {
        int n = _w * _h;
        const int Inf = int.MaxValue / 4;
        int limit = 3 * SealPixels;
        if (_sealDist == null || _sealDist.Length != n) { _sealDist = new int[n]; _sealBack = new int[n]; _sealNear = new ushort[n]; }
        int[] dist = _sealDist;
        ushort[] near = _sealNear;
        for (int i = 0; i < n; i++)
        {
            dist[i] = _owner[i] != 0 ? 0 : Inf;
            near[i] = _owner[i];
        }
        Chamfer(dist, near);
        // grown set: within the seal distance of an obstacle. now the distance from every pixel to the nearest pixel
        // outside it
        int[] back = _sealBack;
        for (int i = 0; i < n; i++) back[i] = dist[i] <= limit ? Inf : 0;
        Chamfer(back, null);
        for (int i = 0; i < n; i++)
            if (_owner[i] == 0 && back[i] > limit && dist[i] <= limit) _owner[i] = near[i];
    }

    // a two pass chamfer distance transform, carrying the nearest source's id along when ids is given
    private static void Chamfer(int[] d, ushort[] ids)
    {
        int w = _w, h = _h;
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                int i = row + x;
                int v = d[i];
                if (v == 0) continue;
                int from = -1;
                if (x > 0 && d[i - 1] + 3 < v) { v = d[i - 1] + 3; from = i - 1; }
                if (y > 0)
                {
                    int j = i - w;
                    if (d[j] + 3 < v) { v = d[j] + 3; from = j; }
                    if (x > 0 && d[j - 1] + 4 < v) { v = d[j - 1] + 4; from = j - 1; }
                    if (x < w - 1 && d[j + 1] + 4 < v) { v = d[j + 1] + 4; from = j + 1; }
                }
                d[i] = v;
                if (ids != null && from >= 0) ids[i] = ids[from];
            }
        }
        for (int y = h - 1; y >= 0; y--)
        {
            int row = y * w;
            for (int x = w - 1; x >= 0; x--)
            {
                int i = row + x;
                int v = d[i];
                if (v == 0) continue;
                int from = -1;
                if (x < w - 1 && d[i + 1] + 3 < v) { v = d[i + 1] + 3; from = i + 1; }
                if (y < h - 1)
                {
                    int j = i + w;
                    if (d[j] + 3 < v) { v = d[j] + 3; from = j; }
                    if (x < w - 1 && d[j + 1] + 4 < v) { v = d[j + 1] + 4; from = j + 1; }
                    if (x > 0 && d[j - 1] + 4 < v) { v = d[j - 1] + 4; from = j - 1; }
                }
                d[i] = v;
                if (ids != null && from >= 0) ids[i] = ids[from];
            }
        }
    }

    // a raised cell out to its drawn edge: the whole cell, less the part of its cliff top piece outside the outline
    private static void PaintRaised(CliffBuilder.RaisedCell cell)
    {
        int x0 = Mathf.RoundToInt((cell.corner.x - _origin.x) * PixelsPerUnit);
        int y0 = Mathf.RoundToInt((cell.corner.y - _origin.y) * PixelsPerUnit);
        int size = Mathf.Max(1, Mathf.RoundToInt(cell.size * PixelsPerUnit));
        PixelMask.Edge edge = PixelMask.CliffEdge(cell.top);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (edge != null && edge.Outside(x * edge.width / size, y * edge.height / size)) continue;
                int px = x0 + x, py = y0 + y;
                if (!Inside(px, py)) continue;
                _raised[py * _w + px] = true;
                _raisedCell[(py / PixelsPerUnit) * _cellsW + px / PixelsPerUnit] = true;
            }
    }

    private static int PixelX(float worldX) => Mathf.FloorToInt((worldX - _origin.x) * PixelsPerUnit);
    private static int PixelY(float worldY) => Mathf.FloorToInt((worldY - _origin.y) * PixelsPerUnit);
    private static bool Inside(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;
    private static Vector2 PixelCentre(int x, int y) => _origin + new Vector2((x + 0.5f) / PixelsPerUnit, (y + 0.5f) / PixelsPerUnit);

    // the world corner of the pixel grid, for lining a light's own pixels up with it
    public static Vector2 GridOrigin { get { Ensure(); return _origin; } }

    // the obstacle whose cell a light or a look starts in (a lamp tile), which doesn't stop it
    public static int OwnCell(Vector2 world)
    {
        Ensure();
        Tile t = Tile.At(world);
        if (t == null || t.tileType != TileType.Obstacle) return 0;
        ObstacleZone zone = t.GetComponentInChildren<ObstacleZone>();
        return zone != null && _ids.TryGetValue(zone, out int id) ? id : 0;
    }

    // whether a pixel stops a line: raised ground for one from low ground, an obstacle's (from high ground only a raised
    // obstacle's), not the cell it started in
    private static bool Stops(int x, int y, bool fromHigh, int skip)
    {
        if ((uint)x >= (uint)_w || (uint)y >= (uint)_h) return false;
        int i = y * _w + x;
        if (!fromHigh && _raised[i]) return true;
        int id = _owner[i];
        return id != 0 && id != skip && (!fromHigh || _highOf[id]);
    }

    private static bool StopsWall(int x, int y) => (uint)x < (uint)_w && (uint)y < (uint)_h && _raised[y * _w + x];

    // any raised ground within reach of a point (a light far from every cliff needn't look for one)
    public static bool RaisedNear(Vector2 world, float reach)
    {
        Ensure();
        if (_raisedCell == null) return false;
        int x0 = Mathf.Max(0, PixelX(world.x - reach) / PixelsPerUnit), x1 = Mathf.Min(_cellsW - 1, PixelX(world.x + reach) / PixelsPerUnit);
        int y0 = Mathf.Max(0, PixelY(world.y - reach) / PixelsPerUnit), y1 = Mathf.Min(_cellsH - 1, PixelY(world.y + reach) / PixelsPerUnit);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (_raisedCell[y * _cellsW + x]) return true;
        return false;
    }

    // how far a line from origin along dir (normalised) gets, up to maxDistance: to the first pixel that stops it. walls:
    // how far it gets past obstacles, stopped only by raised ground (for a light's obstacle art, ExactLight; maxDistance
    // from high ground). a line through a pixel corner is stopped only when both ways round it are
    public static float March(Vector2 origin, Vector2 dir, float maxDistance, bool fromHigh, int skip, out float walls, bool wantWalls = false)
    {
        Ensure();
        walls = maxDistance;
        if (_owner == null) return maxDistance;
        bool[] raised = _raised;
        ushort[] owner = _owner;
        bool[] high = _highOf;
        int w = _w, h = _h;
        float px = (origin.x - _origin.x) * PixelsPerUnit, py = (origin.y - _origin.y) * PixelsPerUnit;
        float max = maxDistance * PixelsPerUnit;
        int x = Mathf.FloorToInt(px), y = Mathf.FloorToInt(py);
        int stepX = dir.x > 0f ? 1 : -1, stepY = dir.y > 0f ? 1 : -1;
        float ax = Mathf.Abs(dir.x), ay = Mathf.Abs(dir.y);
        float tDeltaX = ax > 1e-6f ? 1f / ax : float.MaxValue;
        float tDeltaY = ay > 1e-6f ? 1f / ay : float.MaxValue;
        float tMaxX = ax > 1e-6f ? (stepX > 0 ? x + 1 - px : px - x) / ax : float.MaxValue;
        float tMaxY = ay > 1e-6f ? (stepY > 0 ? y + 1 - py : py - y) / ay : float.MaxValue;
        // walls only matter from low ground; from high ground nothing stops a light's obstacle art
        bool trackWalls = wantWalls && !fromHigh;
        float ground = -1f;
        while (true)
        {
            float t;
            float gap = tMaxX - tMaxY;
            if (gap < 1e-4f && gap > -1e-4f)
            {
                // through a corner: stopped when going round it either way is
                t = tMaxX;
                if (t > max) break;
                if (ground < 0f && Stops(x + stepX, y, fromHigh, skip) && Stops(x, y + stepY, fromHigh, skip))
                {
                    ground = t;
                    if (!trackWalls) break;
                }
                if (trackWalls && StopsWall(x + stepX, y) && StopsWall(x, y + stepY)) { walls = t / PixelsPerUnit; break; }
                x += stepX; y += stepY;
                tMaxX += tDeltaX; tMaxY += tDeltaY;
            }
            else if (gap < 0f)
            {
                t = tMaxX;
                if (t > max) break;
                x += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                t = tMaxY;
                if (t > max) break;
                y += stepY;
                tMaxY += tDeltaY;
            }
            if ((uint)x < (uint)w && (uint)y < (uint)h)
            {
                int i = y * w + x;
                if (ground < 0f)
                {
                    bool stop = !fromHigh && raised[i];
                    if (!stop)
                    {
                        int id = owner[i];
                        stop = id != 0 && id != skip && (!fromHigh || high[id]);
                    }
                    if (stop)
                    {
                        ground = t;
                        if (!trackWalls) break;
                    }
                }
                if (trackWalls && raised[i]) { walls = t / PixelsPerUnit; break; }
            }
            else if (x < -1 || y < -1 || x > w || y > h) break;
        }
        return ground < 0f ? maxDistance : ground / PixelsPerUnit;
    }

    public static float March(Vector2 origin, Vector2 dir, float maxDistance, bool fromHigh, int skip) =>
        March(origin, dir, maxDistance, fromHigh, skip, out _);

    // does a line from a source reach a point (light from anything that isn't a plant with its own fan, and sight
    // past a fan's reach)
    public static bool Reaches(Vector2 from, Vector2 to, bool fromHigh)
    {
        Vector2 d = to - from;
        float dist = d.magnitude;
        if (dist < 1e-4f) return true;
        return March(from, d / dist, dist, fromHigh, OwnCell(from)) >= dist;
    }

    // whether a step from a to b runs into a blocker, and where (shots, skill lines)
    public static bool Blocked(Vector2 a, Vector2 b, bool fromHigh, out Vector2 at)
    {
        Vector2 d = b - a;
        float dist = d.magnitude;
        at = b;
        if (dist < 1e-5f) return false;
        Vector2 dir = d / dist;
        float clear = March(a, dir, dist, fromHigh, OwnCell(a));
        if (clear >= dist) return false;
        at = a + dir * clear;
        return true;
    }

#if UNITY_EDITOR
    // for the F8 readout: what the grid holds at a point
    public static string Describe(Vector2 world)
    {
        Ensure();
        if (_owner == null) return "no light grid (no tiles)";
        int x = PixelX(world.x), y = PixelY(world.y);
        if (!Inside(x, y)) return "outside the light grid";
        int i = y * _w + x;
        return $"grid {_w}x{_h} px, version {_version}; here: obstacle {_owner[i]}{(_owner[i] != 0 && _highOf[_owner[i]] ? " (raised)" : "")}, raised ground {_raised[i]}";
    }
#endif
}
