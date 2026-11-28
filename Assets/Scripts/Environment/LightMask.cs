using System.Collections.Generic;
using UnityEngine;

// the level's blockers at the art's own pixel size, with how high each stands (Tommy, 2026-11-28: "really perfectly
// the very fine pixel", then heights): what stops a plant's line of sight stops its shots and its skill lines too,
// from the same middle (RayFan, LineOfSight). it began as the light grid; light goes through everything now (Tommy,
// 2026-11-28: round lights, no realism needed).
// on it, per pixel, how high whatever stands there reaches (Heights): raised ground out to its drawn rugged edge
// (CliffBuilder: a raised cell's top piece outline and rim tufts count, the bit outside the outline doesn't) stands
// Cliff high; every sight blocking obstacle cell's solid art pixels (PixelMask) stand on their ground as high as the
// obstacle: a boulder or bush Rock (half a taller picture), a forest cell a pine stands in Tree, while a forest cell
// that only shows the tops of pines standing below it holds nothing (their branches are up in the air). gaps under
// SealPixels * 2 across between obstacles are sealed (Tommy: narrow gaps blocked for both).
// a straight line from a light or an eye to a point is stopped where something reaches above it. a line from inside
// an obstacle's own cell (a lamp tile) isn't stopped by that cell. rebuilt when an obstacle or cliff changes (Version)
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
    private static ushort[] _owner;   // the obstacle a blocking pixel belongs to (a sealed pixel: the nearest one's), 0 none
    private static ushort[] _top;     // how high the pixel's blocker reaches over low ground, in pixels, 0 open low ground
    private static bool[] _raised;    // raised ground out to its drawn edge
    private static readonly Dictionary<ObstacleZone, int> _ids = new Dictionary<ObstacleZone, int>();
    // the sealing's working space, kept between builds
    private static int[] _sealDist, _sealBack, _sealNear;

    private const float CliffPx = Heights.Cliff * PixelsPerUnit;
    private const float BodyPx = Heights.Body * PixelsPerUnit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _dirty = true;
        _owner = null;
        _top = null;
        _raised = null;
        _sealDist = _sealBack = _sealNear = null;
        _ids.Clear();
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
        if (!any) { _w = _h = 0; _owner = null; _top = null; _raised = null; return; }
        // tiles stand on cell middles, so a cell corner is half a cell off
        _origin = min - new Vector2(0.5f + MarginCells, 0.5f + MarginCells);
        int w = Mathf.CeilToInt((max.x - min.x + 1f + 2 * MarginCells) * PixelsPerUnit);
        int h = Mathf.CeilToInt((max.y - min.y + 1f + 2 * MarginCells) * PixelsPerUnit);
        if (_owner == null || _w != w || _h != h)
        {
            _w = w; _h = h;
            _owner = new ushort[w * h];
            _top = new ushort[w * h];
            _raised = new bool[w * h];
        }
        else
        {
            System.Array.Clear(_owner, 0, _owner.Length);
            System.Array.Clear(_top, 0, _top.Length);
            System.Array.Clear(_raised, 0, _raised.Length);
        }

        int next = 1;
        foreach (ObstacleZone zone in Object.FindObjectsByType<ObstacleZone>(FindObjectsInactive.Exclude))
        {
            if (!zone.blocksSight || next >= ushort.MaxValue) continue;
            // only the tops of pines standing below: nothing stands on this cell's ground
            if (zone.forest && !zone.pineStandsHere) continue;
            int id = next++;
            _ids[zone] = id;
            Tile tile = zone.transform.parent != null ? zone.transform.parent.GetComponent<Tile>() : null;
            float ground = tile != null && tile.isHighground ? Heights.Cliff : 0f;
            if (zone.art != null) PaintArt(zone, id, ground + Stands(zone));
            else PaintCollider(zone, id, ground + Heights.Rock);
        }
        Seal();

        List<CliffBuilder.RaisedCell> cells = new List<CliffBuilder.RaisedCell>();
        foreach (CliffBuilder cliffs in Object.FindObjectsByType<CliffBuilder>(FindObjectsInactive.Exclude))
            cliffs.CollectRaised(cells);
        foreach (CliffBuilder.RaisedCell cell in cells) PaintRaised(cell);
    }

    // how high an obstacle with art stands over its ground: a pine Tree, anything else half its tallest drawn column
    // and at least Rock
    private static float Stands(ObstacleZone zone)
    {
        if (zone.forest) return Heights.Tree;
        PixelMask.Shape s = zone.art;
        int tallest = 0;
        for (int x = 0; x < s.width; x++)
        {
            int column = 0;
            for (int y = 0; y < s.height; y++) if (s.solid[y * s.width + x]) column++;
            if (column > tallest) tallest = column;
        }
        float drawn = tallest / s.pixelsPerUnit * Mathf.Abs(zone.artMatrix.m11);
        return Mathf.Max(Heights.Rock, drawn * 0.5f);
    }

    private static ushort Px(float tiles) => (ushort)Mathf.Clamp(Mathf.RoundToInt(tiles * PixelsPerUnit), 1, ushort.MaxValue);

    // an obstacle's solid pixels where the tilemap draws them: the cell's middle, through the tile's own matrix (as
    // Tile.ShapeObstacle); a sprite drawn at another pixel size covers each of our pixels whose middle it covers
    private static void PaintArt(ObstacleZone zone, int id, float top)
    {
        PixelMask.Shape s = zone.art;
        ushort topPx = Px(top);
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
                        if (Inside(px, py)) Mark(py * _w + px, id, topPx);
                    }
            }
    }

    // an obstacle without art (no tile sprite found): the pixels inside its collider
    private static void PaintCollider(ObstacleZone zone, int id, float top)
    {
        if (!zone.TryGetComponent(out Collider2D col) || !col.enabled) return;
        ushort topPx = Px(top);
        Bounds b = col.bounds;
        int x0 = PixelX(b.min.x), x1 = PixelX(b.max.x), y0 = PixelY(b.min.y), y1 = PixelY(b.max.y);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (Inside(x, y) && col.OverlapPoint(PixelCentre(x, y))) Mark(y * _w + x, id, topPx);
    }

    private static void Mark(int i, int id, ushort top)
    {
        _owner[i] = (ushort)id;
        if (top > _top[i]) _top[i] = top;
    }

    // closes gaps narrower than twice SealPixels between obstacles (a morphological closing: grow every obstacle by
    // SealPixels, then shrink the result back by as much, so outer edges stay where they're drawn and only narrow
    // gaps and notches fill). distances are 3 4 chamfer, in thirds of a pixel. a filled pixel takes the nearest
    // obstacle's pixel, whose it is and how high, so a lamp's own cell still knows whose it is
    private static void Seal()
    {
        int n = _w * _h;
        const int Inf = int.MaxValue / 4;
        int limit = 3 * SealPixels;
        if (_sealDist == null || _sealDist.Length != n) { _sealDist = new int[n]; _sealBack = new int[n]; _sealNear = new int[n]; }
        int[] dist = _sealDist;
        int[] near = _sealNear;
        for (int i = 0; i < n; i++)
        {
            bool solid = _owner[i] != 0;
            dist[i] = solid ? 0 : Inf;
            near[i] = solid ? i : -1;
        }
        Chamfer(dist, near);
        // grown set: within the seal distance of an obstacle. now the distance from every pixel to the nearest pixel
        // outside it
        int[] back = _sealBack;
        for (int i = 0; i < n; i++) back[i] = dist[i] <= limit ? Inf : 0;
        Chamfer(back, null);
        for (int i = 0; i < n; i++)
        {
            if (_owner[i] != 0 || back[i] <= limit || dist[i] > limit || near[i] < 0) continue;
            _owner[i] = _owner[near[i]];
            _top[i] = _top[near[i]];
        }
    }

    // a two pass chamfer distance transform, carrying the nearest source pixel along when near is given
    private static void Chamfer(int[] d, int[] near)
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
                if (near != null && from >= 0) near[i] = near[from];
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
                if (near != null && from >= 0) near[i] = near[from];
            }
        }
    }

    // a raised cell out to its drawn edge: the whole cell, less the part of its cliff top piece outside the outline
    private static void PaintRaised(CliffBuilder.RaisedCell cell)
    {
        int x0 = Mathf.RoundToInt((cell.corner.x - _origin.x) * PixelsPerUnit);
        int y0 = Mathf.RoundToInt((cell.corner.y - _origin.y) * PixelsPerUnit);
        int size = Mathf.Max(1, Mathf.RoundToInt(cell.size * PixelsPerUnit));
        ushort cliff = Px(Heights.Cliff);
        PixelMask.Edge edge = PixelMask.CliffEdge(cell.top);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (edge != null && edge.Outside(x * edge.width / size, y * edge.height / size)) continue;
                int px = x0 + x, py = y0 + y;
                if (!Inside(px, py)) continue;
                int i = py * _w + px;
                _raised[i] = true;
                if (cliff > _top[i]) _top[i] = cliff;
            }
    }

    private static int PixelX(float worldX) => Mathf.FloorToInt((worldX - _origin.x) * PixelsPerUnit);
    private static int PixelY(float worldY) => Mathf.FloorToInt((worldY - _origin.y) * PixelsPerUnit);
    private static bool Inside(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;
    private static Vector2 PixelCentre(int x, int y) => _origin + new Vector2((x + 0.5f) / PixelsPerUnit, (y + 0.5f) / PixelsPerUnit);

    // the world corner of the pixel grid, for lining a light's own pixels up with it
    public static Vector2 GridOrigin { get { Ensure(); return _origin; } }

    // the ground's height at a point: Heights.Cliff on raised ground (out to its drawn edge), 0 below
    public static float LevelAt(Vector2 world)
    {
        Ensure();
        if (_raised == null) return 0f;
        int x = PixelX(world.x), y = PixelY(world.y);
        return Inside(x, y) && _raised[y * _w + x] ? Heights.Cliff : 0f;
    }

    // the obstacle whose cell a light or a look starts in (a lamp tile), which doesn't stop it
    public static int OwnCell(Vector2 world)
    {
        Ensure();
        Tile t = Tile.At(world);
        if (t == null || t.tileType != TileType.Obstacle) return 0;
        ObstacleZone zone = t.GetComponentInChildren<ObstacleZone>();
        return zone != null && _ids.TryGetValue(zone, out int id) ? id : 0;
    }

    // how high a pixel's blocker reaches, in pixels: the ground alone for the cell a line started in, nothing off the grid
    private static float TopAt(int x, int y, int skip)
    {
        if ((uint)x >= (uint)_w || (uint)y >= (uint)_h) return 0f;
        int i = y * _w + x;
        if (skip != 0 && _owner[i] == skip) return _raised[i] ? CliffPx : 0f;
        return _top[i];
    }

    // one line of a fan (RayFan): from origin along dir (normalised) out to maxDistance, from an eye eyePx pixels over
    // low ground. it notes where a body standing along it (Heights.Body over its ground) goes out of sight or comes
    // back into it, as distances from origin, starting seen. a body is seen when the line to it rises more steeply than
    // the line to the top of everything before it (the steepest so far). a line through a pixel corner is stopped only
    // as high as the lower pixel beside it
    internal static void MarchFan(Vector2 origin, Vector2 dir, float maxDistance, float eyePx, int skip, List<float> body)
    {
        Ensure();
        if (_top == null) return;
        ushort[] top = _top, owner = _owner;
        bool[] raised = _raised;
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
        // the highest a body stands: up on a cliff
        float highest = CliffPx + BodyPx - eyePx;
        float steepest = float.NegativeInfinity;
        bool seen = true;
        const float Near = 1e-3f;
        while (true)
        {
            float tIn;
            float gap = tMaxX - tMaxY;
            if (gap < 1e-4f && gap > -1e-4f)
            {
                // through a corner: what stands beside it stops the line only as high as the lower of the two
                tIn = tMaxX;
                if (tIn > max) break;
                float at = Mathf.Max(tIn, Near);
                float side = Mathf.Min(TopAt(x + stepX, y, skip), TopAt(x, y + stepY, skip));
                if (side > 0f) steepest = Mathf.Max(steepest, (side - eyePx) / at);
                x += stepX; y += stepY;
                tMaxX += tDeltaX; tMaxY += tDeltaY;
            }
            else if (gap < 0f)
            {
                tIn = tMaxX;
                if (tIn > max) break;
                x += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                tIn = tMaxY;
                if (tIn > max) break;
                y += stepY;
                tMaxY += tDeltaY;
            }
            if ((uint)x >= (uint)w || (uint)y >= (uint)h) break;
            int i = y * w + x;
            float tOut = Mathf.Max(Mathf.Min(tMaxX, tMaxY), Near);
            float mid = Mathf.Max(0.5f * (tIn + tOut), Near);

            // a body standing here, seen past everything before it
            float level = raised[i] ? CliffPx : 0f;
            bool b = (level + BodyPx - eyePx) / mid > steepest;
            if (b != seen) { seen = b; body.Add(tIn / PixelsPerUnit); }

            // and how high what's here stands, for what lies past it: the slope to its top, at its near side when it
            // reaches over the eye, at its far side when it stays under
            float stands = skip != 0 && owner[i] == skip ? level : top[i];
            if (stands > 0f)
            {
                float s = (stands - eyePx) / (stands >= eyePx ? Mathf.Max(tIn, Near) : tOut);
                if (s > steepest) steepest = s;
            }

            // nothing further on can be seen any more: the rest stays hidden
            if (steepest >= 0f && steepest * tOut >= highest)
            {
                if (seen) body.Add(tOut / PixelsPerUnit);
                break;
            }
        }
    }

    // how far a straight line gets from a (za tiles over low ground) toward b (zb), before something stands above it:
    // the whole way when nothing does. a line through a pixel corner is stopped only when both pixels beside it are
    public static float Clear(Vector2 a, float za, Vector2 b, float zb, int skip)
    {
        Ensure();
        Vector2 d = b - a;
        float dist = d.magnitude;
        if (_top == null || dist < 1e-5f) return dist;
        Vector2 dir = d / dist;
        float px = (a.x - _origin.x) * PixelsPerUnit, py = (a.y - _origin.y) * PixelsPerUnit;
        float max = dist * PixelsPerUnit;
        float zFrom = za * PixelsPerUnit, rise = (zb - za) * PixelsPerUnit / max;
        int x = Mathf.FloorToInt(px), y = Mathf.FloorToInt(py);
        int stepX = dir.x > 0f ? 1 : -1, stepY = dir.y > 0f ? 1 : -1;
        float ax = Mathf.Abs(dir.x), ay = Mathf.Abs(dir.y);
        float tDeltaX = ax > 1e-6f ? 1f / ax : float.MaxValue;
        float tDeltaY = ay > 1e-6f ? 1f / ay : float.MaxValue;
        float tMaxX = ax > 1e-6f ? (stepX > 0 ? x + 1 - px : px - x) / ax : float.MaxValue;
        float tMaxY = ay > 1e-6f ? (stepY > 0 ? y + 1 - py : py - y) / ay : float.MaxValue;
        while (true)
        {
            float tIn;
            float gap = tMaxX - tMaxY;
            if (gap < 1e-4f && gap > -1e-4f)
            {
                tIn = tMaxX;
                if (tIn > max) break;
                float hAt = zFrom + rise * tIn;
                if (TopAt(x + stepX, y, skip) > hAt && TopAt(x, y + stepY, skip) > hAt) return tIn / PixelsPerUnit;
                x += stepX; y += stepY;
                tMaxX += tDeltaX; tMaxY += tDeltaY;
            }
            else if (gap < 0f)
            {
                tIn = tMaxX;
                if (tIn > max) break;
                x += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                tIn = tMaxY;
                if (tIn > max) break;
                y += stepY;
                tMaxY += tDeltaY;
            }
            if ((uint)x >= (uint)_w || (uint)y >= (uint)_h)
            {
                if (x < -1 || y < -1 || x > _w || y > _h) break;
                continue;
            }
            float tOut = Mathf.Min(Mathf.Min(tMaxX, tMaxY), max);
            // the line's lowest over this pixel
            float low = Mathf.Min(zFrom + rise * tIn, zFrom + rise * tOut);
            if (TopAt(x, y, skip) > low) return tIn / PixelsPerUnit;
        }
        return dist;
    }

    // whether a straight line from a (za) to b (zb) runs into something, and where (on the ground under it)
    public static bool Blocked(Vector2 a, float za, Vector2 b, float zb, out Vector2 at)
    {
        at = b;
        Vector2 d = b - a;
        float dist = d.magnitude;
        if (dist < 1e-5f) return false;
        float clear = Clear(a, za, b, zb, OwnCell(a));
        if (clear >= dist - 1e-5f) return false;
        at = a + d / dist * clear;
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
        return $"grid {_w}x{_h} px, version {_version}; here: obstacle {_owner[i]}, stands {_top[i] / (float)PixelsPerUnit:F2} high, raised ground {_raised[i]}";
    }
#endif
}
