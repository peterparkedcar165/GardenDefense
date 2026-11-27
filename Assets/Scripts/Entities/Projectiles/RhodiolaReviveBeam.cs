using UnityEngine;

// the light that comes down on a plant Rhodiola's skill revives (Tommy, 2026-11-27: Resources/RhodiolaReviveBeam,
// his beam, 14 frames of 64x64 at 65 ms, played once, drawn at 2.5x). its pattern repeats upward, so it's tiled from
// the bottom of the plant's tile to past the top of the camera, as if it came down from the sky. drawn over plants,
// insects and tree tops (below the weather), unlit so it glows at night, on real time (game speed doesn't change
// it) but frozen while paused
public class RhodiolaReviveBeam : MonoBehaviour
{
    private const float FrameSeconds = 0.065f;
    private const float Scale = 2.5f;
    private const string SortingLayer = "Non-Colliders_Air";
    private const int SortingOrder = 100;

    private static Sprite[] _frames;
    private static Material _unlit;

    private SpriteRenderer _renderer;
    private float _age;

    // tileCenter: the middle of the revived plant's tile
    public static void Play(Vector3 tileCenter)
    {
        if (_frames == null)
        {
            _frames = Resources.LoadAll<Sprite>("RhodiolaReviveBeam");
            System.Array.Sort(_frames, (a, b) => FrameIndex(a).CompareTo(FrameIndex(b)));
        }
        if (_frames.Length == 0) return;
        if (_unlit == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null) _unlit = new Material(shader);
        }

        // from the bottom of the tile up to a tile past the top of the camera
        float bottom = tileCenter.y - 0.5f;
        Camera cam = Camera.main;
        float top = cam != null ? cam.transform.position.y + cam.orthographicSize + 1f : bottom + 20f;
        float height = Mathf.Max(1f, top - bottom);

        GameObject go = new GameObject("RhodiolaReviveBeam");
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        go.transform.position = new Vector3(tileCenter.x, bottom, tileCenter.z);
        go.transform.localScale = Vector3.one * Scale;
        RhodiolaReviveBeam beam = go.AddComponent<RhodiolaReviveBeam>();
        beam._renderer = go.AddComponent<SpriteRenderer>();
        beam._renderer.sprite = _frames[0];
        if (_unlit != null) beam._renderer.sharedMaterial = _unlit;
        beam._renderer.sortingLayerName = SortingLayer;
        beam._renderer.sortingOrder = SortingOrder;
        // the frame repeats upward: one frame is a unit across before the 2.5x scale
        beam._renderer.drawMode = SpriteDrawMode.Tiled;
        beam._renderer.tileMode = SpriteTileMode.Continuous;
        beam._renderer.size = new Vector2(_frames[0].bounds.size.x, height / Scale);

        // its glitter, along the part of it the camera shows
        float shownTop = cam != null ? Mathf.Min(top, cam.transform.position.y + cam.orthographicSize) : top;
        ReviveGlitter.Begin(new Vector3(tileCenter.x, bottom, tileCenter.z), Mathf.Max(0.5f, shownTop - bottom),
            _frames.Length * FrameSeconds * GlitterShare);
    }

    // the glitter comes out for this share of the beam's time, while it's full (it thins out after)
    private const float GlitterShare = 0.7f;

    private static int FrameIndex(Sprite s)
    {
        int cut = s.name.LastIndexOf('_');
        return cut >= 0 && int.TryParse(s.name.Substring(cut + 1), out int i) ? i : 0;
    }

    private void Update()
    {
        // real time, so a game speed up doesn't hurry it, but frozen while the game is paused (Tommy)
        _age += Time.timeScale > 0f ? Time.unscaledDeltaTime : 0f;
        int frame = Mathf.FloorToInt(_age / FrameSeconds);
        if (frame >= _frames.Length) { Destroy(gameObject); return; }
        _renderer.sprite = _frames[frame];
    }
}

// the revive beam's glitter (Tommy, 2026-11-27): sparkles born on the beam's middle line, anywhere up the part of it
// the camera shows, that move out sideways (left or right), ease off and drift a little upward, twinkling in and out
// like a mulch drop's glints (MulchRegistry's glitter material, white). on the beam's clock: real time, so game
// speed doesn't hurry it, frozen while paused. it's its own object, so its sparkles finish after the beam is gone
public class ReviveGlitter : MonoBehaviour
{
    // sparkles a second for every unit of beam shown
    private const float RatePerUnit = 14f;
    private static readonly Color GlitterColor = new Color(0.92f, 1f, 0.86f, 1f);

    private ParticleSystem _ps;
    private float _height;
    private float _emitSeconds;
    private float _age;
    private float _budget;
    private bool _paused;

    public static void Begin(Vector3 bottom, float height, float emitSeconds)
    {
        GameObject go = new GameObject("ReviveGlitter");
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        go.transform.position = bottom;
        ReviveGlitter glitter = go.AddComponent<ReviveGlitter>();
        glitter._height = height;
        glitter._emitSeconds = emitSeconds;
        glitter.Build();
    }

    private void Build()
    {
        _ps = gameObject.AddComponent<ParticleSystem>();
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = _ps.main;
        // emitted by hand (Update), so it just keeps running until its last sparkle is gone
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
        main.startColor = GlitterColor;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;
        main.useUnscaledTime = true;

        // emitted by hand below, each with its own way out
        ParticleSystem.EmissionModule emission = _ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = _ps.shape;
        shape.enabled = false;

        // they ease off sideways and are pushed gently upward
        ParticleSystem.LimitVelocityOverLifetimeModule limit = _ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.drag = 1.6f;
        ParticleSystem.ForceOverLifetimeModule force = _ps.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.World;
        force.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        force.y = new ParticleSystem.MinMaxCurve(0.45f, 0.45f);
        force.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // each glint pops in, holds a moment and shrinks away: a twinkle
        ParticleSystem.SizeOverLifetimeModule size = _ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(0.6f, 0.8f), new Keyframe(1f, 0f)));
        ParticleSystem.ColorOverLifetimeModule color = _ps.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
        MulchRegistry registry = Resources.Load<MulchRegistry>("MulchRegistry");
        if (registry != null && registry.glitterMaterial != null) renderer.sharedMaterial = registry.glitterMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Non-Colliders_Air";
        renderer.sortingOrder = 101;

        _ps.Play();
    }

    private void Update()
    {
        // frozen while the game is paused, real time otherwise
        bool paused = Time.timeScale <= 0f;
        if (paused != _paused)
        {
            _paused = paused;
            if (paused) _ps.Pause(); else _ps.Play();
        }
        if (paused) return;

        float dt = Time.unscaledDeltaTime;
        _age += dt;
        if (_age < _emitSeconds)
        {
            _budget += RatePerUnit * _height * dt;
            ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
            while (_budget >= 1f)
            {
                _budget -= 1f;
                float side = Random.value < 0.5f ? -1f : 1f;
                p.position = transform.position + new Vector3(0f, Random.Range(0f, _height), 0f);
                p.velocity = new Vector3(side * Random.Range(0.35f, 1.1f), Random.Range(0.02f, 0.12f), 0f);
                _ps.Emit(p, 1);
            }
        }
        else if (_ps.particleCount == 0)
        {
            Destroy(gameObject);
        }
    }
}
