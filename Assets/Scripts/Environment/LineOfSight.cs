using UnityEngine;

// what a plant that needs a clear line can see (Tommy, 2026-11-27), and where shots and skill lines stop. since
// 2026-11-28 it walks the same pixel grid as light (LightMask), from the same middle along the same lines (RayFan), so
// a plant can target exactly what its light reaches: obstacles by their drawn pixels (trees too, Tommy's pick), gaps
// under ten pixels sealed (he chose them blocked for both), cliffs out to their drawn rugged edge; a LowCollider cell
// never blocks. from high ground only obstacles standing on high ground block (it sees down past cliffs and over
// everything on low ground). who casts and when is the plant's business (Plant; a flying insect is hidden only when the
// low part of the line to it is blocked, Plant.InSight). the same lines decide its targeting and paint the red part of
// its range circle (SightShade). insects still bump into the physics shapes (Tile.ShapeObstacle, CliffBuilder's edges)
public static class LineOfSight
{
    public const float MaxDistance = 40f;

    private static int _mask = -1;
    public static int BlockMask => _mask != -1 ? _mask : (_mask = LayerMask.GetMask("Obstacle"));

    // the lines from a middle out to reach (a plant's own fan, or a placing preview's)
    public static RayFan Cast(Vector2 origin, bool fromHigh, float reach, RayFan reuse = null, float raysPerPixel = 2f, bool walls = false) =>
        RayFan.Cast(origin, fromHigh, reach, reuse, raysPerPixel, walls);

    // does light from a source reach a point (Tommy, 2026-11-28: every light, gameplay first): the same blockers as
    // sight, from the source's ground; a light standing in an obstacle's own cell (a lamp tile) isn't blocked by that cell
    public static bool LightReaches(Vector2 from, Vector2 to, bool fromHigh) => LightMask.Reaches(from, to, fromHigh);

    // whether a shot's step from a to b runs into a blocker, and where. fromHigh: shot from high ground
    public static bool Blocked(Vector2 a, Vector2 b, bool fromHigh, out Vector2 at) => LightMask.Blocked(a, b, fromHigh, out at);

#if UNITY_EDITOR
    // F8 readout (editor only; Plant for a selected plant, CursorIcon while placing one): what the lines from origin
    // meet, to find out why a plant sees past something it shouldn't (Tommy, 2026-11-27, Level 5)
    public static void LogReadout(string who, Vector2 origin, bool fromHigh, float range, RayFan fan, Tile tile)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[LineOfSight] {who} at ({origin.x:F3}, {origin.y:F3}), range {range:F2}, from high ground: {fromHigh}");
        if (tile != null)
            sb.AppendLine($"  tile '{tile.name}' at ({tile.transform.position.x:F3}, {tile.transform.position.y:F3}): type {tile.tileType}, isHighground {tile.isHighground}");
        else sb.AppendLine("  no tile");
        sb.AppendLine("  " + LightMask.Describe(origin));
        if (fan == null) sb.AppendLine("  no lines cast (it sees everything)");
        else
        {
            sb.AppendLine($"  {fan.Count} lines out to {fan.Reach:F2}, cast for version {fan.Version}; blocked inside the range: {fan.BlockedWithin(range)}");
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float clear = fan.Clear(origin + dir * Mathf.Min(range, fan.Reach));
                Vector2 end = origin + dir * clear;
                sb.AppendLine($"  line at {k * 45} degrees: clear for {clear:F2}" + (clear < Mathf.Min(range, fan.Reach) - 1e-3f ? $", stopped at ({end.x:F2}, {end.y:F2}): {LightMask.Describe(end + dir * 0.01f)}" : ""));
            }
        }
        Debug.Log(sb.ToString());
    }
#endif
}
