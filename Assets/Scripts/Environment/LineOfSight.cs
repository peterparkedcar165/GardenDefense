using System.Collections.Generic;
using UnityEngine;

// what a plant that needs a clear line can see (Tommy, 2026-11-27). from where it stands, Rays rays go out
// (one every half degree) to the first blocker: an obstacle tile's box (Tile.SpawnObstacleZone) or a cliff's
// edge (CliffBuilder), both on the Obstacle layer. who casts them and when is the plant's business (Plant:
// only from low ground; high ground sees over everything, down past cliffs too; flying insects are never
// hidden). the same rays decide its targeting and draw the red part of its range circle (SightShade)
public static class LineOfSight
{
    public const int Rays = 720;
    // a little thicker than the 0.1 gap between two neighbouring obstacle boxes, so no ray slips through one
    private const float RayRadius = 0.06f;
    private const float MaxDistance = 40f;
    private const float Step = 2f * Mathf.PI / Rays;

    private static int _mask = -1;
    public static int BlockMask => _mask != -1 ? _mask : (_mask = LayerMask.GetMask("Obstacle"));

    // each ray's clear distance from origin, MaxDistance where nothing blocks it
    public static void Cast(Vector2 origin, float[] into)
    {
        for (int i = 0; i < Rays; i++)
        {
            float a = i * Step;
            RaycastHit2D hit = Physics2D.CircleCast(origin, RayRadius, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), MaxDistance, BlockMask);
            into[i] = hit.collider != null ? hit.distance + RayRadius : MaxDistance;
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

    // whether a shot's step from a to b runs into a blocker, and where
    public static bool Blocked(Vector2 a, Vector2 b, out Vector2 at)
    {
        RaycastHit2D hit = Physics2D.Linecast(a, b, BlockMask);
        at = hit.collider != null ? hit.point : b;
        return hit.collider != null;
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
