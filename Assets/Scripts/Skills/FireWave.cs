using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

// Stargazer ultimate: a wall of fire that sweeps across the map in a direction,
// damaging each insect once as it passes
public class FireWave : MonoBehaviour
{
    private Vector2 direction;
    private Vector2 perp;
    private float speed, halfWidth, thickness, damage, travelDistance, burnMultiplier;
    private int flammableStacks;
    private Plant source;
    private Vector2 startPos;
    private readonly HashSet<Insect> _hit = new HashSet<Insect>();

    // a 2d light that travels with the wave so the fire wall illuminates dark biomes
    private LightFader _lightFader;
    private Light2D _light;
    private const float LightIntensity = 0.7f;
    private const float LightFadeTime = 0.3f;

    private static readonly DamageTag[] waveTags = { DamageTag.AoE, DamageTag.SkillDamage };

    public void Initialize(Vector2 startPos, Vector2 direction, float speed, float width,
                           float thickness, float damage, float burnMultiplier, int flammableStacks,
                           float travelDistance, Plant source)
    {
        this.startPos        = startPos;
        this.direction       = direction.normalized;
        this.perp            = new Vector2(-this.direction.y, this.direction.x);
        this.speed           = speed;
        this.halfWidth       = width * 0.5f;
        this.thickness       = thickness;
        this.damage          = damage;
        this.burnMultiplier  = burnMultiplier;
        this.flammableStacks = flammableStacks;
        this.travelDistance  = travelDistance;
        this.source          = source;

        transform.position = startPos;
        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(thickness, width, 1f);

        CreateLight(width, thickness);
    }

    // a warm point light that follows the wave. kept as a separate object (not a child) so the
    // wave's non uniform scale does not distort it. only lit in dark biomes, like Burn and Glowshroom
    private void CreateLight(float width, float thickness)
    {
        if (DarknessManager.instance == null) return;

        GameObject lightObj = new GameObject("FireWaveLight");
        lightObj.transform.position = transform.position;

        _light = lightObj.AddComponent<Light2D>();
        _light.lightType = Light2D.LightType.Point;
        // leave color at the Light2D default (white), matching the Calendula's light
        _light.falloffIntensity = 0.5f;
        float radius = (width * 0.5f + thickness * 0.5f + 0.5f) * 1.35f;
        _light.pointLightOuterRadius = radius;
        _light.pointLightInnerRadius = radius * 0.3f;
        _light.enabled = DarknessManager.instance.isDark;   // only shine while it is actually dark

        _lightFader = lightObj.AddComponent<LightFader>();
        _lightFader.Setup(_light, LightIntensity);
        _lightFader.FadeIn(0.05f);
        DarknessManager.RegisterLightSource(lightObj.transform, radius);
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

            Vector2 to = (Vector2)insect.transform.position - (Vector2)transform.position;
            float along = Mathf.Abs(Vector2.Dot(to, direction));   // distance along the sweep
            float side  = Mathf.Abs(Vector2.Dot(to, perp));        // distance across the wall
            if (along <= thickness * 0.5f && side <= halfWidth)
            {
                // amplified damage against already-burning targets
                float dmg = insect.HasEffect<BurnEffect>() ? damage * burnMultiplier : damage;
                insect.Damage(dmg, source.damageType, source.elementalType, source, true, waveTags);
                (source as Stargazer)?.AddFlammable(insect, flammableStacks);
                _hit.Add(insect);
            }

        }

        if (Vector2.Distance(startPos, transform.position) >= travelDistance)
            DestroyWave();
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
