using UnityEngine;

// the lines out from one middle across the light grid (LightMask), one every half pixel (or every pixel) round its
// reach (Tommy, 2026-11-28: a plant's light and its line of sight from the same middle, along the same lines). a plant
// casts one and its targeting (CanSee, InSight), its light (LightReaches, ExactLight) and the red part of its range
// circle (SightShade) all read it, so they can't disagree. cast again when the middle moves, its ground changes, it
// needs to reach further, or the blockers change
public sealed class RayFan
{
    public Vector2 Origin { get; private set; }
    public bool FromHigh { get; private set; }
    public float Reach { get; private set; }
    public int Version { get; private set; }
    public bool HasWalls { get; private set; }
    public int Count { get; private set; }
    private float[] _ground = new float[0];
    private float[] _walls = new float[0];

    // whether this fan still answers for a middle, ground and reach (and walls, for a light's obstacle art)
    public bool Fits(Vector2 origin, bool fromHigh, float reach, bool walls) =>
        Version == LightMask.Version && origin == Origin && fromHigh == FromHigh && reach <= Reach && (!walls || HasWalls);

    // raysPerPixel 2: half a pixel apart at the reach (a plant's own sight and light); 1 for a quick preview
    public static RayFan Cast(Vector2 origin, bool fromHigh, float reach, RayFan reuse = null, float raysPerPixel = 2f, bool walls = false)
    {
        RayFan fan = reuse ?? new RayFan();
        reach = Mathf.Max(0.25f, reach);
        int count = Mathf.Max(360, Mathf.CeilToInt(2f * Mathf.PI * reach * LightMask.PixelsPerUnit * raysPerPixel));
        if (fan._ground.Length != count) { fan._ground = new float[count]; fan._walls = new float[count]; }
        fan.Count = count;
        fan.Origin = origin;
        fan.FromHigh = fromHigh;
        fan.Reach = reach;
        fan.HasWalls = walls;
        fan.Version = LightMask.Version;
        int skip = LightMask.OwnCell(origin);
        // with no cliff in reach (or from high ground) nothing stops a light's obstacle art, so the lines end at their
        // first blocker
        bool lookForWalls = walls && !fromHigh && LightMask.RaisedNear(origin, reach);
        float step = 2f * Mathf.PI / count;
        for (int i = 0; i < count; i++)
        {
            float a = i * step;
            fan._ground[i] = LightMask.March(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), reach, fromHigh, skip, out fan._walls[i], lookForWalls);
        }
        return fan;
    }

    // the line nearest a direction
    public int Index(Vector2 d)
    {
        float a = Mathf.Atan2(d.y, d.x) / (2f * Mathf.PI);
        if (a < 0f) a += 1f;
        return Index01(a);
    }

    // the line nearest a turn (0 to 1 round from the right, counter clockwise)
    public int Index01(float turn) => Mathf.RoundToInt(turn * Count) % Count;

    // how far line i gets before something stops it (Reach when nothing does)
    public float Ground(int i) => _ground[i];
    // how far line i gets past obstacles, stopped only by raised ground (Reach when nothing does)
    public float Walls(int i) => _walls[i];

    // how far the line toward a point gets, walked exactly when the point is past the reach
    public float Clear(Vector2 point)
    {
        Vector2 d = point - Origin;
        float dist = d.magnitude;
        if (dist < 1e-4f) return 0f;
        if (dist <= Reach) return _ground[Index(d)];
        return LightMask.March(Origin, d / dist, dist, FromHigh, LightMask.OwnCell(Origin));
    }

    // is a point in the clear from the middle
    public bool Sees(Vector2 point)
    {
        Vector2 d = point - Origin;
        float dist = d.magnitude;
        if (dist < 1e-4f) return true;
        if (dist <= Reach) return dist <= _ground[Index(d)];
        return LightMask.Reaches(Origin, point, FromHigh);
    }

    // how many lines are stopped short of a distance (the F8 readout)
    public int BlockedWithin(float distance)
    {
        int n = 0;
        for (int i = 0; i < Count; i++) if (_ground[i] < Mathf.Min(distance, Reach)) n++;
        return n;
    }
}
