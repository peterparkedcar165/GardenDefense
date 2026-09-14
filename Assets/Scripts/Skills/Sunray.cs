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

    private SoundEffect loopSound, endSound;
    private float endLeadTime;
    private AudioSource activeLoop;

    private static readonly DamageTag[] damageTags = new DamageTag[] { DamageTag.SkillDamage, DamageTag.AoE, DamageTag.DoT };

    // skill tree nodes 3.1, 6.1, 6.2
    private const float HomingSpeed = 0.6f;
    private const float KillRefreshDuration = 0.4f;
    private bool _ambientSunProc;
    private bool _homing;
    private bool _refreshOnKill;
    private Insect _trackedInsect;

    private void Awake()
    {
        targetScale = transform.localScale;
    }

    // visualScaleMultiplier is aoeRadius relative to the plant's base (level 0) skill radius, so
    // the beam's rendered width grows proportionally with the actual hit radius as Path3 levels
    // up, rather than always rendering at its authored default size regardless of level
    // the spawn sound itself is played by the caller shortly before this object is even
    // instantiated (see Sunflower.SpawnSunray) - this only owns the sunray's own lifetime, so it
    // just picks up the loop/end sounds
    public void Initialize(float damagePerSecond, float aoeRadius, float duration, Plant source, float visualScaleMultiplier = 1f,
        SoundEffect loopSound = null, SoundEffect endSound = null, float endLeadTime = 0.2f)
    {
        this.damagePerSecond = damagePerSecond;
        this.aoeRadius = aoeRadius;
        this.duration = duration;
        this.source = source;
        this.loopSound = loopSound;
        this.endSound = endSound;
        this.endLeadTime = endLeadTime;

        targetScale *= visualScaleMultiplier;
        transform.localScale = targetScale;

        if (source is Sunflower sunflower)
        {
            _ambientSunProc = SkillTreeManager.HasUnlock(sunflower, Sunflower.AmbientSunProcUnlock);
            _refreshOnKill  = SkillTreeManager.HasUnlock(sunflower, Sunflower.SunrayRefreshUnlock);
            _homing         = SkillTreeManager.HasUnlock(sunflower, Sunflower.HomingSunrayUnlock);
            if (_homing) _trackedInsect = FindNearestInsect();
        }

        activeLoop = SfxPlayer.PlayLooping(loopSound, transform);

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
        bool endSoundPlayed = false;

        while (elapsed < duration && source != null && source.IsAlive)
        {
            elapsed += Time.deltaTime;
            float rotY = Mathf.Sin(elapsed * oscillationSpeed) * oscillationAngle;
            transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            if (_homing)
            {
                if (_trackedInsect == null || !_trackedInsect.IsAlive)
                    _trackedInsect = FindNearestInsect();
                if (_trackedInsect != null)
                {
                    Vector3 toTarget = _trackedInsect.transform.position - transform.position;
                    float dist = toTarget.magnitude;
                    if (dist > 0.01f)
                        transform.position += toTarget.normalized * Mathf.Min(HomingSpeed * Time.deltaTime, dist);
                }
            }

            if (elapsed >= nextTick)
            {
                List<Insect> snapshot = new List<Insect>(Insect.allInsects);
                foreach (Insect insect in snapshot)
                {
                    if (insect == null || !insect.IsAlive) continue;
                    if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius)
                    {
                        insect.Damage(damagePerSecond * tickInterval, source.damageType, source.elementalType, source, true, damageTags);
                        if (_ambientSunProc && source is Sunflower ambientSunflower)
                            ambientSunflower.TryReduceSunTimerSmall();
                        if (_refreshOnKill && !insect.IsAlive)
                            duration += KillRefreshDuration;
                    }
                }
                nextTick += tickInterval;
            }

            if (!endSoundPlayed && elapsed >= duration - endLeadTime)
            {
                endSoundPlayed = true;
                SfxPlayer.StopLooping(activeLoop);
                SfxPlayer.Play(endSound, transform.position);
            }

            yield return null;
        }

        // covers an early exit (source died before reaching the lead window above) - the loop and
        // end sound still need to resolve exactly once regardless of how the sunray ended
        if (!endSoundPlayed)
        {
            SfxPlayer.StopLooping(activeLoop);
            SfxPlayer.Play(endSound, transform.position);
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

    private Insect FindNearestInsect()
    {
        Insect nearest = null;
        float nearestDist = float.MaxValue;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly) continue;
            float dist = Vector3.Distance(transform.position, insect.transform.position);
            if (dist < nearestDist) { nearestDist = dist; nearest = insect; }
        }
        return nearest;
    }
}
