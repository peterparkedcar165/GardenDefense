using System.Collections.Generic;
using UnityEngine;

// a sprite's exact pixel silhouette (Tommy, 2026-11-28: light and sight blocked by the very pixels of a tree or boulder,
// not a box or a rough outline). a pixel counts when it's nearly opaque, so the see through ground shadows under trees
// and boulders (alpha under 0.75, two overlapping ones too) never block. holes inside are filled. the tile textures
// aren't readable, so their pixels are copied off the graphics card once (Blit and ReadPixels) and kept per texture
public static class PixelMask
{
    private const byte SolidAlpha = 224;

    public class Shape
    {
        public int width, height;
        public bool[] solid;           // row by row from the bottom left, holes filled
        public Vector2 pivot;          // the sprite's pivot, in its pixels
        public float pixelsPerUnit;
        public List<Vector2[]> exact;  // outer outlines along pixel edges, in sprite units around the pivot
        public List<Vector2[]> smooth; // the same, the one pixel stairs straightened (colliders and shadow casters)

        public bool Solid(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && solid[y * width + x];
        // a pixel's middle in sprite units around the pivot
        public Vector2 Centre(int x, int y) => new Vector2((x + 0.5f - pivot.x) / pixelsPerUnit, (y + 0.5f - pivot.y) / pixelsPerUnit);
    }

    private static readonly Dictionary<Sprite, Shape> Shapes = new Dictionary<Sprite, Shape>();
    private static readonly Dictionary<Sprite, Edge> Edges = new Dictionary<Sprite, Edge>();
    private static readonly Dictionary<Texture2D, Color32[]> TexturePixels = new Dictionary<Texture2D, Color32[]>();

    // the copied textures are only needed while a level builds its obstacles
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Shapes.Clear();
        Edges.Clear();
        TexturePixels.Clear();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= DropPixels;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += DropPixels;
    }

    private static void DropPixels(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => TexturePixels.Clear();

    // null for no sprite, a tightly packed or rotated one, or one with nothing solid in it
    public static Shape Of(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;
        if (Shapes.TryGetValue(sprite, out Shape cached)) return cached;
        Shape shape = Build(sprite);
        Shapes[sprite] = shape;
        return shape;
    }

    private static Shape Build(Sprite sprite)
    {
        if (sprite.packed && (sprite.packingMode == SpritePackingMode.Tight || sprite.packingRotation != SpritePackingRotation.None)) return null;
        Color32[] pixels = Pixels(sprite.texture);
        if (pixels == null) return null;
        Rect r = sprite.packed ? sprite.textureRect : sprite.rect;
        int x0 = Mathf.RoundToInt(r.x), y0 = Mathf.RoundToInt(r.y);
        int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
        int texW = sprite.texture.width;
        if (w <= 0 || h <= 0) return null;

        bool[] solid = new bool[w * h];
        bool any = false;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (y0 + y) * texW + x0 + x;
                if (i < 0 || i >= pixels.Length || pixels[i].a < SolidAlpha) continue;
                solid[y * w + x] = true;
                any = true;
            }
        if (!any) return null;
        FillHoles(solid, w, h);

        Shape shape = new Shape
        {
            width = w, height = h, solid = solid,
            pivot = sprite.pivot, pixelsPerUnit = sprite.pixelsPerUnit,
        };
        shape.exact = new List<Vector2[]>();
        shape.smooth = new List<Vector2[]>();
        foreach (List<Vector2Int> loop in Outlines(solid, w, h))
        {
            Vector2[] exact = new Vector2[loop.Count];
            for (int i = 0; i < loop.Count; i++)
                exact[i] = new Vector2((loop[i].x - shape.pivot.x) / shape.pixelsPerUnit, (loop[i].y - shape.pivot.y) / shape.pixelsPerUnit);
            shape.exact.Add(exact);
            // specks of a few pixels block light (exact) but are too small for a collider
            if (Mathf.Abs(Area(loop)) < 6f) continue;
            Vector2[] smooth = Simplify(exact, 0.75f / shape.pixelsPerUnit);
            // a thin spike straightened from both sides can cross itself; such an outline stays exact
            if (smooth.Length < 3 || Crosses(smooth)) smooth = exact;
            shape.smooth.Add(smooth);
        }
        return shape;
    }

    // a cliff top piece's drawn edge (Tommy, 2026-11-28: cliff edges rugged as drawn): which of its pixels lie outside
    // the outline, on the low side. the piece is see through in the middle, so whatever the middle reaches without
    // crossing a drawn pixel (half opaque or more) is the raised top; the drawn outline and rim tufts are raised too;
    // the see through rest is outside
    public class Edge
    {
        public int width, height;
        public bool[] outside;
        public bool Outside(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && outside[y * width + x];
    }

    // null for no piece (a cell raised all round) or one that can't be read
    public static Edge CliffEdge(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;
        if (Edges.TryGetValue(sprite, out Edge cached)) return cached;
        Edge edge = BuildEdge(sprite);
        Edges[sprite] = edge;
        return edge;
    }

    private static Edge BuildEdge(Sprite sprite)
    {
        if (sprite.packed && (sprite.packingMode == SpritePackingMode.Tight || sprite.packingRotation != SpritePackingRotation.None)) return null;
        Color32[] pixels = Pixels(sprite.texture);
        if (pixels == null) return null;
        Rect r = sprite.packed ? sprite.textureRect : sprite.rect;
        int x0 = Mathf.RoundToInt(r.x), y0 = Mathf.RoundToInt(r.y);
        int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
        int texW = sprite.texture.width;
        if (w <= 0 || h <= 0) return null;
        bool[] drawn = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (y0 + y) * texW + x0 + x;
                drawn[y * w + x] = i >= 0 && i < pixels.Length && pixels[i].a >= 128;
            }
        // the middle, or the see through pixel nearest it
        int start = -1;
        float best = float.MaxValue;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (drawn[y * w + x]) continue;
                float dx = x + 0.5f - w * 0.5f, dy = y + 0.5f - h * 0.5f;
                float d = dx * dx + dy * dy;
                if (d < best) { best = d; start = y * w + x; }
            }
        bool[] top = new bool[w * h];
        if (start >= 0)
        {
            Stack<int> open = new Stack<int>();
            top[start] = true;
            open.Push(start);
            while (open.Count > 0)
            {
                int i = open.Pop();
                int x = i % w, y = i / w;
                void Reach(int nx, int ny)
                {
                    int j = ny * w + nx;
                    if (drawn[j] || top[j]) return;
                    top[j] = true;
                    open.Push(j);
                }
                if (x > 0) Reach(x - 1, y);
                if (x < w - 1) Reach(x + 1, y);
                if (y > 0) Reach(x, y - 1);
                if (y < h - 1) Reach(x, y + 1);
            }
        }
        bool[] outside = new bool[w * h];
        for (int i = 0; i < outside.Length; i++) outside[i] = !drawn[i] && !top[i];
        return new Edge { width = w, height = h, outside = outside };
    }

    // the texture's pixels, read straight when it's readable, otherwise copied off the graphics card
    private static Color32[] Pixels(Texture2D tex)
    {
        if (TexturePixels.TryGetValue(tex, out Color32[] cached) && cached != null) return cached;
        Color32[] result = null;
        try
        {
            if (tex.isReadable) result = tex.GetPixels32();
            else
            {
                RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                RenderTexture previous = RenderTexture.active;
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                Texture2D copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, true);
                copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                result = copy.GetPixels32();
                Object.Destroy(copy);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PixelMask] couldn't read the pixels of {tex.name}: {e.Message}");
        }
        TexturePixels[tex] = result;
        return result;
    }

    // empty pixels the outside can't reach become solid
    private static void FillHoles(bool[] solid, int w, int h)
    {
        bool[] outside = new bool[w * h];
        Stack<int> open = new Stack<int>();
        void Seed(int x, int y)
        {
            int i = y * w + x;
            if (solid[i] || outside[i]) return;
            outside[i] = true;
            open.Push(i);
        }
        for (int x = 0; x < w; x++) { Seed(x, 0); Seed(x, h - 1); }
        for (int y = 0; y < h; y++) { Seed(0, y); Seed(w - 1, y); }
        while (open.Count > 0)
        {
            int i = open.Pop();
            int x = i % w, y = i / w;
            if (x > 0) Seed(x - 1, y);
            if (x < w - 1) Seed(x + 1, y);
            if (y > 0) Seed(x, y - 1);
            if (y < h - 1) Seed(x, y + 1);
        }
        for (int i = 0; i < solid.Length; i++) if (!outside[i]) solid[i] = true;
    }

    // the outlines along pixel edges, solid on the left of each step (counter clockwise round a solid patch). where two
    // solid pixels touch only at a corner the walk turns left, so they make two outlines meeting at that point
    private static List<List<Vector2Int>> Outlines(bool[] solid, int w, int h)
    {
        bool S(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && solid[y * w + x];
        int Key(int x, int y) => y * (w + 1) + x;
        // each edge from a corner: its end and direction (0 right, 1 up, 2 left, 3 down)
        Dictionary<int, List<int>> from = new Dictionary<int, List<int>>();
        List<Vector2Int> starts = new List<Vector2Int>();
        List<int> dirs = new List<int>();
        void Add(int ax, int ay, int dir)
        {
            int k = Key(ax, ay);
            if (!from.TryGetValue(k, out List<int> list)) from[k] = list = new List<int>(2);
            list.Add(starts.Count);
            starts.Add(new Vector2Int(ax, ay));
            dirs.Add(dir);
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!solid[y * w + x]) continue;
                if (!S(x, y - 1)) Add(x, y, 0);
                if (!S(x + 1, y)) Add(x + 1, y, 1);
                if (!S(x, y + 1)) Add(x + 1, y + 1, 2);
                if (!S(x - 1, y)) Add(x, y + 1, 3);
            }
        Vector2Int[] step = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };
        bool[] used = new bool[starts.Count];
        List<List<Vector2Int>> loops = new List<List<Vector2Int>>();
        for (int first = 0; first < starts.Count; first++)
        {
            if (used[first]) continue;
            List<Vector2Int> loop = new List<Vector2Int>();
            int e = first;
            while (e >= 0 && !used[e])
            {
                used[e] = true;
                int dir = dirs[e];
                loop.Add(starts[e]);
                Vector2Int end = starts[e] + step[dir];
                int next = -1;
                if (from.TryGetValue(Key(end.x, end.y), out List<int> outs))
                {
                    int left = (dir + 1) & 3;
                    foreach (int o in outs)
                    {
                        if (used[o] && o != first) continue;
                        if (next < 0 || dirs[o] == left) next = o;
                    }
                }
                if (next == first) break;
                e = next;
            }
            loops.Add(MergeStraight(loop));
        }
        return loops;
    }

    private static float Area(List<Vector2Int> p)
    {
        float a = 0f;
        for (int i = 0; i < p.Count; i++)
        {
            Vector2Int q = p[(i + 1) % p.Count];
            a += p[i].x * q.y - q.x * p[i].y;
        }
        return a * 0.5f;
    }

    // corners only: drop points in the middle of a straight run
    private static List<Vector2Int> MergeStraight(List<Vector2Int> loop)
    {
        List<Vector2Int> result = new List<Vector2Int>(loop.Count);
        int n = loop.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2Int a = loop[(i + n - 1) % n], b = loop[i], c = loop[(i + 1) % n];
            Vector2Int d1 = b - a, d2 = c - b;
            if (d1.x * d2.y - d1.y * d2.x == 0 && d1.x * d2.x + d1.y * d2.y > 0) continue;
            result.Add(b);
        }
        return result;
    }

    // whether two sides of a closed outline that don't share a corner cross or touch
    private static bool Crosses(Vector2[] p)
    {
        int n = p.Length;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = p[i], b = p[(i + 1) % n];
            for (int j = i + 2; j < n; j++)
            {
                if (i == 0 && j == n - 1) continue;
                if (SegmentsMeet(a, b, p[j], p[(j + 1) % n])) return true;
            }
        }
        return false;
    }

    private static bool SegmentsMeet(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float Cross(Vector2 o, Vector2 u, Vector2 v) => (u.x - o.x) * (v.y - o.y) - (u.y - o.y) * (v.x - o.x);
        float d1 = Cross(c, d, a), d2 = Cross(c, d, b), d3 = Cross(a, b, c), d4 = Cross(a, b, d);
        const float eps = 1e-9f;
        if (((d1 > eps && d2 < -eps) || (d1 < -eps && d2 > eps)) && ((d3 > eps && d4 < -eps) || (d3 < -eps && d4 > eps))) return true;
        bool On(Vector2 o, Vector2 u, Vector2 q, float cr) => Mathf.Abs(cr) <= eps && q.x >= Mathf.Min(o.x, u.x) - eps && q.x <= Mathf.Max(o.x, u.x) + eps && q.y >= Mathf.Min(o.y, u.y) - eps && q.y <= Mathf.Max(o.y, u.y) + eps;
        return On(c, d, a, d1) || On(c, d, b, d2) || On(a, b, c, d3) || On(a, b, d, d4);
    }

    // a closed outline with points closer than tolerance to the line between their neighbours dropped (Douglas
    // Peucker, split at the point farthest from the first)
    private static Vector2[] Simplify(Vector2[] loop, float tolerance)
    {
        int n = loop.Length;
        if (n <= 4) return loop;
        int far = 0;
        float best = -1f;
        for (int i = 1; i < n; i++)
        {
            float d = (loop[i] - loop[0]).sqrMagnitude;
            if (d > best) { best = d; far = i; }
        }
        List<Vector2> result = new List<Vector2>();
        bool[] keep = new bool[n + 1];
        keep[0] = keep[far] = keep[n] = true;
        Vector2 P(int i) => loop[i % n];
        void Run(int a, int b)
        {
            if (b - a < 2) return;
            Vector2 pa = P(a), pb = P(b), ab = pb - pa;
            float len = ab.magnitude;
            int worst = -1;
            float worstDist = tolerance;
            for (int i = a + 1; i < b; i++)
            {
                Vector2 ap = P(i) - pa;
                float d = len < 1e-6f ? ap.magnitude : Mathf.Abs(ab.x * ap.y - ab.y * ap.x) / len;
                if (d > worstDist) { worstDist = d; worst = i; }
            }
            if (worst < 0) return;
            keep[worst] = true;
            Run(a, worst);
            Run(worst, b);
        }
        Run(0, far);
        Run(far, n);
        for (int i = 0; i < n; i++) if (keep[i]) result.Add(loop[i]);
        return result.ToArray();
    }
}
