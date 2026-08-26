using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

// Stargazer ultimate: a ball of fire that sweeps across the map in a direction,
// damaging each insect once as it passes
public class FireWave : MonoBehaviour
{
    private Vector2 direction;
    private float speed, radius, damage, travelDistance, burnMultiplier;
    private int flammableStacks;
    private Plant source;
    private Vector2 startPos;
    private readonly HashSet<Insect> _hit = new HashSet<Insect>();
    private bool _returnsAfterEnd;
    private bool _isReturning;

    // a 2d light that travels with the wave so the fire wall illuminates dark biomes
    private LightFader _lightFader;
    private Light2D _light;
    private const float LightIntensity = 0.7f;
    private const float LightFadeTime = 0.3f;
    private const float LightRadiusPadding = 0.5f; // flat padding beyond the wave's own radius
    private const float LightRadiusScale   = 1.5f; // then scaled up, same as the old rectangle formula

    private static readonly DamageTag[] waveTags = { DamageTag.AoE, DamageTag.SkillDamage };

    public void Initialize(Vector2 startPos, Vector2 direction, float speed, float radius,
                           float damage, float burnMultiplier, int flammableStacks,
                           float travelDistance, Plant source, bool returnsAfterEnd = false)
    {
        this.startPos        = startPos;
        this.direction       = direction.normalized;
        this.speed           = speed;
        this.radius          = radius;
        this.damage          = damage;
        this.burnMultiplier  = burnMultiplier;
        this.flammableStacks = flammableStacks;
        this.travelDistance  = travelDistance;
        this.source          = source;
        _returnsAfterEnd     = returnsAfterEnd;

        transform.position = startPos;
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        CreateLight(radius);
    }

    // a warm point light that follows the wave. kept as a separate object (not a child) so the
    // wave's scale does not distort it. only lit in dark biomes, like Burn and Glowshroom
    private void CreateLight(float radius)
    {
        if (DarknessManager.instance == null) return;

        GameObject lightObj = new GameObject("FireWaveLight");
        lightObj.transform.position = transform.position;

        _light = lightObj.AddComponent<Light2D>();
        _light.lightType = Light2D.LightType.Point;
        // leave color at the Light2D default (white), matching the Calendula's light
        _light.falloffIntensity = 0.5f;
        float lightRadius = (radius + LightRadiusPadding) * LightRadiusScale;
        _light.pointLightOuterRadius = lightRadius;
        _light.pointLightInnerRadius = lightRadius * 0.3f;
        _light.enabled = DarknessManager.instance.isDark;   // only shine while it is actually dark

        _lightFader = lightObj.AddComponent<LightFader>();
        _lightFader.Setup(_light, LightIntensity);
        _lightFader.FadeIn(0.05f);
        DarknessManager.RegisterLightSource(lightObj.transform, lightRadius);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (_lightFader != null)
        {
            _lightFader.transform.position = transform.position;
            // only emit light while it is actually dark, matching the other light sources
            if (_light != null) _light.enabled = DarknessManager.instance != null && DarknessManager.instance.isDark;
        }

        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || _hit.Contains(insect)) continue;

            if (Vector2.Distance(insect.transform.position, transform.position) <= radius)
            {
                // amplified damage against already-burning targets
                float dmg = insect.HasEffect<BurnEffect>() ? damage * burnMultiplier : damage;
                insect.Damage(dmg, source.damageType, source.elementalType, source, true, waveTags);
                (source as Stargazer)?.AddFlammable(insect, flammableStacks);
                (source as Stargazer)?.ApplySkillBurn(insect);
                _hit.Add(insect);
            }

        }

        if (Vector2.Distance(startPos, transform.position) >= travelDistance)
        {
            if (_returnsAfterEnd && !_isReturning)
            {
                _isReturning     = true;
                _returnsAfterEnd = false;
                direction        = -direction;
                startPos         = (Vector2)transform.position;
                _hit.Clear();
            }
            else
            {
                DestroyWave();
            }
        }
    }

    private void DestroyWave()
    {
        if (_lightFader != null)
        {
            DarknessManager.UnregisterLightSource(_lightFader.transform);
            _lightFader.FadeOut(LightFadeTime, destroyOnComplete: true);
            _lightFader = null;
        }
        Destroy(gameObject);
    }

    // safety: if the wave is destroyed without going through DestroyWave, do not orphan the light
    private void OnDestroy()
    {
        if (_lightFader != null)
        {
            DarknessManager.UnregisterLightSource(_lightFader.transform);
            Destroy(_lightFader.gameObject);
            _lightFader = null;
        }
    }
}
