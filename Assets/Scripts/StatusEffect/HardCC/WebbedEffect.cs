using UnityEngine;

// inherits StunEffect (a HardCrowdControl), so it prevents the target from acting.
// applied to plants by the Cave Spider on hit, and shows a web sprite while active
public class WebbedEffect : StunEffect
{
    private static GameObject _webPrefab;
    private GameObject _webVisual;

    public WebbedEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source) { }

    public override string GetName() => "<color=#DDDDDD>Webbed</color>";
    public override string GetDescription() => "Trapped in sticky webbing, preventing any action.";

    public override void OnApply()
    {
        if (_webPrefab == null)
            _webPrefab = Resources.Load<GameObject>("WebbedVisual");
        if (_webPrefab == null || target == null) return;

        _webVisual = Object.Instantiate(_webPrefab, target.transform);
        _webVisual.transform.localPosition = Vector3.zero;
    }

    public override void OnExpire()
    {
        if (_webVisual != null) Object.Destroy(_webVisual);
    }
}
