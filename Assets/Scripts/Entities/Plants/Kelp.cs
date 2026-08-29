using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Kelp : Aura
{
    private KelpData KData => data as KelpData;

    [SerializeField] private GameObject bubbleProjectilePrefab;
    [SerializeField] private GameObject bubbleIndicatorPrefab;
    private GameObject bubbleIndicatorInstance;

    private bool autoCastEnabled = false;
    private Vector3 autoCastPosition;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    public override bool UsesTargeting => true;

    private float AttackSplashRadius => KData?.attackSplashRadius ?? 1f;

    private float OxygenReplenishPerHit    => (KData?.baseOxygenReplenish ?? 5f) + (KData?.path2OxygenReplenishPerLevel ?? 1f) * effectivePath2Level;
    private float OxygenRequirementPerSun  => Mathf.Max(10f, (KData?.baseOxygenRequirement ?? 100f) - (KData?.path2OxygenRequirementReductionPerLevel ?? 10f) * effectivePath2Level);
    private int   SunPerThreshold          => IsPath2Maxed ? 2 : 1;

    private float BubbleWidth          => (KData?.baseBubbleWidth         ?? 0.8f) + (KData?.path3BubbleWidthPerLevel        ?? 0.15f) * effectivePath3Level;
    private float BubbleInitialOxygen  => (KData?.baseBubbleInitialOxygen  ?? 20f) + (KData?.path3BubbleInitialOxygenPerLevel ?? 5f)   * effectivePath3Level;
    private float BubbleRegenPerSecond => (KData?.baseBubbleRegenPerSecond ?? 2f)  + (KData?.path3BubbleRegenPerLevel        ?? 0.5f) * effectivePath3Level;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        // the Oxygen-progress bar reuses the standard passive bar: it starts empty (timer ==
        // cooldown) and counts down toward 0 as Oxygen is granted, instead of over time
        passiveCooldownTimer = passiveCooldown;
    }

    // basePassiveCooldown always reflects the current Oxygen-per-Sun threshold, so the passive
    // bar's built-in fill math (1 - timer/cooldown) reads naturally as "progress toward the next Sun"
    public override void UpdateStats()
    {
        basePassiveCooldown = OxygenRequirementPerSun;
        base.UpdateStats();
    }

    protected override void Update()
    {
        base.Update();
        air = 100f; // Kelp is aquatic: her own Oxygen never depletes
        // cancel the base class's time-based passive countdown: only Oxygen granted moves this
        passiveCooldownTimer += Time.deltaTime;

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();

        if (autoCastEnabled && SkillReady)
            OnTargetConfirmed(autoCastPosition);

        UpdateBubbleIndicator();
    }

    // click Auto Cast to pick a direction, click again to turn it off
    public override void ToggleAutoCast()
    {
        if (autoCastEnabled)
        {
            autoCastEnabled = false;
            return;
        }
        BeginBubbleTargeting(OnAutoCastTargetConfirmed);
    }

    private void OnAutoCastTargetConfirmed(Vector3 position)
    {
        autoCastEnabled = true;
        autoCastPosition = position;
    }

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled, targetPosition = autoCastPosition };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled) return;
        autoCastEnabled = true;
        autoCastPosition = state.targetPosition;
    }

    protected override void Attack()
    {
        base.Attack();

        GameObject primary = FindTarget();
        Insect primaryInsect = primary != null ? primary.GetComponent<Insect>() : null;
        if (primaryInsect == null || !primaryInsect.IsAlive) return;

        primaryInsect.Damage(attackDamage, damageType, elementalType, this, true,
            new DamageTag[] { DamageTag.Attack, DamageTag.Melee, DamageTag.SingleTarget });

        if (IsPath1Maxed)
        {
            foreach (Insect insect in new List<Insect>(Insect.allInsects))
            {
                if (insect == null || insect == primaryInsect || !insect.IsAlive) continue;
                if (Vector3.Distance(primaryInsect.transform.position, insect.transform.position) > AttackSplashRadius) continue;
                insect.Damage(attackDamage, damageType, elementalType, this, true,
                    new DamageTag[] { DamageTag.Attack, DamageTag.Melee, DamageTag.AoE });
            }
        }

        ReleaseBubbles();
    }

    private GameObject FindTarget()
    {
        switch (targeting)
        {
            case TARGETING.Nearest:   return FindNearest(Insect.allInsects);
            case TARGETING.First:     return FindFirst(Insect.allInsects);
            case TARGETING.Last:      return FindLast(Insect.allInsects);
            case TARGETING.Strongest: return FindStrongest(Insect.allInsects);
            default:                  return null;
        }
    }

    // fires on every attack hit: a burst of Oxygen to nearby plants (scaled by each receiving
    // plant's own Respiration, same as every other Oxygen-replenishing source), credited toward
    // Kelp's own cumulative-Oxygen Sun tracker via AccumulateOxygen
    private void ReleaseBubbles()
    {
        float baseAmount = OxygenReplenishPerHit;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == this) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;

            AccumulateOxygen(plant.ReplenishOxygen(baseAmount));
        }
    }

    // shared by the attack passive and the skill's Air Bubble effect (both instant burst and
    // per-second regen): the passive bar (same field Sunflower uses for its sun-cooldown display)
    // tracks cumulative Oxygen granted instead of time, refilling to a full threshold once it
    // drains to 0 or below, at which point Sun is granted
    public void AccumulateOxygen(float amount)
    {
        passiveCooldownTimer -= amount;
        while (passiveCooldownTimer <= 0f)
        {
            GenerateSun(SunPerThreshold);
            passiveCooldownTimer += passiveCooldown;
        }
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        BeginBubbleTargeting(OnTargetConfirmed);
    }

    // shared by the manual skill cast and the auto-cast direction picker: both aim with the
    // same corridor indicator, just confirming into a different callback
    private void BeginBubbleTargeting(System.Action<Vector3> callback)
    {
        if (bubbleIndicatorInstance != null) return;
        SkillTargetingManager.instance.BeginTargeting(0f, callback);
        if (bubbleIndicatorPrefab != null)
        {
            bubbleIndicatorInstance = Instantiate(bubbleIndicatorPrefab, transform.position, Quaternion.identity);
            bubbleIndicatorInstance.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    // straight corridor from Kelp to bubbleMaxRange, matching the bubble's actual travel range
    // and current width, mirroring Dandelion's WindGust aim indicator
    private void UpdateBubbleIndicator()
    {
        if (bubbleIndicatorInstance == null) return;

        if (!SkillTargetingManager.instance.IsTargeting)
        {
            Destroy(bubbleIndicatorInstance);
            bubbleIndicatorInstance = null;
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        mouseWorld.z = 0f;

        float range = KData?.bubbleMaxRange ?? 15f;
        Vector2 dir   = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Vector3 indicatorCenter = transform.position + (Vector3)(dir * range * 0.5f);
        bubbleIndicatorInstance.transform.SetPositionAndRotation(indicatorCenter, Quaternion.Euler(0f, 0f, angle));
        bubbleIndicatorInstance.transform.localScale = new Vector3(range, BubbleWidth, 1f);
        bubbleIndicatorInstance.GetComponent<SpriteRenderer>().enabled = true;
    }

    private void OnTargetConfirmed(Vector3 targetPosition)
    {
        skillCooldownTimer = skillCooldown;
        if (bubbleProjectilePrefab == null) return;

        Vector2 direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        GameObject obj = Instantiate(bubbleProjectilePrefab, transform.position, Quaternion.identity);
        obj.GetComponent<KelpBubbleProjectile>()?.Initialize(
            direction,
            KData?.bubbleTravelSpeed ?? 6f,
            KData?.bubbleMaxRange    ?? 15f,
            BubbleWidth,
            BubbleInitialOxygen,
            BubbleRegenPerSecond,
            skillDuration,
            this);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (KData?.path1AttackDamagePerLevel ?? 5f) * level;
        baseAttackRange  = data.baseAttackRange  + (KData?.path1AttackRangePerLevel  ?? 0.2f) * level;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + (KData?.path2AttackSpeedPerLevel ?? 0.05f) * level;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (KData?.path3DurationPerLevel ?? 2f) * level;
    }

    public override string GetName() => "<b><color=#2E8B57>Kelp</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} whips insects with her fronds and keeps the garden breathing, releasing Oxygen-rich bubbles that also generate Sun.";

    public override string GetAttackDescription() =>
        $"Whips the target insect, dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        $"Hitting a target releases air bubbles, granting plants within range <color=green><b>{OxygenReplenishPerHit:F1}</b></color> <color=#4FC3F7><b>Oxygen</b></color>. For every <color=green><b>{OxygenRequirementPerSun:F0}</b></color> <color=#4FC3F7><b>Oxygen</b></color> provided, Kelp produces <color=yellow><b>{SunPerThreshold}</b></color> <color=yellow>Sun</color>.";

    public override string GetSkillDesription() =>
        $"Fires a bubble in a chosen direction. Plants it touches are enveloped in an <color=#4FC3F7><b>Air Bubble</b></color> for <color=green><b>{skillDuration:F0}s</b></color>, instantly restoring <color=green><b>{BubbleInitialOxygen:F0}</b></color> <color=#4FC3F7><b>Oxygen</b></color> and regenerating <color=green><b>{BubbleRegenPerSecond:F1}</b></color> per second.";

    public override string GetPath1Name() => "Whip";
    public override string GetPath2Name() => "Bubbles";
    public override string GetPath3Name() => "Current";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = KData?.path1AttackDamagePerLevel ?? 5f;
        float rpl  = KData?.path1AttackRangePerLevel  ?? 0.2f;
        string desc = details
            ? $"Whips the target insect, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rpl:F2}</b></color> per level. [<color=green><b>+{rpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Whips can also hit nearby insects within a <color=green><b>{AttackSplashRadius:F0}</b></color> radius of the target.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float oxypl  = KData?.path2OxygenReplenishPerLevel ?? 1f;
        float reqpl  = KData?.path2OxygenRequirementReductionPerLevel ?? 10f;
        float aspl   = KData?.path2AttackSpeedPerLevel ?? 0.05f;
        string desc = details
            ? $"Hitting a target releases air bubbles, granting plants within range <color=green><b>[({KData?.baseOxygenReplenish ?? 5f:F1}) + ({oxypl:F1}/Lvl.)]</b></color> <color=#4FC3F7><b>Oxygen</b></color>. For every <color=green><b>[({KData?.baseOxygenRequirement ?? 100f:F0}) - ({reqpl:F0}/Lvl.)]</b></color> <color=#4FC3F7><b>Oxygen</b></color> provided, Kelp produces <color=yellow><b>1</b></color> <color=yellow>Sun</color>."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath2Level:F2}</b></color>]\n\n" +
               $"Increase Oxygen replenished per hit by <color=green><b>{oxypl:F1}</b></color> per level. [<color=green><b>+{oxypl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"Decrease Oxygen required per Sun by <color=green><b>{reqpl:F0}</b></color> per level. [<color=green><b>-{reqpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, "Increase Sun produced per threshold by <color=green><b>1</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl   = KData?.path3DurationPerLevel ?? 2f;
        float initpl  = KData?.path3BubbleInitialOxygenPerLevel ?? 5f;
        float regenpl = KData?.path3BubbleRegenPerLevel ?? 0.5f;
        float widthpl = KData?.path3BubbleWidthPerLevel ?? 0.15f;
        string desc = details
            ? $"Fires a bubble in a chosen direction. Plants it touches are enveloped in an <color=#4FC3F7><b>Air Bubble</b></color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds, instantly restoring <color=green><b>[({KData?.baseBubbleInitialOxygen ?? 20f:F0}) + ({initpl:F0}/Lvl.)]</b></color> <color=#4FC3F7><b>Oxygen</b></color> and regenerating <color=green><b>[({KData?.baseBubbleRegenPerSecond ?? 2f:F1}) + ({regenpl:F1}/Lvl.)]</b></color> per second."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase initial Oxygen restored by <color=green><b>{initpl:F0}</b></color> per level. [<color=green><b>+{initpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase Oxygen regeneration by <color=green><b>{regenpl:F1}</b></color> per second per level. [<color=green><b>+{regenpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"Increase Bubble width by <color=green><b>{widthpl:F2}</b></color> per level. [<color=green><b>+{widthpl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"<color=#4FC3F7><b>Air Bubble</b></color> also grants <color=yellow><b>100%</b></color> Sun Yield.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
