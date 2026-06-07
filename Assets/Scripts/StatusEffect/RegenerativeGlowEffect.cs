using UnityEngine;
using UnityEngine.Rendering.Universal;

// heal-over-time buff applied by the Glowworm. inherits the regen tick from RegenerationEffect
// and also attaches the same yellowish-green light the Glowworm emits to the buffed insect
public class RegenerativeGlowEffect : RegenerationEffect
{
    public static readonly Color GlowColor = new Color(0.6f, 1f, 0.1f);

    private readonly float _glowRadius;
    private readonly float _glowIntensity;
    private Light2D _light;

    public RegenerativeGlowEffect(Entity target, float duration, int level, Entity source,
                                   float healingPerSecond, float glowRadius, float glowIntensity)
        : base(target, duration, level, source, healingPerSecond)
    {
        _glowRadius    = glowRadius;
        _glowIntensity = glowIntensity;
    }

    public override void OnApply()
    {
        base.OnApply();
        SpawnLight();
    }

    public override void OnExpire()
    {
        RemoveLight();
    }

    public override void OnTargetDied()
    {
        // target's gameObject is being destroyed; just unregister so DarknessManager doesn't hold a dead ref
        if (_light != null)
        {
            DarknessManager.UnregisterLightSource(_light.transform);
            _light = null;
        }
    }

    private void SpawnLight()
    {
        Transform visualRoot = target.transform.Find("Visual") ?? target.transform;

        // reuse if the effect is refreshed before the previous one expires
        Transform existing = visualRoot.Find("RegenerativeGlow");
        if (existing != null)
        {
            _light = existing.GetComponent<Light2D>();
            return;
        }

        GameObject lightObj = new GameObject("RegenerativeGlow");
        lightObj.transform.SetParent(visualRoot);
        lightObj.transform.localPosition = Vector3.zero;

        _light = lightObj.AddComponent<Light2D>();
        _light.lightType             = Light2D.LightType.Point;
        _light.color                 = GlowColor;
        _light.intensity             = _glowIntensity;
        _light.falloffIntensity      = 0.5f;
        _light.pointLightOuterRadius = _glowRadius;
        _light.pointLightInnerRadius = _glowRadius * 0.3f;

        DarknessManager.RegisterLightSource(lightObj.transform, _glowRadius);
    }

    private void RemoveLight()
    {
        if (_light == null) return;
        DarknessManager.UnregisterLightSource(_light.transform);
        Object.Destroy(_light.gameObject);
        _light = null;
    }

    public override string GetName() => "<color=#AAFF44>Regenerative Glow</color>";
    public override string GetDescription() => $"Recovering <color=#AAFF44><b>{healingPerSecond:F0}</b></color> health per second while emitting a healing glow.";
}
