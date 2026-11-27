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

    // from: where it rises (the insect's middle). speed: units a second along the straight line to the tip.
    // the arc rises arcPerUnit for every unit of that line, at least minArc
    public static void Launch(Rhodiola owner, Vector3 from, float speed, float arcPerUnit, float minArc, System.Action onArrive)
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
        go.transform.localScale = Vector3.one * 0.5f;
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

    private static int FrameIndex(Sprite s)
    {
        int cut = s.name.LastIndexOf('_');
        return cut >= 0 && int.TryParse(s.name.Substring(cut + 1), out int i) ? i : 0;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        _renderer.sprite = _frames[Mathf.FloorToInt(_age / FrameSeconds) % _frames.Length];

        if (_owner == null || !_owner.IsAlive) { Destroy(gameObject); return; }

        // a curve from where it rose to the tip as it is this frame, bowed up in the middle: as the tip moves the
        // curve bends with it, and at the end the orb is exactly on the tip
        _t = Mathf.Min(1f, _t + Time.deltaTime / _duration);
        Vector3 end = _owner.StaffTipPosition;
        Vector3 control = (_start + end) * 0.5f + Vector3.up * _arcHeight;
        float u = 1f - _t;
        transform.position = u * u * _start + 2f * u * _t * control + _t * _t * end;
        // half size as it leaves the insect, full size halfway, half size again as it reaches her (Tommy)
        transform.localScale = Vector3.one * (0.5f + 0.5f * Mathf.Sin(Mathf.PI * _t));

        if (_t >= 1f)
        {
            _onArrive?.Invoke();
            Destroy(gameObject);
        }
    }
}
