using UnityEngine;

// her drain's orb (Tommy, 2026-11-27: Resources/RhodiolaDrainOrb, the glowing in and out orb, 17 frames at 130 ms):
// it leaves the insect she hit and arcs to the crystal at the tip of her staff, following the tip wherever her
// animation takes it (a hop into her heal, the staff swung out sideways), and only lands there, never on her body.
// landing runs onArrive (her heal, her sap, her Rejuvenating Burgeon). if she dies or is uprooted first, it fades
// out where it is with nothing
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

    // the glitter is let go and stops, so what's out finishes fading instead of popping away with the orb
    private void ReleaseGlitter()
    {
        if (_glitter == null) return;
        foreach (ParticleSystem ps in _glitter)
        {
            if (ps == null) continue;
            ps.transform.SetParent(null, true);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(ps.gameObject, ps.main.startLifetime.constantMax + 0.1f);
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
            ReleaseGlitter();
            Destroy(gameObject);
        }
    }
}
