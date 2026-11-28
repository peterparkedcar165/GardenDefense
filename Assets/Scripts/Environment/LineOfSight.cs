using UnityEngine;

// what a plant that needs a clear line can see (Tommy, 2026-11-27). from where it stands, Rays rays go out
// (one every half degree) to the first blocker: an obstacle tile's box (Tile.SpawnObstacleZone) or a cliff's
// edge (CliffBuilder), both on the Obstacle layer; a LowCollider cell's box never blocks (Tommy, 2026-11-28). from
// high ground only obstacles standing on high ground block
// (it sees down past cliffs and over everything on low ground). who casts them and when is the plant's business (Plant;
// a flying insect is hidden only when the low part of the line to it is blocked, Plant.InSight). the same rays decide its targeting and paint the red part of its range
// circle (SightShade)
public static class LineOfSight
{
    public const int Rays = 720;
    // obstacle boxes are 0.9 of a cell: two side by side leave a 0.1 gap, two meeting only at a corner a 0.14 slit
    // (0.1 times root 2) right where a ray from a tile's middle runs at 45 degrees. rays a bit thicker than the slit
    // (0.16 across) can't slip through either; at 0.12 the 45 degree ones went through diagonal tree lines (Tommy saw
    // plants target past obstacles from certain tiles)
    private const float RayRadius = 0.08f;
    public const float MaxDistance = 40f;
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

    // a LowCollider cell's box (Tommy, 2026-11-28): an obstacle for placing and for insects, but sight, shots and
    // skills pass over it
    private static bool SeeThrough(Collider2D col) => col.TryGetComponent(out ObstacleZone zone) && !zone.blocksSight;

    // from high ground only an obstacle standing on high ground blocks (Tommy, 2026-11-27, "block on same level"; for a
    // while low ones blocked too, until he saw a raised Sunflower in Level 2 blocked by bushes below): a tile's box whose
    // tile is raised. it sees down past cliff edges and over everything on low ground
    private static bool BlocksFromHigh(Collider2D col)
    {
        Transform tile = col.transform.parent;
        return tile != null && tile.TryGetComponent(out Tile t) && t.isHighground;
    }

    private static bool Blocks(Collider2D col, bool fromHigh) => !SeeThrough(col) && (!fromHigh || BlocksFromHigh(col));

    // each ray's clear distance from origin, MaxDistance where nothing blocks it. fromHigh: looking from high ground
    public static void Cast(Vector2 origin, float[] into, bool fromHigh = false)
    {
        for (int i = 0; i < Rays; i++)
        {
            float a = i * Step;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            if (!fromHigh)
            {
                // one cast is enough unless the first thing it meets is a see through box
                RaycastHit2D hit = Physics2D.CircleCast(origin, RayRadius, dir, MaxDistance, BlockMask);
                if (hit.collider == null) { into[i] = MaxDistance; continue; }
                if (!SeeThrough(hit.collider)) { into[i] = hit.distance + RayRadius; continue; }
            }
            float best = MaxDistance;
            int n = Physics2D.CircleCast(origin, RayRadius, dir, Filter, Hits, MaxDistance);
            for (int k = 0; k < n; k++)
                if (Hits[k].collider != null && Blocks(Hits[k].collider, fromHigh) && Hits[k].distance + RayRadius < best)
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
        return dist <= rays[RayIndex(Mathf.Atan2(d.y, d.x))];
    }

    // the ray nearest a direction (radians, as Atan2 gives it); targeting and the red area both use it
    public static int RayIndex(float angle)
    {
        if (angle < 0f) angle += 2f * Mathf.PI;
        return Mathf.RoundToInt(angle / Step) % Rays;
    }

    // whether a shot's step from a to b runs into a blocker, and where. fromHigh: shot from high ground
    public static bool Blocked(Vector2 a, Vector2 b, bool fromHigh, out Vector2 at)
    {
        if (!fromHigh)
        {
            RaycastHit2D hit = Physics2D.Linecast(a, b, BlockMask);
            if (hit.collider == null) { at = b; return false; }
            if (!SeeThrough(hit.collider)) { at = hit.point; return true; }
        }
        int n = Physics2D.Linecast(a, b, Filter, Hits);
        float best = float.MaxValue;
        at = b;
        for (int k = 0; k < n; k++)
        {
            if (Hits[k].collider == null || !Blocks(Hits[k].collider, fromHigh) || Hits[k].distance >= best) continue;
            best = Hits[k].distance;
            at = Hits[k].point;
        }
        return best < float.MaxValue;
    }

#if UNITY_EDITOR
    // F8 readout (editor only; Plant for a selected plant, CursorIcon while placing one): what the sight rays from
    // origin meet, to find out why a plant sees past something it shouldn't (Tommy, 2026-11-27, Level 5)
    public static void LogReadout(string who, Vector2 origin, bool fromHigh, float range, float[] rays, Tile tile)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[LineOfSight] {who} at ({origin.x:F3}, {origin.y:F3}), range {range:F2}, cast from high ground: {fromHigh}");
        if (tile != null)
            sb.AppendLine($"  tile '{tile.name}' at ({tile.transform.position.x:F3}, {tile.transform.position.y:F3}): type {tile.tileType}, isHighground {tile.isHighground}");
        else sb.AppendLine("  no tile");
        int blocked = 0;
        if (rays != null) for (int i = 0; i < Rays; i++) if (rays[i] < range) blocked++;
        sb.AppendLine("  rays blocked inside the range: " + (rays == null ? "none cast (it sees everything)" : blocked + " of " + Rays));
        Collider2D[] near = Physics2D.OverlapCircleAll(origin, range + 1f, BlockMask);
        sb.AppendLine($"  Obstacle layer colliders within {range + 1f:F1}: {near.Length}");
        foreach (Collider2D c in near)
            sb.AppendLine($"    {c.GetType().Name} '{c.name}' under '{(c.transform.parent != null ? c.transform.parent.name : "-")}' centre ({c.bounds.center.x:F2}, {c.bounds.center.y:F2}) size ({c.bounds.size.x:F2}, {c.bounds.size.y:F2}){(SeeThrough(c) ? " see through (LowCollider)" : "")}");
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI / 4f;
            RaycastHit2D hit = Physics2D.CircleCast(origin, RayRadius, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), MaxDistance, BlockMask);
            sb.AppendLine($"  ray at {k * 45} degrees: " + (hit.collider != null ? $"{hit.collider.GetType().Name} '{hit.collider.name}' under '{(hit.collider.transform.parent != null ? hit.collider.transform.parent.name : "-")}' at {hit.distance + RayRadius:F2}" : "nothing"));
        }
        int zones = 0, live = 0;
        foreach (ObstacleZone z in Object.FindObjectsByType<ObstacleZone>(FindObjectsInactive.Include))
        {
            zones++;
            if (z.isActiveAndEnabled && z.gameObject.layer == LayerMask.NameToLayer("Obstacle") && z.TryGetComponent(out Collider2D zc) && zc.enabled) live++;
        }
        sb.AppendLine($"  obstacle zones in the scene: {zones}, {live} active on the Obstacle layer; mask {BlockMask}; " +
                      $"physics: queries start in colliders {Physics2D.queriesStartInColliders}, simulation {Physics2D.simulationMode}");
        Debug.Log(sb.ToString());
    }
#endif
}
