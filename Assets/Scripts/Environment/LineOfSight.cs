using System.Collections.Generic;
using UnityEngine;

// what a plant that needs a clear line can see (Tommy, 2026-11-27). from where it stands, Rays rays go out
// (one every half degree) to the first blocker: an obstacle tile's box (Tile.SpawnObstacleZone) or a cliff's
// edge (CliffBuilder), both on the Obstacle layer. from high ground every obstacle blocks, on high ground or low,
// but the cliff edges don't (it sees down past them). who casts them and when is the plant's business (Plant;
// flying insects are never hidden). the same rays decide its targeting and draw the red part of its range
// circle (SightShade)
public static class LineOfSight
{
    public const int Rays = 720;
    // obstacle boxes are 0.9 of a cell: two side by side leave a 0.1 gap, two meeting only at a corner a 0.14 slit
    // (0.1 times root 2) right where a ray from a tile's middle runs at 45 degrees. rays a bit thicker than the slit
    // (0.16 across) can't slip through either; at 0.12 the 45 degree ones went through diagonal tree lines (Tommy saw
    // plants target past obstacles from certain tiles)
    private const float RayRadius = 0.08f;
    private const float MaxDistance = 40f;
    private const float Step = 2f * Mathf.PI / Rays;

    private static int _mask = -1;
    public static int BlockMask => _mask != -1 ? _mask : (_mask = LayerMask.GetMask("Obstacle"));

    // every blocker along a ray, nearest first (for a look from high ground, which skips some)
    private static readonly RaycastHit2D[] Hits = new RaycastHit2D[64];
    private static ContactFilter2D _filter;
    private static bool _filterReady;
    private static ContactFilter2D Filter
    {
        get
        {
            if (_filterReady) return _filter;
            _filter = new ContactFilter2D();
            _filter.SetLayerMask(BlockMask);
            _filter.useTriggers = true;
            _filterReady = true;
            return _filter;
        }
    }

    // from high ground everything blocks but a cliff edge (Tommy, 2026-11-27): it sees down past the cliffs, while
    // trees, bushes and boulders block it on high ground and low alike (at first only the ones on high ground did).
    // the cliff edges are the only edge colliders on the Obstacle layer
    private static bool BlocksFromHigh(Collider2D col) => !(col is EdgeCollider2D);

    // each ray's clear distance from origin, MaxDistance where nothing blocks it. fromHigh: looking from high ground
    public static void Cast(Vector2 origin, float[] into, bool fromHigh = false)
    {
        for (int i = 0; i < Rays; i++)
        {
            float a = i * Step;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            if (!fromHigh)
            {
                RaycastHit2D hit = Physics2D.CircleCast(origin, RayRadius, dir, MaxDistance, BlockMask);
                into[i] = hit.collider != null ? hit.distance + RayRadius : MaxDistance;
                continue;
            }
            float best = MaxDistance;
            int n = Physics2D.CircleCast(origin, RayRadius, dir, Filter, Hits, MaxDistance);
            for (int k = 0; k < n; k++)
                if (Hits[k].collider != null && BlocksFromHigh(Hits[k].collider) && Hits[k].distance + RayRadius < best)
                    best = Hits[k].distance + RayRadius;
            into[i] = best;
        }
    }

    // is point in the clear, along the ray nearest its direction
    public static bool Sees(float[] rays, Vector2 origin, Vector2 point)
    {
        Vector2 d = point - origin;
        float dist = d.magnitude;
        if (dist < 0.001f) return true;
        float a = Mathf.Atan2(d.y, d.x);
        if (a < 0f) a += 2f * Mathf.PI;
        int i = Mathf.RoundToInt(a / Step) % Rays;
        return dist <= rays[i];
    }

    // whether a shot's step from a to b runs into a blocker, and where. fromHigh: shot from high ground
    public static bool Blocked(Vector2 a, Vector2 b, bool fromHigh, out Vector2 at)
    {
        if (!fromHigh)
        {
            RaycastHit2D hit = Physics2D.Linecast(a, b, BlockMask);
            at = hit.collider != null ? hit.point : b;
            return hit.collider != null;
        }
        int n = Physics2D.Linecast(a, b, Filter, Hits);
        float best = float.MaxValue;
        at = b;
        for (int k = 0; k < n; k++)
        {
            if (Hits[k].collider == null || !BlocksFromHigh(Hits[k].collider) || Hits[k].distance >= best) continue;
            best = Hits[k].distance;
            at = Hits[k].point;
        }
        return best < float.MaxValue;
    }

    private static readonly List<Vector3> Verts = new List<Vector3>();
    private static readonly List<Color> Colors = new List<Color>();
    private static readonly List<int> Tris = new List<int>();

    // the part of a circle of radius range that the rays don't reach, as triangles in the circle's own
    // space, where the circle is 1 across (a range circle's transform is scaled to twice the range): each
    // pair of neighbouring rays adds the band from where they stop out to the edge. false if nothing's hidden
    public static bool BuildShade(Mesh mesh, float[] rays, float range, Color color)
    {
        Verts.Clear(); Colors.Clear(); Tris.Clear();
        float scale = 0.5f / Mathf.Max(0.0001f, range);
        for (int i = 0; i < Rays; i++)
        {
            int j = (i + 1) % Rays;
            float di = Mathf.Min(rays[i], range), dj = Mathf.Min(rays[j], range);
            if (di >= range && dj >= range) continue;
            Vector2 ui = new Vector2(Mathf.Cos(i * Step), Mathf.Sin(i * Step));
            Vector2 uj = new Vector2(Mathf.Cos(j * Step), Mathf.Sin(j * Step));
            int v = Verts.Count;
            Verts.Add(ui * di * scale);
            Verts.Add(ui * 0.5f);
            Verts.Add(uj * 0.5f);
            Verts.Add(uj * dj * scale);
            for (int k = 0; k < 4; k++) Colors.Add(color);
            Tris.Add(v); Tris.Add(v + 2); Tris.Add(v + 1);
            Tris.Add(v); Tris.Add(v + 3); Tris.Add(v + 2);
        }
        mesh.Clear();
        if (Verts.Count == 0) return false;
        mesh.SetVertices(Verts);
        mesh.SetColors(Colors);
        mesh.SetTriangles(Tris, 0);
        mesh.RecalculateBounds();
        return true;
    }
}
