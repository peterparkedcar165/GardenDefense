using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sunray : MonoBehaviour
{
    private float damagePerSecond;
    private float aoeRadius;
    private float duration;
    private Plant source;
    private const float tickInterval = 0.25f;
    private const float shrinkDuration = 0.2f;

    private Vector3 targetScale;

    private ParticleSystem sunParticles;
    private float baseShapeRadius;

    private SoundEffect endSound;
    private float endLeadTime;
    private AudioSource activeLoop;

    private static readonly DamageTag[] damageTags = new DamageTag[] { DamageTag.SkillDamage, DamageTag.AoE, DamageTag.DoT };


    // skill tree nodes 3.1, 6.1, 6.2
    private const float HomingSpeed = 0.6f;
    private const float KillRefreshDuration = 3f;
    private bool _ambientSunProc;
    private bool _homing;
    private bool _refreshOnKill;
    // Heatstroke (skill tree): insects inside the beam are slowed, put on every tick and lasting this long, a little
    // more than a tick, so it holds while they're inside and goes soon after they leave
    private bool _heatstroke;
    private const float HeatstrokeLinger = 0.35f;
    private Insect _trackedInsect;
    // insects hit this tick, cleared and refilled every tick instead of a new list
    private readonly List<Insect> _inRange = new List<Insect>();

    private void Awake()
    {
        targetScale = transform.localScale;
        sunParticles = GetComponentInChildren<ParticleSystem>();
        if (sunParticles != null) baseShapeRadius = sunParticles.shape.radius;
        SetUpBeam();
    }

    // Tommy's animated beam (2026-11-26, Downloads/gd_sunray_beam: Resources/SunrayBeam, 32 frames of 64 px,
    // the light flowing down, a seamless loop, played at 144 fps). the Visual used to stretch one 16 px sprite 2 by
    // 40 times; now it undoes the root's stretch (so the pixels stay square) and tiles the frame up the
    // beam's whole height instead. the frames are imported at 20 px a tile, so their 20 px of light are
    // exactly as wide as the old beam (its 16 px at 32 a tile, times the root's 2: 1 tile), and the beam just
    // as tall (20 tiles); both still grow with the skill's radius (Initialize scales the root). the
    // frames step on game time, so the beam pauses with the game
    // Tommy: 144 (the sheet was made for 30, so the light flows about 5 times as fast, a loop every 0.22 s;
    // a screen slower than 144 Hz shows only some of the frames)
    private const float BeamFps = 144f;
    private static Sprite[] beamFrames;
    private SpriteRenderer beam;
    // the beam's own scale under the root's 2 by 40 stretch: kept every frame (something set it 3.2 times
    // bigger after it was made, Tommy saw a 3 tile beam; LateUpdate logs it if it happens again)
    private Vector3 beamScale = Vector3.one;
    private bool beamScaleReported;
    private float beamTime;
    private int beamFrame = -1;

    private void SetUpBeam()
    {
        if (beamFrames == null || beamFrames.Length == 0)
        {
            beamFrames = Resources.LoadAll<Sprite>("SunrayBeam");
            System.Array.Sort(beamFrames, (a, b) => FrameNumber(a).CompareTo(FrameNumber(b)));
        }
        if (beamFrames.Length == 0) return;
        Transform visual = transform.Find("Visual");
        SpriteRenderer sr = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (sr == null) return;
        // the old beam's size in the root's own units, before its stretch
        Vector2 oldSize = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : new Vector2(0.5f, 0.5f);
        Vector3 stretch = transform.localScale;
        if (Mathf.Approximately(stretch.x, 0f) || Mathf.Approximately(stretch.y, 0f)) return;
        beamScale = new Vector3(1f / stretch.x, 1f / stretch.y, 1f);
        visual.localScale = beamScale;
        sr.sprite = beamFrames[0];
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(beamFrames[0].bounds.size.x, oldSize.y * stretch.y);
        beam = sr;
        beamFrame = 0;
    }

    // what the beam was built at, in world units (Tommy, 2026-11-26: a 3 tile beam at diameter 2; to find why)
    private void LogSize(float radius, float scale)
    {
        string beamInfo = beam != null
            ? $"sprite {(beam.sprite != null ? beam.sprite.name + " " + beam.sprite.rect.size + " ppu " + beam.sprite.pixelsPerUnit : "none")}, size {beam.size}, " +
              $"visual lossy {beam.transform.lossyScale}, world bounds {beam.bounds.size}"
            : "no animated beam (old sprite)";
        string particles = sunParticles != null
            ? $"particles start size {sunParticles.main.startSize.constant}, shape radius {sunParticles.shape.radius}, scaling {sunParticles.main.scalingMode}"
            : "no particles";
        Debug.Log($"[Sunray] radius {radius} (diameter {radius * 2f}), visual scale {scale}, root scale {transform.localScale}; {beamInfo}; {particles}");
    }

    // SunrayBeam_12 -> 12
    private static int FrameNumber(Sprite s)
    {
        int cut = s.name.LastIndexOf('_');
        return cut >= 0 && int.TryParse(s.name.Substring(cut + 1), out int n) ? n : 0;
    }

    // the beam back at its own scale, saying once what had changed it
    private void KeepBeamScale(string when)
    {
        if (beam == null) return;
        Transform v = beam.transform;
        if (v.localScale == beamScale) return;
        if (!beamScaleReported)
        {
            beamScaleReported = true;
            Debug.Log($"[Sunray] the beam's scale was {v.localScale} at {when}, not {beamScale}; set back");
        }
        v.localScale = beamScale;
    }

    private void LateUpdate() => KeepBeamScale("a frame");

    private void Update()
    {
        if (beam == null) return;
        beamTime += Time.deltaTime;
        int frame = (int)(beamTime * BeamFps) % beamFrames.Length;
        if (frame == beamFrame) return;
        beamFrame = frame;
        beam.sprite = beamFrames[frame];
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
        this.endSound = endSound;
        this.endLeadTime = endLeadTime;

        targetScale *= visualScaleMultiplier;
        transform.localScale = targetScale;
        KeepBeamScale("spawn");
        LogSize(aoeRadius, visualScaleMultiplier);

        // the particle system's own scaling mode is Local (ignores parent/hierarchy scale), so
        // the edge shape it spawns particles along doesn't track the beam's width above on its
        // own - keep it in sync manually with the same multiplier
        if (sunParticles != null)
        {
            var shape = sunParticles.shape;
            shape.radius = baseShapeRadius * visualScaleMultiplier;
        }

        if (source is Sunflower sunflower)
        {
            _ambientSunProc = SkillTreeManager.HasUnlock(sunflower, Sunflower.AmbientSunProcUnlock);
            _refreshOnKill  = SkillTreeManager.HasUnlock(sunflower, Sunflower.SunrayRefreshUnlock);
            _homing         = SkillTreeManager.HasUnlock(sunflower, Sunflower.HomingSunrayUnlock);
            _heatstroke     = SkillTreeManager.HasUnlock(sunflower, Sunflower.HeatstrokeUnlock);
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

            DarknessManager.RegisterLightSource(transform, 5f, overhead: true);
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
            // the beam holds still (Tommy, 2026-11-26): it used to rock 40 degrees about its upright axis,
            // about 6 times a second, which made its width jitter

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
                // this pass only reads, so the live insect list is walked directly. the damage
                // below goes over inRange, its own reused list
                List<Insect> inRange = _inRange;
                inRange.Clear();
                List<Insect> allInsects = Insect.allInsects;
                for (int i = 0; i < allInsects.Count; i++)
                {
                    Insect insect = allInsects[i];
                    if (insect == null || !insect.IsAlive) continue;
                    // its hitbox, not just its position (Tommy, 2026-11-26): the circle round its feet the
                    // projectiles use (Insect.BaseHitboxRadius) only has to touch the damage area
                    if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius + insect.BaseHitboxRadius)
                        inRange.Add(insect);
                }

                // every insect in the beam takes the full damage, however many there are (Tommy, 2026-11-27: the
                // falloff, 10% less per other insect down to 50%, is gone)
                foreach (Insect insect in inRange)
                {
                    insect.Damage(damagePerSecond * tickInterval, source.damageType, source.elementalType, source, true, damageTags);
                    if (_ambientSunProc && source is Sunflower ambientSunflower)
                        ambientSunflower.TryReduceSunTimerSmall();
                    if (_refreshOnKill && !insect.IsAlive)
                        duration += KillRefreshDuration;
                    if (_heatstroke && insect.IsAlive)
                        insect.ApplyEffect(new HeatstrokeEffect(insect, HeatstrokeLinger, source));
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

        // mechanical illumination ends the instant the skill's duration is up, independent of
        // however long the visual fade-out animation below takes to finish - LightFader.FadeOut
        // runs its own coroutine in parallel on this same GameObject and (without
        // destroyOnComplete) disables it rather than destroying it once done, which would
        // otherwise race this coroutine and kill it mid-cleanup if the fade finishes first,
        // leaving this object disabled forever instead of unregistered and destroyed
        DarknessManager.UnregisterLightSource(transform);

        // detach the particles before the shrink below starts squashing this transform's own
        // scale to 0 - same reasoning as Projectile.OnBeforeDestroy(): a child particle system
        // using Local scaling mode reads the parent's scale as its own particle-size multiplier,
        // so left attached it would visibly squash/deform along with the beam instead of just
        // finishing its own fade naturally
        DetachParticles();

        // shrink X to 0 and fade light out together
        var fader = GetComponent<LightFader>();
        if (fader != null) fader.FadeOut(shrinkDuration, destroyOnComplete: true);
        else Destroy(gameObject, shrinkDuration);

        float t = 0f;
        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            float scaleX = Mathf.Lerp(targetScale.x, 0f, t / shrinkDuration);
            transform.localScale = new Vector3(scaleX, targetScale.y, targetScale.z);
            yield return null;
        }
    }

    // reparenting with worldPositionStays: false leaves localScale untouched (SetParent(null, true)
    // would rewrite it to cancel out the parent's scale, snapping particle size the instant it
    // detaches), so position/rotation have to be restored manually instead - same pattern as
    // Projectile.OnBeforeDestroy()
    private void DetachParticles()
    {
        if (sunParticles == null) return;

        Transform t = sunParticles.transform;
        Vector3 worldPos = t.position;
        Quaternion worldRot = t.rotation;

        t.SetParent(null, false);
        t.position = worldPos;
        t.rotation = worldRot;

        sunParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(sunParticles.gameObject, sunParticles.main.startLifetime.constantMax);
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
