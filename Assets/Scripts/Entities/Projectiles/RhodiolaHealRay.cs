using UnityEngine;

// Rhodiola's heal ray (Tommy, 2026-11-27, Downloads/gd_rhodiola/heal_ray: his beam at 75% and spiral at 50%, in
// Resources, sliced on import). while she channels into an ally a spiral grows at the point of the crystal on her
// staff (RhodiolaHealSpiralStart, once), then the beam shoots from there to the ally in ExtendTime and keeps playing
// (RhodiolaHealBeam, its frames already flow toward the ally) while the spiral loops under it (RhodiolaHealSpiralLoop,
// a quarter turn per loop). when the heal ends the beam fades out over FadeTime and the spiral finishes its quarter
// turn, then shrinks away (RhodiolaHealSpiralEnd). 130 ms a frame on game time, like her animation. both follow the
// crystal every frame, wherever her animation has it, and are drawn like her (her sorting layer and material) just
// above her and the plants, the beam over the spiral. not parented to her, so nothing that looks through her
// children finds them; they go when she does
public class RhodiolaHealRay : MonoBehaviour
{
    private const float FrameTime = 0.13f;
    private const float ExtendTime = 0.12f;
    private const float FadeTime = 0.25f;
    // the beam's frames are 24 px tall at 64 px a unit
    private const float BeamHeight = 24f / 64f;
    // the spiral sits this far above her own sprite's order, the beam one more
    private const int SpiralOrderAbove = 4;

    private static Sprite[] _beamFrames, _spiralStart, _spiralLoop, _spiralEnd;

    private enum SpiralState { Off, Start, Loop, End }
    private SpiralState _state = SpiralState.Off;

    private Component _owner;
    private System.Func<Vector3> _origin;
    private SpriteRenderer _beam, _spiral;
    private Entity _target;
    private Vector3 _targetPoint;
    private bool _stopRequested, _beamOn, _beamFading;
    private int _spiralFrame, _beamFrame;
    // a beam with no spiral (Widespread Bloom's, from the plant she heals on to the next): it shoots at Begin
    private bool _beamOnly;
    private float _frameTimer, _extendT, _fadeT;

    // how long after Begin the beam reaches the ally: the spiral's start, then the beam's shot
    public static float LeadSeconds => Load() ? _spiralStart.Length * FrameTime + ExtendTime : 0f;

    private static bool Load()
    {
        if (_beamFrames == null)
        {
            _beamFrames = Frames("RhodiolaHealBeam");
            _spiralStart = Frames("RhodiolaHealSpiralStart");
            _spiralLoop = Frames("RhodiolaHealSpiralLoop");
            _spiralEnd = Frames("RhodiolaHealSpiralEnd");
        }
        return _beamFrames.Length > 0 && _spiralStart.Length > 0 && _spiralLoop.Length > 0 && _spiralEnd.Length > 0;
    }

    private static Sprite[] Frames(string sheet)
    {
        Sprite[] frames = Resources.LoadAll<Sprite>(sheet);
        System.Array.Sort(frames, (a, b) => FrameIndex(a).CompareTo(FrameIndex(b)));
        return frames;
    }

    private static int FrameIndex(Sprite s)
    {
        int cut = s.name.LastIndexOf('_');
        return cut >= 0 && int.TryParse(s.name.Substring(cut + 1), out int i) ? i : 0;
    }

    // owner: who it belongs to (it goes when they do). look: the sprite it's drawn like. origin: where the ray leaves
    // her, asked every frame. spiral: false for the beam alone. null when the art isn't there
    public static RhodiolaHealRay Create(Component owner, SpriteRenderer look, System.Func<Vector3> origin, bool spiral = true)
    {
        if (!Load()) return null;
        GameObject go = new GameObject("RhodiolaHealRay");
        RhodiolaHealRay ray = go.AddComponent<RhodiolaHealRay>();
        ray._owner = owner;
        ray._origin = origin;
        ray._beamOnly = !spiral;
        ray._spiral = ray.MakeRenderer("HealSpiral", look, SpiralOrderAbove);
        ray._beam = ray.MakeRenderer("HealBeam", look, SpiralOrderAbove + 1);
        ray._beam.drawMode = SpriteDrawMode.Tiled;
        ray._beam.tileMode = SpriteTileMode.Continuous;
        return ray;
    }

    private SpriteRenderer MakeRenderer(string objName, SpriteRenderer look, int above)
    {
        GameObject go = new GameObject(objName);
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (look != null)
        {
            // never her hit flash's material (she may be mid flash right now): the default sprite material then
            Material m = look.sharedMaterial;
            if (m != null && m.shader != null && m.shader.name != "Custom/SpriteHitFlash") sr.sharedMaterial = m;
            sr.sortingLayerID = look.sortingLayerID;
            sr.sortingOrder = look.sortingOrder + above;
        }
        sr.enabled = false;
        return sr;
    }

    // she starts healing this ally. again while the ray is going it only moves the beam to the new one
    public void Begin(Entity ally)
    {
        _target = ally;
        if (ally != null) _targetPoint = ally.VisualCenter;
        _stopRequested = false;
        if (_beamOnly)
        {
            if (!_beamOn || _beamFading) StartBeam();
        }
        else if (_state == SpiralState.Off || _state == SpiralState.End)
        {
            _state = SpiralState.Start;
            _spiralFrame = 0;
            _frameTimer = 0f;
            _spiral.enabled = true;
            ShowSpiral();
        }
        else if (_state == SpiralState.Loop && (!_beamOn || _beamFading))
        {
            StartBeam();
        }
        Place();
    }

    // the heal ends: the beam fades out, the spiral finishes its quarter turn and shrinks away
    public void Stop()
    {
        _stopRequested = true;
        if (_beamOn && !_beamFading) { _beamFading = true; _fadeT = 0f; }
    }

    // gone at once, nothing left to finish (she was switched off)
    public void Hide()
    {
        _state = SpiralState.Off;
        _beamOn = _beamFading = false;
        _stopRequested = false;
        _target = null;
        if (_spiral != null) _spiral.enabled = false;
        if (_beam != null) _beam.enabled = false;
    }

    // after her animation has picked this frame's sprite, so the ray sits on the crystal of the frame she shows
    private void LateUpdate()
    {
        if (_owner == null) { Destroy(gameObject); return; }
        if (_state == SpiralState.Off && !_beamOn) return;

        _frameTimer += Time.deltaTime;
        while (_frameTimer >= FrameTime)
        {
            _frameTimer -= FrameTime;
            Step();
        }

        if (_beamOn)
        {
            if (_beamFading)
            {
                _fadeT += Time.deltaTime / FadeTime;
                _beam.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - _fadeT));
                if (_fadeT >= 1f) { _beamOn = false; _beamFading = false; _beam.enabled = false; }
            }
            else if (_extendT < 1f)
            {
                _extendT = Mathf.Min(1f, _extendT + Time.deltaTime / ExtendTime);
            }
        }

        Place();
    }

    private void Step()
    {
        if (_beamOn) { _beamFrame = (_beamFrame + 1) % _beamFrames.Length; _beam.sprite = _beamFrames[_beamFrame]; }

        switch (_state)
        {
            case SpiralState.Start:
                _spiralFrame++;
                if (_spiralFrame >= _spiralStart.Length)
                {
                    _spiralFrame = 0;
                    if (_stopRequested || _target == null) _state = SpiralState.End;
                    else { _state = SpiralState.Loop; StartBeam(); }
                }
                break;
            case SpiralState.Loop:
                _spiralFrame++;
                // a quarter turn is done: the end starts from here, so the join is seamless
                if (_spiralFrame >= _spiralLoop.Length)
                {
                    _spiralFrame = 0;
                    if (_stopRequested || _target == null)
                    {
                        _state = SpiralState.End;
                        if (_beamOn && !_beamFading) { _beamFading = true; _fadeT = 0f; }
                    }
                }
                break;
            case SpiralState.End:
                _spiralFrame++;
                if (_spiralFrame >= _spiralEnd.Length) { _state = SpiralState.Off; _spiral.enabled = false; return; }
                break;
            default:
                return;
        }
        ShowSpiral();
    }

    private void StartBeam()
    {
        _beamOn = true;
        _beamFading = false;
        _extendT = 0f;
        _beamFrame = 0;
        _beam.sprite = _beamFrames[0];
        _beam.color = Color.white;
        _beam.enabled = true;
    }

    private void ShowSpiral()
    {
        switch (_state)
        {
            case SpiralState.Start: _spiral.sprite = _spiralStart[_spiralFrame]; break;
            case SpiralState.Loop: _spiral.sprite = _spiralLoop[_spiralFrame]; break;
            case SpiralState.End: _spiral.sprite = _spiralEnd[_spiralFrame]; break;
        }
    }

    // the spiral's middle on the crystal's point, the beam from there to the middle of the ally's sprite (where it
    // last was, once it's gone)
    private void Place()
    {
        Vector3 from = _origin != null ? _origin() : transform.position;
        _spiral.transform.SetPositionAndRotation(from, Quaternion.identity);

        if (!_beamOn) return;
        if (_target != null) _targetPoint = _target.VisualCenter;
        Vector2 d = (Vector2)(_targetPoint - from);
        // fast out, soft landing
        float eased = 1f - (1f - _extendT) * (1f - _extendT);
        _beam.transform.SetPositionAndRotation(from, Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg));
        _beam.size = new Vector2(Mathf.Max(0.0001f, d.magnitude * eased), BeamHeight);
    }
}
