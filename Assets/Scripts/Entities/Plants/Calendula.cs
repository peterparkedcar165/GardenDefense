using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Calendula : Aura
{
    private CalendulaData CData => data as CalendulaData;
    [SerializeField] private GameObject fireBurstPrefab;

    // floral glow's on hit damage scaling, 25% base, +5% per level, 50% at max level
    public float FloralGlowDamageScaling =>
        (CData?.floralGlowBaseDamageScaling ?? 0.25f) + (CData?.floralGlowDamageScalingPerLevel ?? 0.05f) * effectivePath3Level;

    private bool autoCastEnabled = false;
    private Tile autoCastTargetTile = null;
    private Plant _autoCastHighlighted;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        Plant.OnPlantPlaced += HandlePlantPlaced;
        ApplyAuraToAllInRange();
    }

    private void HandlePlantPlaced(Plant plant)
    {
        if (!IsAlive) return;
        ApplyAuraToAllInRange();
    }

    // applied once on placement, on reaching Path2 max, or whenever any new plant appears on
    // the field (via Plant.OnPlantPlaced) — not re-scanned every tick. removal is handled
    // entirely by CalendulasLightEffect itself (PlantAuraBuffEffect base)
    private void ApplyAuraToAllInRange()
    {
        if (!IsPath2Maxed) return;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > lightEmissionRange) continue;
            plant.ApplyEffect(new CalendulasLightEffect(plant, 1, this, lightEmissionRange, 0.15f));
        }
    }

    protected override bool ShowLight => DarknessManager.instance != null && (DarknessManager.instance.isDark || DarknessManager.instance.pitchBlack);
    protected override bool ShowDarkCircle => false;

    public override void UpdateStats()
    {
        baseLightEmissionRange = baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier);
        coordinatedDamageAdder = IsPath1Maxed ? 0.33f : 0f;
        base.UpdateStats();
    }

    protected override void Update()
    {
        base.Update();

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();

        if (autoCastEnabled)
        {
            // resolved live from the tile (not a pinned instance), so if the target plant dies
            // and gets revived, the auto-cast picks the new instance back up on its own
            Plant currentTarget = Plant.GetPlantOnTile(autoCastTargetTile);
            if (currentTarget != null && currentTarget.IsAlive && SkillReady)
            {
                int myLevel = effectivePath3Level + 1;
                FloralGlowEffect existing = currentTarget.GetEffect<FloralGlowEffect>();
                // a stronger instance is already active (e.g. from another Calendula), wait it out
                if (existing == null || existing.level <= myLevel)
                    CastFloralGlow(currentTarget, myLevel);
            }
        }

        UpdateAutoCastHighlight();
    }

    // while this Calendula is selected and auto casting, highlight its locked target in yellow
    private void UpdateAutoCastHighlight()
    {
        Plant desired = (IsSelected && autoCastEnabled) ? Plant.GetPlantOnTile(autoCastTargetTile) : null;
        if (_autoCastHighlighted != null && _autoCastHighlighted != desired)
            _autoCastHighlighted.ClearHighlight();
        if (desired != null)
            desired.SetHighlight(Color.yellow);
        _autoCastHighlighted = desired;
    }

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled, targetTile = autoCastTargetTile };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled || state.targetTile == null) return;
        autoCastEnabled = true;
        autoCastTargetTile = state.targetTile;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Plant.OnPlantPlaced -= HandlePlantPlaced;
        _autoCastHighlighted?.ClearHighlight();
    }

    // click Auto Cast to pick a target, click again to turn it off
    public override void ToggleAutoCast()
    {
        if (autoCastEnabled)
        {
            autoCastEnabled = false;
            autoCastTargetTile = null;
            return;
        }
        SkillTargetingManager.instance.BeginPlantTargeting(OnAutoCastTargetConfirmed, this);
    }

    private void OnAutoCastTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return;
        autoCastEnabled = true;
        autoCastTargetTile = targetPlant.occupiedTile;
    }

    // drives both the visual burst's travel time (SpawnFireBurst) and how long the live damage
    // sweep takes to expand from 0 to attackRange. public so FloralGlowEffect's explosion can
    // share it
    public const float FireBurstLifetime = 0.3f;

    protected override void Attack()
    {
        base.Attack();
        SpawnFireBurst(transform.position, attackRange);

        // snapshot only the damage values (so a mid-burst attackDamage change can't retroactively
        // affect it), not the target list — who gets hit is decided live, frame by frame, below
        float snapshotDamage = attackDamage;
        DamageType snapshotDamageType = damageType;
        ElementalType snapshotElementalType = elementalType;
        StartCoroutine(SweepAttackDamage(snapshotDamage, snapshotDamageType, snapshotElementalType));
    }

    // the fire spreads outward from Calendula over FireBurstLifetime. every frame, ANY insect
    // currently within the growing radius is damaged — including one that wasn't even in range
    // when the attack fired but wanders into the expanding burst zone partway through. each
    // insect can only be hit once per attack
    private IEnumerator SweepAttackDamage(float damage, DamageType dmgType, ElementalType elemType)
    {
        DamageTag[] tags = new DamageTag[] { DamageTag.AoE, DamageTag.Attack };
        HashSet<Insect> hit = new HashSet<Insect>();
        float elapsed = 0f;

        while (elapsed < FireBurstLifetime)
        {
            float currentRadius = (elapsed / FireBurstLifetime) * attackRange;
            foreach (Insect insect in new List<Insect>(Insect.allInsects))
            {
                if (insect == null || !insect.IsAlive || hit.Contains(insect)) continue;
                if (Vector3.Distance(transform.position, insect.transform.position) > currentRadius) continue;
                hit.Add(insect);
                insect.Damage(damage, dmgType, elemType, this, true, tags);
            }
            yield return null;
            elapsed += Time.deltaTime;
        }

        // catch anyone the wavefront should have reached by now but a frame gap missed
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || hit.Contains(insect)) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) > attackRange) continue;
            hit.Add(insect);
            insect.Damage(damage, dmgType, elemType, this, true, tags);
        }
    }

    // same fire burst visual used by the attack, reused at a smaller radius by Floral Glow's explosion
    public void SpawnFireBurst(Vector3 position, float radius)
    {
        if (fireBurstPrefab == null) return;
        GameObject burst = Instantiate(fireBurstPrefab, position, Quaternion.identity);
        ParticleSystem ps = burst.GetComponent<ParticleSystem>();
        if (ps == null) return;

        const float lifetime = FireBurstLifetime;

        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            radius * 0.7f / lifetime,
            radius        / lifetime);

        var lvol = ps.limitVelocityOverLifetime;
        lvol.enabled = true;
        lvol.separateAxes = false;
        lvol.dampen = 0.4f;
        AnimationCurve limitCurve = new AnimationCurve(
            new Keyframe(0f,   1f, 0f, 0f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f,   0f, 0f, 0f)
        );
        lvol.limit = new ParticleSystem.MinMaxCurve(radius / lifetime, limitCurve);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
    }

    private void OnTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return;
        int myLevel = effectivePath3Level + 1;
        FloralGlowEffect existing = targetPlant.GetEffect<FloralGlowEffect>();
        if (existing != null && existing.level > myLevel)
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
            return;
        }
        CastFloralGlow(targetPlant, myLevel);
    }

    // shared by the manual skill cast and auto cast, does not reopen targeting on its own
    private void CastFloralGlow(Plant targetPlant, int level)
    {
        skillCooldownTimer = skillCooldown;
        targetPlant.ApplyEffect(new FloralGlowEffect(targetPlant, skillDuration, level, this, this));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (CData?.path1AttackDamagePerLevel ?? 5f)  * level;
        baseFireDamage   = (CData?.path1FireDamagePerLevel ?? 0.05f) * level;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAttackRange = data.baseAttackRange + (CData?.path2AttackRangePerLevel ?? 0.175f) * level;
        ApplyAuraToAllInRange();
    }

    public override void OnPath3Unlock()
    {
        skillCooldownTimer = 0f;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (CData?.path3SkillDurationPerLevel ?? 2f) * level;
    }

    public override string GetName() => "<b><color=orange>Calendula</color></b>";
    public override string GetDescription() => $"The {GetName()} periodically releases waves of flaming petals and can infuse allies with fire energy.";
    public override string GetPath1Name() => "Petals";
    public override string GetPath2Name() => "Illuminate";
    public override string GetPath3Name() => "Floral Glow";

    public override string GetAttackDescription()
        => $"Releases flaming petals dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects within range.";

    public override string GetPassiveDescription() =>
        $"Illuminate the surrounding area allowing plants to see insects.\n\n" +
        $"<color=green><b>Base Illumination Range</b></color> is equal to <color=green><b>Attack Range</b></color>.";

    public override string GetSkillDesription() =>
        $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>{skillDuration:F0}s</b></color>. The plant's projectile attacks deal an additional <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage * FloralGlowDamageScaling:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} on hit. Emits light equal to <b><color=orange>Calendula</color></b>'s <color=green><b>Base Illumination Range</b></color>.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl   = CData?.path1AttackDamagePerLevel ?? 5f;
        float firepl = CData?.path1FireDamagePerLevel    ?? 0.05f;
        string desc = details
            ? $"Releases flaming petals dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects within range."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=orange><b>Fire Damage</b></color> by <color=green><b>{firepl * 100f:F0}%</b></color> per level. [<color=green><b>+{firepl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Increase <color=#6495ED><b>Coordinated Damage</b></color> by <color=green><b>33%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float rangepl = CData?.path2AttackRangePerLevel ?? 0.175f;
        string p2Bonus = "Plants within illumination range gain <color=orange><b>Calendula's Light</b></color>, increasing <color=green><b>Attack Speed</b></color> by <color=green><b>15%</b></color>.";
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path2Level, p2Bonus)}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl  = CData?.path3SkillDurationPerLevel ?? 2f;
        float dmgScalingBase = CData?.floralGlowBaseDamageScaling ?? 0.25f;
        float dmgScalingPerLevel = CData?.floralGlowDamageScalingPerLevel ?? 0.05f;
        string desc = details
            ? $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. The plant's projectile attacks deal an additional <color=green><b>[({dmgScalingBase * 100f:F0}%) + ({dmgScalingPerLevel * 100f:F0}%/Lvl.) Attack Damage + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.DamageTypeLabel(damageType)} on hit. Emits light equal to <b><color=orange>Calendula</color></b>'s <color=green><b>Base Illumination Range</b></color>."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Attack Damage</b></color> scaling by <color=green><b>{dmgScalingPerLevel * 100f:F0}%</b></color> per level. [<color=green><b>+{dmgScalingPerLevel * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Damage dealt by <color=orange><b>Floral Glow</b></color> now affects other insects in a <color=green><b>2</b></color>-radius.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
