using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

public class Glowshroom : Shooter
{
    private GlowshroomData GMData => data as GlowshroomData;
    protected override float LightIntensity => 1f;

    private bool _lightColored = false;

    // no targeting needed: the skill just flashes from her own position
    private bool autoCastEnabled = false;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    private float SplashRadius       => GMData?.splashRadius              ?? 1.5f;
    private float SplashMultBase     => GMData?.splashDamageMultiplier    ?? 0.5f;
    private float SplashMultMP       => (GMData?.splashDamageMPMultiplier ?? 0.5f) * magicPower / 100f;
    private float SplashMult         => SplashMultBase + SplashMultMP;
    private float FungalGlowDuration => (GMData?.fungalGlowDuration       ?? 6f) + (GMData?.path2FungalGlowDurationPerLevel ?? 1f) * effectivePath2Level;
    private float LightMult          => GMData?.lightRadiusMultiplier      ?? 3f;
    private float BlindDurationBase  => (GMData?.blindDuration             ?? 3f) + (GMData?.path3BlindDurationPerLevel     ?? 0.5f) * effectivePath3Level;
    private float BlindDurationMP    => (GMData?.blindDurationMPMultiplier ?? 0.04f) * magicPower;
    private float BlindDuration      => BlindDurationBase + BlindDurationMP;
    private float BlindPenalty       => GMData?.blindAccuracyPenalty       ?? 1f;

    private int   DeathSunGeneration => Mathf.RoundToInt((GMData?.deathSunGeneration ?? 3f) + (GMData?.path2SunGenerationPerLevel ?? 1f) * effectivePath2Level);
    private float DeathBlindDuration => GMData?.deathBlindDuration ?? 4f;
    private float DeathBlindRadius   => GMData?.deathBlindRadius   ?? 2f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();

        if (autoCastEnabled && SkillReady)
            ActivateSkill();
    }

    // click Auto Cast to toggle it on, click again to turn it off — no target to pick
    public override void ToggleAutoCast() => autoCastEnabled = !autoCastEnabled;

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled) return;
        autoCastEnabled = true;
    }

    // innate smart targeting: still resolves via the player's chosen targeting mode (First/
    // Nearest/Last/Strongest), but prefers candidates that don't have Fungal Glow yet, so
    // attacks spread the glow around instead of piling it onto whichever insect got hit first.
    // only falls back to a glowing target when every valid insect already has it
    protected override GameObject FindTarget()
    {
        List<Insect> withoutGlow = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect.HasEffect<FungalGlowEffect>()) continue;
            withoutGlow.Add(insect);
        }

        GameObject target = FindByTargeting(withoutGlow);
        return target != null ? target : FindByTargeting(Insect.allInsects);
    }

    private GameObject FindByTargeting(List<Insect> insects)
    {
        switch (targeting)
        {
            case TARGETING.First:     return FindFirst(insects);
            case TARGETING.Nearest:   return FindNearest(insects);
            case TARGETING.Last:      return FindLast(insects);
            case TARGETING.Strongest: return FindStrongest(insects);
            default:                  return null;
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;

        GameObject targetGO = FindTarget();
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        GlowshroomProjectile bolt = proj.GetComponent<GlowshroomProjectile>();
        if (bolt != null)
        {
            bolt.SetTarget(targetGO);
            bolt.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing,
                            damageType, elementalType, this, SplashRadius, SplashMult);
        }
    }

    // called by FungalGlowEffect.OnTargetDied() whenever a glowing insect dies, regardless of
    // cause. Sun generation always triggers; the blind flash is a Path2-max-only bonus
    public void OnFungalGlowInsectDied(Vector3 position)
    {
        GenerateSun(DeathSunGeneration);

        if (!IsPath2Maxed) return;
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(position, insect.transform.position) > DeathBlindRadius) continue;
            insect.ApplyEffect(new BlindEffect(insect, DeathBlindDuration, 1, this, BlindPenalty));
        }
    }

    public void OnBoltHit(Vector3 hitPosition, Insect mainTarget)
    {
        // apply Fungal Glow to main target
        if (mainTarget != null && mainTarget.IsAlive)
            mainTarget.ApplyEffect(new FungalGlowEffect(mainTarget, FungalGlowDuration, 1, this));

        // splash damage to nearby insects
        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (insect == mainTarget) continue;
            if (Vector3.Distance(hitPosition, insect.transform.position) <= SplashRadius)
            {
                insect.Damage(attackDamage * SplashMult, damageType, elementalType, this, false,
                    new DamageTag[] { DamageTag.AoE });
                if (insect.IsAlive && IsPath1Maxed)
                    insect.ApplyEffect(new FungalGlowEffect(insect, FungalGlowDuration, 1, this));
            }
        }
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        StartCoroutine(FlashSkill());
    }

    private IEnumerator FlashSkill()
    {
        float bonus = LightMult - 1f;
        lightEmissionRangeMultiplier += bonus;

        float expandedRange = lightEmissionRange * LightMult;
        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= expandedRange)
                insect.ApplyEffect(new BlindEffect(insect, BlindDuration, 1, this, BlindPenalty));
        }

        float elapsed = 0f;
        while (elapsed < skillDuration)
        {
            elapsed += Time.deltaTime;
            if (IsPath3Maxed)
            {
                foreach (Insect insect in new List<Insect>(Insect.allInsects))
                {
                    if (insect == null || !insect.IsAlive) continue;
                    if (Vector3.Distance(transform.position, insect.transform.position) > expandedRange) continue;
                    BlindEffect existing = insect.GetEffect<BlindEffect>();
                    if (existing != null)
                        existing.RefreshDuration(BlindDuration);
                    else
                        insect.ApplyEffect(new BlindEffect(insect, BlindDuration, 1, this, BlindPenalty));
                }
            }
            yield return null;
        }

        // ramp the bonus back down so the light radius shrinks smoothly instead of snapping
        float fadeDuration = 1f;
        float applied = bonus;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float target = Mathf.Lerp(bonus, 0f, t / fadeDuration);
            lightEmissionRangeMultiplier += target - applied;
            applied = target;
            yield return null;
        }
        lightEmissionRangeMultiplier -= applied;
    }

    public override void UpdateStats()
    {
        baseLightEmissionRange = (baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier)) * 1.25f;
        base.UpdateStats();

        if (!_lightColored)
        {
            Light2D light = GetComponentInChildren<Light2D>();
            if (light != null)
            {
                light.color = new Color(0.05f, 0.65f, 1f);
                _lightColored = true;
            }
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (GMData?.path1AttackDamagePerLevel ?? 8f)   * level;
        baseAttackRange  = data.baseAttackRange  + (GMData?.path1AttackRangePerLevel  ?? 0.15f) * level;
    }

    public override void OnPath2Upgrade(int level) { }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (GMData?.path3SkillDurationPerLevel ?? 1f) * level;
    }

    public override string GetName() => "<b><color=green>Glowshroom</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a bioluminescent cave fungus that illuminates the darkness, infects insects with a glow, and blinds them with a sudden flash of light.";

    public override string GetAttackDescription() =>
        $"Fires a fungal bolt dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to the target, splashing <color=green><b>{SplashMultBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{SplashMultMP * 100f:F0}%</b></color>] of that damage to all insects within <color=green><b>{SplashRadius:F1}</b></color> radius.";

    public override string GetPassiveDescription() =>
        $"Dealing damage inflicts <color=#88FF88>Fungal Glow</color>, causing the insect to emit a faint light for <color=green><b>{FungalGlowDuration:F0}s</b></color>. When a glowing insect takes <color=#4FC3F7><b>Water</b></color> damage, the duration is refreshed. Whenever an insect affected by <color=#88FF88>Fungal Glow</color> dies, the {GetName()} generates <color=yellow><b>{DeathSunGeneration}</b></color> <color=yellow>Sun</color>.";

    public override string GetSkillDesription() =>
        $"Unleashes a blinding flash, tripling the illumination radius to <color=green><b>{lightEmissionRange * LightMult:F1}</b></color> for <color=green><b>{skillDuration:F0}s</b></color>. All insects caught in the expanded radius are <color=#DDDDDD><b>Blinded</b></color> for <color=green><b>{BlindDurationBase:F1}s</b></color> [<color=#FFB6C1><b>+{BlindDurationMP:F1}s</b></color>], causing their attacks to miss.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl    = GMData?.path1AttackDamagePerLevel ?? 8f;
        float rangepl = GMData?.path1AttackRangePerLevel  ?? 0.15f;
        float splashMP = GMData?.splashDamageMPMultiplier ?? 0.5f;
        string desc = details
            ? $"Fires a fungal bolt dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} to the target, splashing <color=green><b>{(GMData?.splashDamageMultiplier ?? 0.5f) * 100f:F0}%</b></color> <color=#FFB6C1>[+{splashMP * 100f:F0}% Magic Power]</color> of that damage to all insects within <color=green><b>{SplashRadius:F1}</b></color> radius."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F2}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Splash damage now applies <color=#88FF88>Fungal Glow</color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float durpl = GMData?.path2FungalGlowDurationPerLevel ?? 1f;
        float sunpl = GMData?.path2SunGenerationPerLevel ?? 1f;
        string desc = details
            ? $"Dealing damage inflicts <color=#88FF88>Fungal Glow</color>, causing the insect to emit a faint light for <color=green><b>[({GMData?.fungalGlowDuration ?? 6f:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. When a glowing insect takes <color=#4FC3F7><b>Water</b></color> damage, the duration is refreshed. Whenever an insect affected by <color=#88FF88>Fungal Glow</color> dies, the {GetName()} generates <color=yellow><b>[({GMData?.deathSunGeneration ?? 3f:F0}) + ({sunpl:F0}/Lvl.)]</b></color> <color=yellow>Sun</color>."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=#88FF88>Fungal Glow</color> duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase Sun generated per death by <color=green><b>{sunpl:F0}</b></color> per level. [<color=green><b>+{sunpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Whenever an insect affected by <color=#88FF88>Fungal Glow</color> dies, it creates a flash which <color=#DDDDDD><b>Blinds</b></color> nearby insects for <color=green><b>{DeathBlindDuration:F0}s</b></color> in a <color=green><b>{DeathBlindRadius:F0}</b></color> radius.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float blindpl  = GMData?.path3BlindDurationPerLevel   ?? 0.5f;
        float durpl    = GMData?.path3SkillDurationPerLevel   ?? 1f;
        float blindMP  = GMData?.blindDurationMPMultiplier    ?? 0.04f;
        string desc = details
            ? $"Unleashes a blinding flash, tripling the illumination radius for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. All insects caught in the expanded radius are <color=#DDDDDD><b>Blinded</b></color> for <color=green><b>[({GMData?.blindDuration ?? 3f:F1}) + ({blindpl:F1}/Lvl.) + <color=#FFB6C1>{blindMP * 100f:F0}% Magic Power</color>]</b></color> seconds, causing their attacks to miss."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=#DDDDDD>Blind</color> duration by <color=green><b>{blindpl:F1}</b></color> seconds per level. [<color=green><b>+{blindpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase skill duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Blind applies continuously while the skill is active.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
