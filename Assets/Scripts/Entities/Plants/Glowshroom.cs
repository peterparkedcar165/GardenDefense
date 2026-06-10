using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

public class Glowshroom : Shooter
{
    private GlowshroomData GMData => data as GlowshroomData;
    protected override float LightIntensity => 1f;

    private bool _lightColored = false;

    private float SplashRadius       => GMData?.splashRadius              ?? 1.5f;
    private float SplashMultBase     => GMData?.splashDamageMultiplier    ?? 0.5f;
    private float SplashMultMP       => (GMData?.splashDamageMPMultiplier ?? 0.5f) * magicPower / 100f;
    private float SplashMult         => SplashMultBase + SplashMultMP;
    private float FungalGlowDuration => (GMData?.fungalGlowDuration       ?? 6f) + (GMData?.path2FungalGlowDurationPerLevel ?? 1f) * effectivePath2Level;
    private float SpreadRadius       => (GMData?.fungalGlowSpreadRadius    ?? 2f) + (GMData?.path2SpreadRadiusPerLevel       ?? 0.25f) * effectivePath2Level;
    private float LightMult          => GMData?.lightRadiusMultiplier      ?? 3f;
    private float BlindDurationBase  => (GMData?.blindDuration             ?? 3f) + (GMData?.path3BlindDurationPerLevel     ?? 0.5f) * effectivePath3Level;
    private float BlindDurationMP    => (GMData?.blindDurationMPMultiplier ?? 0.04f) * magicPower;
    private float BlindDuration      => BlindDurationBase + BlindDurationMP;
    private float BlindPenalty       => GMData?.blindAccuracyPenalty       ?? 1f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();
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

    public void OnBoltHit(Vector3 hitPosition, Insect mainTarget)
    {
        // apply Fungal Glow to main target
        if (mainTarget != null && mainTarget.IsAlive)
            mainTarget.ApplyEffect(new FungalGlowEffect(mainTarget, FungalGlowDuration, 1, this, SpreadRadius));

        // splash damage + Fungal Glow to nearby insects
        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (insect == mainTarget) continue;
            if (Vector3.Distance(hitPosition, insect.transform.position) <= SplashRadius)
            {
                insect.Damage(attackDamage * SplashMult, damageType, elementalType, this, false,
                    new DamageTag[] { DamageTag.AoE });
                if (insect.IsAlive)
                    insect.ApplyEffect(new FungalGlowEffect(insect, FungalGlowDuration, 1, this, SpreadRadius));
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

        yield return new WaitForSeconds(skillDuration);

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
        $"The {GetName()} is a bioluminescent cave fungus that illuminates the darkness, infects insects with a spreading glow, and blinds them with a sudden flash of light.";

    public override string GetAttackDescription() =>
        $"Fires a fungal bolt dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the target, splashing <color=green><b>{SplashMultBase * 100f:F0}%</b></color> [<color=#FFB6C1><b>+{SplashMultMP * 100f:F0}%</b></color>] of that damage to all insects within <color=green><b>{SplashRadius:F1}</b></color> radius.";

    public override string GetPassiveDescription() =>
        $"Dealing damage inflicts <color=#88FF88>Fungal Glow</color>, causing the insect to emit a faint light for <color=green><b>{FungalGlowDuration:F0}s</b></color>. When a glowing insect takes <color=#4FC3F7><b>Water</b></color> damage, the duration is refreshed and the glow spreads to all insects within <color=green><b>{SpreadRadius:F1}</b></color> radius.";

    public override string GetSkillDesription() =>
        $"Unleashes a blinding flash, tripling the illumination radius to <color=green><b>{lightEmissionRange * LightMult:F1}</b></color> for <color=green><b>{skillDuration:F0}s</b></color>. All insects caught in the expanded radius are <color=#DDDDDD><b>Blinded</b></color> for <color=green><b>{BlindDurationBase:F1}s</b></color> [<color=#FFB6C1><b>+{BlindDurationMP:F1}s</b></color>], causing their attacks to miss.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl    = GMData?.path1AttackDamagePerLevel ?? 8f;
        float rangepl = GMData?.path1AttackRangePerLevel  ?? 0.15f;
        string scaling = details
            ? $"Scaling: <color=#FFB6C1><b>{(GMData?.splashDamageMPMultiplier ?? 0.5f) * 100f:F0}%</b></color> Magic Power (Splash Damage)\n\n" +
              $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
              $"Increase Attack Range by <color=green><b>{rangepl:F2}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F2}</b></color>]"
            : $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color>.\n\n" +
              $"Increase Attack Range by <color=green><b>{rangepl:F2}</b></color>.";
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float durpl    = GMData?.path2FungalGlowDurationPerLevel ?? 1f;
        float radiuspl = GMData?.path2SpreadRadiusPerLevel       ?? 0.25f;
        string scaling = details
            ? $"Increase <color=#88FF88>Fungal Glow</color> duration by <color=green><b>{durpl:F0}s</b></color> per level. [<color=green><b>+{durpl * effectivePath2Level:F0}s</b></color>]\n\n" +
              $"Increase spread radius by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath2Level:F2}</b></color>]"
            : $"Increase <color=#88FF88>Fungal Glow</color> duration by <color=green><b>{durpl:F0}s</b></color>.\n\n" +
              $"Increase spread radius by <color=green><b>{radiuspl:F2}</b></color>.";
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float blindpl = GMData?.path3BlindDurationPerLevel ?? 0.5f;
        float durpl   = GMData?.path3SkillDurationPerLevel ?? 1f;
        string scaling = details
            ? $"Scaling: <color=#FFB6C1><b>{(GMData?.blindDurationMPMultiplier ?? 0.04f) * 100f:F0}s</b></color> Blind per 100 Magic Power\n\n" +
              $"Increase <color=#DDDDDD>Blind</color> duration by <color=green><b>{blindpl:F1}s</b></color> per level. [<color=green><b>+{blindpl * effectivePath3Level:F1}s</b></color>]\n\n" +
              $"Increase skill duration by <color=green><b>{durpl:F0}s</b></color> per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]"
            : $"Increase <color=#DDDDDD>Blind</color> duration by <color=green><b>{blindpl:F1}s</b></color>.\n\n" +
              $"Increase skill duration by <color=green><b>{durpl:F0}s</b></color>.";
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
