using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sunray : MonoBehaviour
{
    private float damagePerSecond;
    private float aoeRadius;
    private float duration;
    private Plant source;
    private const float tickInterval = 0.1f;
    private const float oscillationSpeed = 40f;
    private const float oscillationAngle = 40f;
    private const float shrinkDuration = 0.2f;

    private Vector3 targetScale;

    private static readonly DamageTag[] damageTags = new DamageTag[] { DamageTag.SkillDamage, DamageTag.AoE, DamageTag.DoT };

    private void Awake()
    {
        targetScale = transform.localScale;
    }

    // visualScaleMultiplier is aoeRadius relative to the plant's base (level 0) skill radius, so
    // the beam's rendered width grows proportionally with the actual hit radius as Path3 levels
    // up, rather than always rendering at its authored default size regardless of level
    public void Initialize(float damagePerSecond, float aoeRadius, float duration, Plant source, float visualScaleMultiplier = 1f)
    {
        this.damagePerSecond = damagePerSecond;
        this.aoeRadius = aoeRadius;
        this.duration = duration;
        this.source = source;

        targetScale *= visualScaleMultiplier;
        transform.localScale = targetScale;

        if (DarknessManager.instance != null)
        {
            var light = gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.color = Color.white;
            light.intensity = 0f;
            light.falloffIntensity = 0.5f;
            light.pointLightOuterRadius = 5f;
            light.pointLightInnerRadius = 5f * 0.3f;

            var fader = gameObject.AddComponent<LightFader>();
            fader.Setup(light, 1f);
            fader.FadeIn(0.3f);

            DarknessManager.RegisterLightSource(transform, 5f);
        }
        StartCoroutine(SunrayRoutine());
    }

    private IEnumerator SunrayRoutine()
    {
        float elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < duration && source != null && source.IsAlive)
        {
            elapsed += Time.deltaTime;
            float rotY = Mathf.Sin(elapsed * oscillationSpeed) * oscillationAngle;
            transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            if (elapsed >= nextTick)
            {
                List<Insect> snapshot = new List<Insect>(Insect.allInsects);
                foreach (Insect insect in snapshot)
                {
                    if (insect == null || !insect.IsAlive) continue;
                    if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius)
                        insect.Damage(damagePerSecond * tickInterval, source.damageType, source.elementalType, source, true, damageTags);
                }
                nextTick += tickInterval;
            }
            yield return null;
        }

        // shrink X to 0 and fade light out together
        var fader = GetComponent<LightFader>();
        if (fader != null) fader.FadeOut(shrinkDuration);

        float t = 0f;
        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            float scaleX = Mathf.Lerp(targetScale.x, 0f, t / shrinkDuration);
            transform.localScale = new Vector3(scaleX, targetScale.y, targetScale.z);
            yield return null;
        }

        DarknessManager.UnregisterLightSource(transform);
        Destroy(gameObject);
    }
}
