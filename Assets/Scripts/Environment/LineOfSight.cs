using UnityEngine;

// what a plant that needs a clear line can see (Tommy, 2026-11-27), and where shots and skill lines stop. it walks a
// pixel grid of the blockers (LightMask) from the plant's middle (RayFan). since 2026-11-28 everything has a height
// (Heights): a plant's eye sits Heights.Eye over its ground and looks at bodies (Heights.Body over theirs, a flyer as
// high as it flies), and a straight line between them is stopped only by what stands above it there: from low ground a
// boulder, bush or forest hides what's behind it and the ground up on a cliff stays out of sight (a body standing near
// the top of one shows over the edge); from high ground nothing standing on low ground blocks it (Tommy, 2026-11-28: it
// sees and targets past the obstacles below, forests too, unless they stand on high ground as well). a LowCollider
// cell never blocks. shots fly along the same line, skill lines
// level at the plant's eye height. light doesn't use any of it: lights are round and go through everything (Tommy,
// 2026-11-28). insects still bump into the physics shapes (Tile.ShapeObstacle, CliffBuilder's edges)
public static class LineOfSight
{
    public const float MaxDistance = 40f;

    // the lines from a middle out to reach (a plant's own fan, or a placing preview's)
    public static RayFan Cast(Vector2 origin, float eyeZ, bool fromHigh, float reach, RayFan reuse = null, float raysPerPixel = 2f) =>
        RayFan.Cast(origin, eyeZ, fromHigh, reach, reuse, raysPerPixel);

    // the ground's height at a point: Heights.Cliff up on a cliff, 0 below
    public static float GroundAt(Vector2 p) => LightMask.LevelAt(p);

    // a body standing at a point: what an eye sees of a plant or a ground insect there
    public static float BodyAt(Vector2 p) => LightMask.LevelAt(p) + Heights.Body;

    // an insect's body over low ground: a flyer's as high as its picture flies
    public static float BodyOf(Insect insect)
    {
        Vector2 at = insect.transform.position;
        float body = Heights.Body;
        if (insect.isFlying) body = Mathf.Max(body, insect.GetAimPoint().y - insect.transform.position.y);
        return LightMask.LevelAt(at) + body;
    }

    // is the straight line from a point at one height to a point at another clear. fromHigh: a look from high ground,
    // which nothing standing on low ground stops
    public static bool Sees(Vector2 from, float fromZ, Vector2 to, float toZ, bool fromHigh)
    {
        float dist = Vector2.Distance(from, to);
        if (dist < 1e-4f) return true;
        return LightMask.Clear(from, fromZ, to, toZ, LightMask.OwnCell(from), fromHigh) >= dist - 1e-4f;
    }

    // the height of the straight line from one point and height to another, over a point along it (a shot's, on its
    // way along its plant's line of sight)
    public static float HeightAlong(Vector2 from, float fromZ, Vector2 to, float toZ, Vector2 p)
    {
        float dist = Vector2.Distance(from, to);
        if (dist < 1e-4f) return toZ;
        return Mathf.LerpUnclamped(fromZ, toZ, Vector2.Distance(from, p) / dist);
    }

    // whether a level step from a to b at a plant's eye height runs into something, and where (skill lines, a thrown
    // shield). fromHigh: the plant stands on high ground
    public static bool Blocked(Vector2 a, Vector2 b, bool fromHigh, out Vector2 at)
    {
        float eye = Heights.EyeOver(fromHigh);
        return LightMask.Blocked(a, eye, b, eye, fromHigh, out at);
    }

    // whether a step from a (za over low ground) to b (zb) runs into something, and where (a shot along its line).
    // fromHigh: shot from high ground, which nothing standing on low ground stops
    public static bool Blocked(Vector2 a, float za, Vector2 b, float zb, bool fromHigh, out Vector2 at) => LightMask.Blocked(a, za, b, zb, fromHigh, out at);

#if UNITY_EDITOR
    // F8 readout (editor only; Plant for a selected plant, CursorIcon while placing one): what the lines from origin
    // meet, to find out why a plant sees past something it shouldn't (Tommy, 2026-11-27, Level 5)
    public static void LogReadout(string who, Vector2 origin, float eyeZ, float range, RayFan fan, Tile tile)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[LineOfSight] {who} at ({origin.x:F3}, {origin.y:F3}), range {range:F2}, eye {eyeZ:F2} over low ground");
        if (tile != null)
            sb.AppendLine($"  tile '{tile.name}' at ({tile.transform.position.x:F3}, {tile.transform.position.y:F3}): type {tile.tileType}, isHighground {tile.isHighground}");
        else sb.AppendLine("  no tile");
        sb.AppendLine("  " + LightMask.Describe(origin));
        if (fan == null) sb.AppendLine("  no lines cast (it sees everything)");
        else
        {
            sb.AppendLine($"  {fan.Count} lines out to {fan.Reach:F2} from eye {fan.EyeZ:F2}, cast for version {fan.Version}; lines hiding a body inside the range: {fan.HiddenWithin(range)}");
            float limit = Mathf.Min(range, fan.Reach);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float seen = fan.FirstHidden(fan.Index(dir));
                Vector2 end = origin + dir * seen;
                sb.AppendLine($"  line at {k * 45} degrees: bodies seen for {seen:F2}" +
                              (seen < limit - 1e-3f ? $", first hidden at ({end.x:F2}, {end.y:F2}): {LightMask.Describe(end + dir * 0.01f)}" : ""));
            }
        }
        Debug.Log(sb.ToString());
    }
#endif
}
