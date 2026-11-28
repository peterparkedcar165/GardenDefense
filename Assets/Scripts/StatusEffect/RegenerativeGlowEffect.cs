using UnityEngine;
using UnityEngine.Rendering.Universal;

// heal-over-time buff applied by the Glowworm. inherits the regen tick from RegenerationEffect
// and also attaches the same yellowish-green light the Glowworm emits to the buffed insect.
// if the target is already a Glowworm, the light is skipped to avoid a double glow
public class RegenerativeGlowEffect : RegenerationEffect
{
    public static readonly Color GlowColor = new Color(0.6f, 1f, 0.1f);

    private readonly float _glowRadius;
    private readonly float _glowIntensity;
    private readonly float _regenPercent;
    private Light2D _light;

    public RegenerativeGlowEffect(Entity target, float duration, int level, Entity source,
                                   float healingPerSecond, float regenPercent,
                                   float glowRadius, float glowIntensity)
        : base(target, duration, level, source, healingPerSecond)
    {
        _regenPercent  = regenPercent;
        _glowRadius    = glowRadius;
        _glowIntensity = glowIntensity;
        elementalType  = ElementalType.Grass;
    }

    // a refresh from the same glowworm: the new instance takes the old one's light, so the old
    // one's OnExpire (which runs right after this) has nothing to destroy and the glow stays on
    public override void OnReapply(StatusEffect previous)
    {
        base.OnReapply(previous);
        if (previous is RegenerativeGlowEffect prev && prev._light != null)
        {
            _light = prev._light;
            prev._light = null;
        }
    }

    public override void OnApply()
    {
        base.OnApply();
        if (target is Glowworm) return;   // Glowworm already has its own permanent glow
        SpawnLight();
    }

    // the flat part is fixed when it lands, the max health part is read as each tick lands
    protected override void HealTick(float share) =>
        HealTarget((healingPerSecond + target.maxHealth * _regenPercent) * share);

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
        // handed over by the instance this one refreshed (OnReapply): still registered, keep it
        if (_light != null) return;

        // another glowworm's glow already on this insect: share its light (one glow per insect).
        // the light is only destroyed when the last instance using it ends (RemoveLight)
        foreach (StatusEffect e in target.activeEffects)
        {
            if (e != this && e is RegenerativeGlowEffect other && other._light != null)
            {
                _light = other._light;
                return;
            }
        }

        Transform visualRoot = target.transform.Find("Visual");
        if (visualRoot == null) visualRoot = target.transform;

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
        _light.enabled               = DarknessManager.instance != null && DarknessManager.instance.isDark;

        DarknessManager.RegisterLightSource(lightObj.transform, _glowRadius);
    }

    private void RemoveLight()
    {
        if (_light == null) return;
        // another glowworm's instance still shares this light: leave it shining for that one
        foreach (StatusEffect e in target.activeEffects)
        {
            if (e != this && e is RegenerativeGlowEffect other && other._light == _light)
            {
                _light = null;
                return;
            }
        }
        DarknessManager.UnregisterLightSource(_light.transform);
        Object.Destroy(_light.gameObject);
        _light = null;
    }

    public override string GetName() => "<color=green>Regenerative Glow</color>";
    public override string GetDescription()
    {
        float pctPerSec = _regenPercent * 100f;
        return $"Recovering <color=#AAFF44><b>{healingPerSecond:F0}</b></color> + <color=#AAFF44><b>{pctPerSec:F1}%</b></color> max health per second while emitting a healing glow.";
    }
}