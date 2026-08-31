using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FungalGlowEffect : StatusEffect
{
    private readonly float originalDuration;
    private LightFader _fader;
    private const float LightRadius = 1.2f;

    public FungalGlowEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Grass;
        this.originalDuration = duration;
    }

    public override void OnApply()
    {
        // only emit light in dark biomes (mirrors BurnEffect) , never in lit levels
        if (DarknessManager.instance != null)
        {
            _fader = GetOrCreateFader();
            _fader.FadeIn(0.3f);

            // register as a light source so the lit insect (and any insect in its glow)
            // is treated as illuminated in pitch black cave levels
            DarknessManager.UnregisterLightSource(_fader.transform);
            DarknessManager.RegisterLightSource(_fader.transform, LightRadius);
        }
    }

    private LightFader GetOrCreateFader()
    {
        Transform visualRoot = target.transform.Find("Visual") ?? target.transform;

        Transform existing = visualRoot.Find("FungalGlowLight");
        if (existing != null)
        {
            LightFader fader = existing.GetComponent<LightFader>();
            if (fader != null) return fader;
        }

        GameObject lightObj = new GameObject("FungalGlowLight");
        lightObj.transform.SetParent(visualRoot);
        lightObj.transform.localPosition = Vector3.zero;

        Light2D light                = lightObj.AddComponent<Light2D>();
        light.lightType              = Light2D.LightType.Point;
        light.color                  = new Color(0.05f, 0.65f, 1f);
        light.falloffIntensity       = 0.5f;
        light.pointLightOuterRadius  = 1.2f;
        light.pointLightInnerRadius  = 0.24f;

        LightFader newFader = lightObj.AddComponent<LightFader>();
        newFader.Setup(light, 1f);
        return newFader;
    }

    public override void OnExpire()
    {
        if (_fader == null) return;
        DarknessManager.UnregisterLightSource(_fader.transform);
        _fader.FadeOut(0.5f);
    }

    public override void OnTargetDied()
    {
        if (source is Glowshroom gm) gm.OnFungalGlowInsectDied(target.transform.position);

        if (_fader == null) return;
        DarknessManager.UnregisterLightSource(_fader.transform);
        _fader.transform.SetParent(null);
        _fader.FadeOut(1f, destroyOnComplete: true);
        _fader = null;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnDamageReceived(ElementalType elementalType, Entity damageSource, DamageTag[] damageTags = null)
    {
        if (elementalType != ElementalType.Water) return;
        duration = originalDuration;
    }

    public override string GetName() => "<color=green>Fungal Glow</color>";
    public override string GetDescription() =>
        "Emitting a faint fungal light. <color=#4FC3F7>Water</color> damage refreshes the duration.";
}
