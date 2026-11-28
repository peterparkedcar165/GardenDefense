using System.Collections.Generic;
using UnityEngine;

// the lines out from one middle across the sight grid (LightMask), one every half pixel (or every pixel) round its
// reach, from an eye at a height (Tommy, 2026-11-28). a plant casts one and its targeting (CanSee, InSight) and the red
// part of its range circle (SightShade) both read it, so they can't disagree. along each line it knows where a body
// standing there (a plant or an insect, Heights.Body over its ground) is seen and where not. cast again when the
// middle moves, its height changes, it needs to reach further, or the blockers change
public sealed class RayFan
{
    public Vector2 Origin { get; private set; }
    // the eye's height over low ground (Heights)
    public float EyeZ { get; private set; }
    public float Reach { get; private set; }
    public int Version { get; private set; }
    public int Count { get; private set; }
    // per line, where a body goes out of sight or comes back into it (seen at the middle), as distances from it; a
    // line's own run starts at its index in _start
    private readonly List<float> _flips = new List<float>();
    private int[] _start = new int[1];

    // whether this fan still answers for a middle, a height and a reach
    public bool Fits(Vector2 origin, float eyeZ, float reach) =>
        Version == LightMask.Version && origin == Origin && Mathf.Abs(eyeZ - EyeZ) < 1e-4f && reach <= Reach;

    // raysPerPixel 2: half a pixel apart at the reach (a plant's own sight); 1 for a quick preview
    public static RayFan Cast(Vector2 origin, float eyeZ, float reach, RayFan reuse = null, float raysPerPixel = 2f)
    {
        RayFan fan = reuse ?? new RayFan();
        reach = Mathf.Max(0.25f, reach);
        int count = Mathf.Max(360, Mathf.CeilToInt(2f * Mathf.PI * reach * LightMask.PixelsPerUnit * raysPerPixel));
        if (fan._start.Length != count + 1) fan._start = new int[count + 1];
        fan._flips.Clear();
        fan.Count = count;
        fan.Origin = origin;
        fan.EyeZ = eyeZ;
        fan.Reach = reach;
        fan.Version = LightMask.Version;
        int skip = LightMask.OwnCell(origin);
        float eyePx = eyeZ * LightMask.PixelsPerUnit;
        float step = 2f * Mathf.PI / count;
        for (int i = 0; i < count; i++)
        {
            fan._start[i] = fan._flips.Count;
            float a = i * step;
            LightMask.MarchFan(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), reach, eyePx, skip, fan._flips);
        }
        fan._start[count] = fan._flips.Count;
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

    // is a body a distance along line i seen
    public bool Seen(int i, float distance)
    {
        bool seen = true;
        for (int k = _start[i]; k < _start[i + 1] && _flips[k] <= distance; k++) seen = !seen;
        return seen;
    }

    // how far line i goes before a body first goes out of sight (Reach when it never does), for the F8 readout
    public float FirstHidden(int i) => _start[i] < _start[i + 1] ? _flips[_start[i]] : Reach;

    // is a body standing at a point seen from the middle (a plant or an insect there, or the spot it'd stand on). past
    // the reach the line to it is walked there and then
    public bool Sees(Vector2 point)
    {
        Vector2 d = point - Origin;
        float dist = d.magnitude;
        if (dist < 1e-4f) return true;
        if (dist <= Reach) return Seen(Index(d), dist);
        return LineOfSight.Sees(Origin, EyeZ, point, LineOfSight.BodyAt(point));
    }

    // how many lines hide a body somewhere short of a distance (the F8 readout)
    public int HiddenWithin(float distance)
    {
        int n = 0;
        for (int i = 0; i < Count; i++) if (FirstHidden(i) < Mathf.Min(distance, Reach)) n++;
        return n;
    }
}
