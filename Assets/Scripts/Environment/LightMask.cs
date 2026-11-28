using System.Collections.Generic;
using UnityEngine;

// the level's light blockers at the art's own pixel size (Tommy, 2026-11-28: "really perfectly the very fine pixel"):
// every sight blocking obstacle cell's solid pixels (PixelMask, its tile's sprite) and every cliff wall (CliffBuilder,
// on the borders between pixels), on one grid lined up with the tiles' pixels. lights and the gameplay light checks
// walk it pixel by pixel: from low ground a wall or any obstacle pixel stops light, from high ground only the pixels of
// an obstacle standing on high ground (as for line of sight). a light standing in an obstacle's own cell (a lamp tile)
// isn't stopped by that cell. rebuilt when an obstacle or wall changes (Version)
public static class LightMask
{
    public const int PixelsPerUnit = 64;
    private const int MarginCells = 3;
    private const byte WallLeft = 1, WallBottom = 2;

    private static bool _dirty = true;
    private static int _version;
    // goes up whenever the blockers change, so an exact light knows to redraw
    public static int Version { get { Ensure(); return _version; } }

    private static Vector2 _origin;   // world corner of pixel (0, 0), on a cell corner
    private static int _w, _h;
    private static ushort[] _owner;   // which obstacle a pixel belongs to, 0 for none
    private static byte[] _walls;     // cliff walls on a pixel's left or bottom border
    private static readonly List<bool> _high = new List<bool> { false };
    private static bool[] _highOf = { false };
    private static readonly Dictionary<ObstacleZone, int> _ids = new Dictionary<ObstacleZone, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _dirty = true;
        _owner = null;
        _walls = null;
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
        if (!any) { _w = _h = 0; _owner = null; _walls = null; _highOf = new[] { false }; return; }
        // tiles stand on cell middles, so a cell corner is half a cell off
        _origin = min - new Vector2(0.5f + MarginCells, 0.5f + MarginCells);
        int w = Mathf.CeilToInt((max.x - min.x + 1f + 2 * MarginCells) * PixelsPerUnit);
        int h = Mathf.CeilToInt((max.y - min.y + 1f + 2 * MarginCells) * PixelsPerUnit);
        if (_owner == null || _w != w || _h != h)
        {
            _w = w; _h = h;
            _owner = new ushort[w * h];
            _walls = new byte[w * h];
        }
        else
        {
            System.Array.Clear(_owner, 0, _owner.Length);
            System.Array.Clear(_walls, 0, _walls.Length);
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

        List<Vector2> walls = new List<Vector2>();
        foreach (CliffBuilder cliffs in Object.FindObjectsByType<CliffBuilder>(FindObjectsInactive.Exclude))
            cliffs.CollectWalls(walls);
        for (int i = 0; i + 1 < walls.Count; i += 2) PaintWall(walls[i], walls[i + 1]);
        _highOf = _high.ToArray();
    }

    // an obstacle's solid pixels through its tile's matrix; a sprite drawn at another pixel size covers each of our
    // pixels whose middle it covers
    private static void PaintArt(ObstacleZone zone, int id)
    {
        PixelMask.Shape s = zone.art;
        // where the tilemap draws it: the cell's middle, through the tile's own matrix (as Tile.ShapeObstacle)
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
                        Set(toWorld.MultiplyPoint3x4(local), id);
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

    private static void Set(Vector2 world, int id)
    {
        int x = PixelX(world.x), y = PixelY(world.y);
        if (Inside(x, y)) _owner[y * _w + x] = (ushort)id;
    }

    // a wall along a pixel border, straight across or straight up
    private static void PaintWall(Vector2 a, Vector2 b)
    {
        float ax = (a.x - _origin.x) * PixelsPerUnit, ay = (a.y - _origin.y) * PixelsPerUnit;
        float bx = (b.x - _origin.x) * PixelsPerUnit, by = (b.y - _origin.y) * PixelsPerUnit;
        if (Mathf.Abs(ax - bx) < 0.5f)
        {
            int x = Mathf.RoundToInt(ax);
            int from = Mathf.RoundToInt(Mathf.Min(ay, by)), to = Mathf.RoundToInt(Mathf.Max(ay, by));
            for (int y = from; y < to; y++) if (Inside(x, y)) _walls[y * _w + x] |= WallLeft;
        }
        else if (Mathf.Abs(ay - by) < 0.5f)
        {
            int y = Mathf.RoundToInt(ay);
            int from = Mathf.RoundToInt(Mathf.Min(ax, bx)), to = Mathf.RoundToInt(Mathf.Max(ax, bx));
            for (int x = from; x < to; x++) if (Inside(x, y)) _walls[y * _w + x] |= WallBottom;
        }
    }

    private static int PixelX(float worldX) => Mathf.FloorToInt((worldX - _origin.x) * PixelsPerUnit);
    private static int PixelY(float worldY) => Mathf.FloorToInt((worldY - _origin.y) * PixelsPerUnit);
    private static bool Inside(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;
    private static Vector2 PixelCentre(int x, int y) => _origin + new Vector2((x + 0.5f) / PixelsPerUnit, (y + 0.5f) / PixelsPerUnit);

    // the world corner of the pixel grid, for lining a light's own pixels up with it
    public static Vector2 GridOrigin { get { Ensure(); return _origin; } }

    // the obstacle a pixel belongs to (0 for none), for lighting an obstacle's own art
    public static int OwnerAt(Vector2 world)
    {
        Ensure();
        if (_owner == null) return 0;
        int x = PixelX(world.x), y = PixelY(world.y);
        return Inside(x, y) ? _owner[y * _w + x] : 0;
    }

    // the obstacle whose cell a light stands in (a lamp tile), which doesn't stop its own light
    public static int OwnCell(Vector2 world)
    {
        Ensure();
        Tile t = Tile.At(world);
        if (t == null || t.tileType != TileType.Obstacle) return 0;
        ObstacleZone zone = t.GetComponentInChildren<ObstacleZone>();
        return zone != null && _ids.TryGetValue(zone, out int id) ? id : 0;
    }

    private static bool Stops(int x, int y, bool fromHigh, int skip)
    {
        if (!Inside(x, y)) return false;
        int id = _owner[y * _w + x];
        return id != 0 && id != skip && (!fromHigh || _highOf[id]);
    }

    // a wall on the border crossed stepping from pixel (x, y) one step along an axis
    private static bool WallX(int x, int y, int stepX) { int bx = stepX > 0 ? x + 1 : x; return Inside(bx, y) && (_walls[y * _w + bx] & WallLeft) != 0; }
    private static bool WallY(int x, int y, int stepY) { int by = stepY > 0 ? y + 1 : y; return Inside(x, by) && (_walls[by * _w + x] & WallBottom) != 0; }

    // how far light gets from origin along dir (normalised) before something stops it, up to maxDistance, and what
    // stopped it (an obstacle id, a negative number for a wall, 0 for nothing). a line through a pixel corner is
    // stopped only when both ways round the corner are. every light check walks this, so the plain steps are kept tight
    public static float March(Vector2 origin, Vector2 dir, float maxDistance, bool fromHigh, int skip, out int hit)
    {
        Ensure();
        hit = 0;
        ushort[] owner = _owner;
        if (owner == null) return maxDistance;
        byte[] walls = _walls;
        bool[] high = _highOf;
        int w = _w, h = _h;
        float px = (origin.x - _origin.x) * PixelsPerUnit, py = (origin.y - _origin.y) * PixelsPerUnit;
        float max = maxDistance * PixelsPerUnit;
        int x = Mathf.FloorToInt(px), y = Mathf.FloorToInt(py);
        int stepX = dir.x > 0f ? 1 : -1, stepY = dir.y > 0f ? 1 : -1;
        int wallOffX = stepX > 0 ? 1 : 0, wallOffY = stepY > 0 ? 1 : 0;
        float ax = Mathf.Abs(dir.x), ay = Mathf.Abs(dir.y);
        float tDeltaX = ax > 1e-6f ? 1f / ax : float.MaxValue;
        float tDeltaY = ay > 1e-6f ? 1f / ay : float.MaxValue;
        float tMaxX = ax > 1e-6f ? (stepX > 0 ? x + 1 - px : px - x) / ax : float.MaxValue;
        float tMaxY = ay > 1e-6f ? (stepY > 0 ? y + 1 - py : py - y) / ay : float.MaxValue;
        bool useWalls = !fromHigh;
        while (true)
        {
            float t;
            float gap = tMaxX - tMaxY;
            if (gap < 1e-4f && gap > -1e-4f)
            {
                // through a corner: blocked when going round it either way is
                t = tMaxX;
                if (t > max) return maxDistance;
                bool viaX = (useWalls && WallX(x, y, stepX)) || Stops(x + stepX, y, fromHigh, skip) || (useWalls && WallY(x + stepX, y, stepY));
                bool viaY = (useWalls && WallY(x, y, stepY)) || Stops(x, y + stepY, fromHigh, skip) || (useWalls && WallX(x, y + stepY, stepX));
                if (viaX && viaY)
                {
                    hit = Stops(x + stepX, y, fromHigh, skip) ? owner[y * w + x + stepX]
                        : Stops(x, y + stepY, fromHigh, skip) ? owner[(y + stepY) * w + x] : -1;
                    return t / PixelsPerUnit;
                }
                x += stepX; y += stepY;
                tMaxX += tDeltaX; tMaxY += tDeltaY;
            }
            else if (gap < 0f)
            {
                t = tMaxX;
                if (t > max) return maxDistance;
                if (useWalls)
                {
                    int bx = x + wallOffX;
                    if ((uint)bx < (uint)w && (uint)y < (uint)h && (walls[y * w + bx] & WallLeft) != 0) { hit = -1; return t / PixelsPerUnit; }
                }
                x += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                t = tMaxY;
                if (t > max) return maxDistance;
                if (useWalls)
                {
                    int by = y + wallOffY;
                    if ((uint)x < (uint)w && (uint)by < (uint)h && (walls[by * w + x] & WallBottom) != 0) { hit = -1; return t / PixelsPerUnit; }
                }
                y += stepY;
                tMaxY += tDeltaY;
            }
            if ((uint)x < (uint)w && (uint)y < (uint)h)
            {
                int id = owner[y * w + x];
                if (id != 0 && id != skip && (!fromHigh || high[id])) { hit = id; return t / PixelsPerUnit; }
            }
            else if (x < -1 || y < -1 || x > w || y > h) return maxDistance;
        }
    }

    // does light from a source reach a point (every gameplay light check; a plant with an exact light reads its own
    // table instead, which is the same walk)
    public static bool Reaches(Vector2 from, Vector2 to, bool fromHigh)
    {
        Vector2 d = to - from;
        float dist = d.magnitude;
        if (dist < 1e-4f) return true;
        int skip = OwnCell(from);
        return March(from, d / dist, dist, fromHigh, skip, out _) >= dist;
    }
}
