using UnityEngine;

// a mote of life energy for Rhodiola (Tommy, 2026-11-27; it used to be her healing seed): it flies to
// an entity and, if that one is still alive when it gets there, calls back. her drain sends motes from
// the insect she hit back to herself, and her channel sends them from her to the plant she heals. no
// collider: it only ever reaches the one it was sent to. if that one dies on the way it flies on to
// where it was and fades there without calling back
//
// the root is the mote's base, on the ground line it travels along; its Visual (and the fly particles,
// moved under it) is drawn launchHeight above where it starts and comes down evenly to the base as it
// closes in. the Shadow stays on the base
public class RhodiolaProjectile : MonoBehaviour
{
    private Entity _target;
    private float _speed;
    private System.Action<Entity> _onArrive;
    private Vector3 _lastKnownTargetPosition;

    private Transform _visual;
    private Vector3 _visualRestLocal;
    private float _launchHeight;
    private float _launchDistance;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        _visual = transform.Find("Visual");
        if (_visual == null) return;
        _visualRestLocal = _visual.localPosition;
        // the fly particles ride with the drawn mote, not its base
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
            if (ps.transform.parent == transform) ps.transform.SetParent(_visual, true);
    }

    public void Initialize(Entity target, float speed, System.Action<Entity> onArrive)
    {
        _target = target;
        _speed = speed;
        _onArrive = onArrive;
        _lastKnownTargetPosition = target != null ? target.transform.position : transform.position;
    }

    // the drawn mote starts at this world height, its base staying where it was spawned
    public void SetLaunchHeight(float worldY)
    {
        if (_visual == null) return;
        _launchHeight = worldY - transform.TransformPoint(_visualRestLocal).y;
        _launchDistance = Vector3.Distance(transform.position, _lastKnownTargetPosition);
        PlaceVisual();
    }

    private void PlaceVisual()
    {
        if (_visual == null || _launchHeight == 0f) return;
        float share = _launchDistance > 0.0001f
            ? Mathf.Clamp01(Vector3.Distance(transform.position, _lastKnownTargetPosition) / _launchDistance) : 0f;
        _visual.position = transform.TransformPoint(_visualRestLocal) + Vector3.up * (_launchHeight * share);
    }

    void Update()
    {
        if (_target != null && _target.health > 0)
            _lastKnownTargetPosition = _target.transform.position;

        transform.position = Vector3.MoveTowards(transform.position, _lastKnownTargetPosition, _speed * Time.deltaTime);
        PlaceVisual();

        if (Vector3.Distance(transform.position, _lastKnownTargetPosition) < 0.15f)
            Arrive();
    }

    private void Arrive()
    {
        if (_target != null && _target.health > 0) _onArrive?.Invoke(_target);
        DetachParticles();
        Destroy(gameObject);
    }

    // the fly and trail particles are let go right before the mote goes, so they finish fading out
    // instead of popping out of existence with it (as Projectile.OnBeforeDestroy does for the others)
    private void DetachParticles()
    {
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>())
        {
            Transform t = ps.transform;
            Vector3 worldPos = t.position;
            Quaternion worldRot = t.rotation;

            t.SetParent(null, false);
            t.position = worldPos;
            t.rotation = worldRot;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(ps.gameObject, ps.main.startLifetime.constantMax);
        }
    }
}
