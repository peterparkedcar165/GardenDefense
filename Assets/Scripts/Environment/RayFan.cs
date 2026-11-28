using System.Collections.Generic;
using UnityEngine;

// the lines out from one middle across the light grid (LightMask), one every half pixel (or every pixel) round its
// reach, from an eye at a height (Tommy, 2026-11-28: a plant's light and its line of sight from the same middle, along
// the same lines; then heights). a plant casts one and its targeting (CanSee, InSight), its light (LightReaches,
// ExactLight) and the red part of its range circle (SightShade) all read it, so they can't disagree. along each line
// it knows where three things are lit (seen) and where not: the ground, a body standing there (a plant or an insect,
// Heights.Body over its ground) and an obstacle's picture. cast again when the middle moves, its height changes, it
// needs to reach further, or the blockers change
public sealed class RayFan
{
    // what stands at a point along a line: the ground itself, a plant's or insect's body, an obstacle's picture
    public enum Kind { Ground, Body, Art }

    public Vector2 Origin { get; private set; }
    // the eye's height over low ground (Heights)
    public float EyeZ { get; private set; }
    public float Reach { get; private set; }
    public int Version { get; private set; }
    public int Count { get; private set; }
    // per line, where each kind turns dark or lit again (from lit at the middle), as distances from it; a line's own
    // run starts at its index in the matching start array
    private readonly List<float> _ground = new List<float>(), _body = new List<float>(), _art = new List<float>();
    private int[] _groundStart = new int[1], _bodyStart = new int[1], _artStart = new int[1];

    // whether this fan still answers for a middle, a height and a reach
    public bool Fits(Vector2 origin, float eyeZ, float reach) =>
        Version == LightMask.Version && origin == Origin && Mathf.Abs(eyeZ - EyeZ) < 1e-4f && reach <= Reach;

    // raysPerPixel 2: half a pixel apart at the reach (a plant's own sight and light); 1 for a quick preview
    public static RayFan Cast(Vector2 origin, float eyeZ, float reach, RayFan reuse = null, float raysPerPixel = 2f)
    {
        RayFan fan = reuse ?? new RayFan();
        reach = Mathf.Max(0.25f, reach);
        int count = Mathf.Max(360, Mathf.CeilToInt(2f * Mathf.PI * reach * LightMask.PixelsPerUnit * raysPerPixel));
        if (fan._groundStart.Length != count + 1)
        {
            fan._groundStart = new int[count + 1];
            fan._bodyStart = new int[count + 1];
            fan._artStart = new int[count + 1];
        }
        fan._ground.Clear();
        fan._body.Clear();
        fan._art.Clear();
        fan.Count = count;
        fan.Origin = origin;
        fan.EyeZ = eyeZ;
        fan.Reach = reach;
        fan.Version = LightMask.Version;
        int skip = LightMask.OwnCell(origin);
        // with no cliff in reach nothing stops an obstacle's picture
        bool cliffs = LightMask.RaisedNear(origin, reach);
        float eyePx = eyeZ * LightMask.PixelsPerUnit;
        float step = 2f * Mathf.PI / count;
        for (int i = 0; i < count; i++)
        {
            fan._groundStart[i] = fan._ground.Count;
            fan._bodyStart[i] = fan._body.Count;
            fan._artStart[i] = fan._art.Count;
            float a = i * step;
            LightMask.MarchFan(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), reach, eyePx, skip, cliffs, fan._ground, fan._body, fan._art);
        }
        fan._groundStart[count] = fan._ground.Count;
        fan._bodyStart[count] = fan._body.Count;
        fan._artStart[count] = fan._art.Count;
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

    // is the kind of thing a distance along line i lit (seen)
    public bool Lit(int i, float distance, Kind kind)
    {
        List<float> flips;
        int from, to;
        switch (kind)
        {
            case Kind.Ground: flips = _ground; from = _groundStart[i]; to = _groundStart[i + 1]; break;
            case Kind.Body: flips = _body; from = _bodyStart[i]; to = _bodyStart[i + 1]; break;
            default: flips = _art; from = _artStart[i]; to = _artStart[i + 1]; break;
        }
        bool lit = true;
        for (int k = from; k < to && flips[k] <= distance; k++) lit = !lit;
        return lit;
    }

    // how far line i goes before the kind first turns dark (Reach when it never does), for the F8 readout
    public float FirstDark(int i, Kind kind)
    {
        switch (kind)
        {
            case Kind.Ground: return _groundStart[i] < _groundStart[i + 1] ? _ground[_groundStart[i]] : Reach;
            case Kind.Body: return _bodyStart[i] < _bodyStart[i + 1] ? _body[_bodyStart[i]] : Reach;
            default: return _artStart[i] < _artStart[i + 1] ? _art[_artStart[i]] : Reach;
        }
    }

    // is a body standing at a point seen from the middle (a plant or an insect there, or the spot it'd stand on). past
    // the reach the line to it is walked there and then
    public bool Sees(Vector2 point)
    {
        Vector2 d = point - Origin;
        float dist = d.magnitude;
        if (dist < 1e-4f) return true;
        if (dist <= Reach) return Lit(Index(d), dist, Kind.Body);
        return LineOfSight.Sees(Origin, EyeZ, point, LineOfSight.BodyAt(point));
    }

    // how many lines hide a body somewhere short of a distance (the F8 readout)
    public int HiddenWithin(float distance)
    {
        int n = 0;
        for (int i = 0; i < Count; i++) if (FirstDark(i, Kind.Body) < Mathf.Min(distance, Reach)) n++;
        return n;
    }
}
