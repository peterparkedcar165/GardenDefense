using UnityEngine;

// her drain's orb (Tommy, 2026-11-27: Resources/RhodiolaDrainOrb, the glowing in and out orb, 17 frames at 130 ms):
// it leaves the insect she hit and arcs to the crystal at the tip of her staff, following the tip wherever her
// animation takes it (a hop into her heal, the staff swung out sideways), and only lands there, never on her body.
// landing runs onArrive (her heal, her sap, her Rejuvenating Burgeon) and pops the orb into a ring of sparkles
// flying outward (Burst); the same ring bursts from the insect as the orb rises off it. if she dies or is uprooted
// first, it fades out where it is with nothing
public class RhodiolaDrainOrb : MonoBehaviour
{
    private const float FrameSeconds = 0.13f;
    private static Sprite[] _frames;

    private Rhodiola _owner;
    private System.Action _onArrive;
    private SpriteRenderer _renderer;
    private Vector3 _start;
    private float _duration;
    private float _arcHeight;
    private float _t;
    private float _age;

    // its size along the way: 75% of the orb's art (Tommy), times half at both ends and full in the middle
    private const float SizeScale = 0.75f;

    // the trailing glitter: the old seed's particle systems (trailSource, Rhodiola's life mote prefab, with
    // Tommy's tuned fly and trail particles), riding with the orb and left to fade where it lands
    private ParticleSystem[] _glitter;

    // from: where it rises (the insect's middle). speed: units a second along the straight line to the tip.
    // the arc rises arcPerUnit for every unit of that line, at least minArc
    public static void Launch(Rhodiola owner, Vector3 from, float speed, float arcPerUnit, float minArc, GameObject trailSource, System.Action onArrive)
    {
        if (_frames == null)
        {
            _frames = Resources.LoadAll<Sprite>("RhodiolaDrainOrb");
            System.Array.Sort(_frames, (a, b) => FrameIndex(a).CompareTo(FrameIndex(b)));
        }
        if (owner == null || _frames.Length == 0) { onArrive?.Invoke(); return; }

        GameObject go = new GameObject("RhodiolaDrainOrb");
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        RhodiolaDrainOrb orb = go.AddComponent<RhodiolaDrainOrb>();
        orb._owner = owner;
        orb._onArrive = onArrive;
        orb._start = from;
        go.transform.position = from;
        go.transform.localScale = Vector3.one * (0.5f * SizeScale);
        orb.TakeGlitter(trailSource);
        // her hit on the insect (Tommy, 2026-11-27): the same ring of sparkles as the landing on her staff bursts out
        // where the orb rises. world space, so they stay on the insect's spot as the orb flies off
        orb.Burst();
        float distance = Vector2.Distance(from, owner.StaffTipPosition);
        orb._duration = Mathf.Max(0.15f, distance / Mathf.Max(0.01f, speed));
        orb._arcHeight = Mathf.Max(minArc, distance * arcPerUnit);

        orb._renderer = go.AddComponent<SpriteRenderer>();
        orb._renderer.sprite = _frames[0];
        SpriteRenderer body = owner.BodyRenderer;
        if (body != null)
        {
            orb._renderer.sharedMaterial = body.sharedMaterial;
            orb._renderer.sortingLayerID = body.sortingLayerID;
            orb._renderer.sortingOrder = body.sortingOrder + 5;
        }
    }

    // copies the trail source and keeps only its top particle systems, moved onto the orb (their own scaling is
    // Local, so the orb's size doesn't change them); the rest of the copy goes
    private void TakeGlitter(GameObject trailSource)
    {
        if (trailSource == null) return;
        GameObject donor = Instantiate(trailSource, transform.position, Quaternion.identity);
        foreach (MonoBehaviour script in donor.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
        var taken = new System.Collections.Generic.List<ParticleSystem>();
        foreach (ParticleSystem ps in donor.GetComponentsInChildren<ParticleSystem>(true))
        {
            Transform parent = ps.transform.parent;
            if (parent != null && parent.GetComponentInParent<ParticleSystem>() != null) continue;
            ps.transform.SetParent(transform, false);
            ps.transform.localPosition = Vector3.zero;
            taken.Add(ps);
        }
        Destroy(donor);
        _glitter = taken.ToArray();
        foreach (ParticleSystem ps in _glitter) ps.Play(true);
    }

    // the glitter is let go and stops making more: every sparkle already out stays in the world and lives out
    // its own life (Tommy), and unity removes the empty system after the last one (stop action Destroy)
    private void ReleaseGlitter()
    {
        if (_glitter == null) return;
        foreach (ParticleSystem ps in _glitter)
        {
            if (ps == null) continue;
            // not SetParent(null, true): keeping the world size would rewrite the system's localScale to the orb's
            // (half size at the tip), and these systems scale Local, drawing their sparkles by that localScale, so
            // every sparkle snapped smaller as the orb landed (Tommy). as Projectile.OnBeforeDestroy does
            Transform t = ps.transform;
            Vector3 worldPos = t.position;
            Quaternion worldRot = t.rotation;
            t.SetParent(null, false);
            t.position = worldPos;
            t.rotation = worldRot;
            ParticleSystem.MainModule main = ps.main;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        _glitter = null;
    }

    private static int FrameIndex(Sprite s)
    {
        int cut = s.name.LastIndexOf('_');
        return cut >= 0 && int.TryParse(s.name.Substring(cut + 1), out int i) ? i : 0;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        _renderer.sprite = _frames[Mathf.FloorToInt(_age / FrameSeconds) % _frames.Length];

        if (_owner == null || !_owner.IsAlive) { ReleaseGlitter(); Destroy(gameObject); return; }

        // a curve from where it rose to the tip as it is this frame, bowed up in the middle: as the tip moves the
        // curve bends with it, and at the end the orb is exactly on the tip
        _t = Mathf.Min(1f, _t + Time.deltaTime / _duration);
        Vector3 end = _owner.StaffTipPosition;
        Vector3 control = (_start + end) * 0.5f + Vector3.up * _arcHeight;
        float u = 1f - _t;
        transform.position = u * u * _start + 2f * u * _t * control + _t * _t * end;
        // half size as it leaves the insect, full size halfway, half size again as it reaches her (Tommy), all at 75%
        transform.localScale = Vector3.one * ((0.5f + 0.5f * Mathf.Sin(Mathf.PI * _t)) * SizeScale);

        if (_t >= 1f)
        {
            _onArrive?.Invoke();
            Burst();
            ReleaseGlitter();
            Destroy(gameObject);
        }
    }

    // landing on the tip (Tommy, 2026-11-27): the orb pops into a ring of its own sparkles flying outward, slowing
    // as they spread. they go into its world space trail system, so they stay where it landed and fade with the
    // rest of its glitter. numbers are proposals
    private const int BurstCount = 14;
    private const float BurstSpeedMin = 0.9f, BurstSpeedMax = 1.5f;   // units a second
    private const float BurstLifeMin = 0.45f, BurstLifeMax = 0.7f;    // seconds
    private const float BurstDrag = 3f;                               // how fast they slow down
    private const float BurstSizeMin = 1f, BurstSizeMax = 1.35f;      // times the glitter's own start size

    private void Burst()
    {
        if (_glitter == null) return;
        ParticleSystem ps = null;
        foreach (ParticleSystem p in _glitter)
        {
            if (p == null) continue;
            bool pWorld = p.main.simulationSpace == ParticleSystemSimulationSpace.World;
            if (ps == null || pWorld && ps.main.simulationSpace != ParticleSystemSimulationSpace.World) ps = p;
        }
        if (ps == null) return;

        ParticleSystem.MainModule main = ps.main;
        bool world = main.simulationSpace == ParticleSystemSimulationSpace.World;
        // the glitter has no drag of its own; the trail's sparkles barely move, so this only shows on the burst
        ParticleSystem.LimitVelocityOverLifetimeModule slow = ps.limitVelocityOverLifetime;
        slow.enabled = true;
        slow.limit = 1000f;
        slow.drag = BurstDrag;
        slow.multiplyDragByParticleSize = false;
        slow.multiplyDragByParticleVelocity = false;

        float size = main.startSize.constantMax;
        Vector3 at = world ? transform.position : ps.transform.InverseTransformPoint(transform.position);
        var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
        float turn = Random.value * 360f;
        for (int i = 0; i < BurstCount; i++)
        {
            float a = (turn + (i + Random.Range(-0.3f, 0.3f)) * 360f / BurstCount) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            if (!world) dir = ps.transform.InverseTransformDirection(dir);
            emit.position = at;
            emit.velocity = dir * Random.Range(BurstSpeedMin, BurstSpeedMax);
            emit.startLifetime = Random.Range(BurstLifeMin, BurstLifeMax);
            emit.startSize = size * Random.Range(BurstSizeMin, BurstSizeMax);
            ps.Emit(emit, 1);
        }
    }
}
