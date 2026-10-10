using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Calendula : Aura
{
    private CalendulaData CData => data as CalendulaData;
    [SerializeField] private GameObject fireBurstPrefab;

    [Header("Floral Glow Delivery")]
    // casting the skill no longer applies Floral Glow instantly - this is fired from Calendula at
    // the targeted plant, and the buff is only granted once it actually arrives (see CastFloralGlow)
    [SerializeField] private GameObject floralGlowProjectilePrefab;

    [Header("Floral Glow Particles")]
    // whichever plant currently carries Floral Glow has these attached to its own projectiles
    // (see Projectile.Initialize/OnTriggerEnter2D) - read generically off the FloralGlowEffect
    // instance, so any plant's projectile can pick them up regardless of its own particle setup
    [SerializeField] private GameObject floralGlowFlyParticlePrefab;
    [SerializeField] private GameObject floralGlowHitParticlePrefab;
    public GameObject FloralGlowFlyParticlePrefab => floralGlowFlyParticlePrefab;
    public GameObject FloralGlowHitParticlePrefab => floralGlowHitParticlePrefab;

    // floral glow's on hit damage scaling, 25% base, +5% per level, 50% at max level
    public float FloralGlowDamageScaling =>
        (CData?.floralGlowBaseDamageScaling ?? 0.25f) + (CData?.floralGlowDamageScalingPerLevel ?? 0.05f) * effectivePath3Level;

    // Igniting Glow: Floral Glow's coordinated hit applies a Fire Primer like any other Fire
    // damage would - without it, that hit is tagged NoPrimer so it never does (see FloralGlowEffect)
    public bool IgnitingGlowActive => SkillTreeManager.HasUnlock(this, IgnitingGlowUnlock);

    private bool autoCastEnabled = false;
    private Tile autoCastTargetTile = null;
    private Plant _autoCastHighlighted;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    // skill tree node unlock ids
    public const string GuidingLightUnlock  = "calendula_guiding_light";
    public const string RadiantAuraUnlock   = "calendula_radiant_aura";
    public const string InstantSkillUnlock  = "calendula_instant_skill";
    public const string BorrowedLightUnlock = "calendula_borrowed_light";
    public const string IgnitingGlowUnlock  = "calendula_igniting_glow";

    private float _guidingLightTimer;
    private float _radiantAuraTimer;
    private float _borrowedLightTimer;
    private const float GuidingLightInterval   = 0.25f;
    private const float RadiantAuraInterval    = 0.5f;
    private const float BorrowedLightInterval  = 0.25f;
    // percent of max health healed per second - scaled by RadiantAuraInterval at each tick so
    // ticking more often (smoother health bar movement) doesn't change the actual heal rate
    private const float RadiantAuraHealPercent = 0.025f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();

        // LoadData already applied any skill tree path1LevelAdder/path2LevelAdder/
        // path3LevelAdder ("+1 Effective X Point" nodes) and recomputed
        // effectivePath1/2/3Level from them, so re-running these three hooks here bakes that
        // virtual level straight into attackDamage/attackRange/skillDuration/etc.
        OnPath1Upgrade(effectivePath1Level);
        OnPath2Upgrade(effectivePath2Level);
        OnPath3Upgrade(effectivePath3Level);

        Plant.OnPlantPlaced += HandlePlantPlaced;
        ApplyAuraToAllInRange();

        // free skill readiness on placement - deliberately bypasses UnlockPath3() (which spends
        // sun and adds to totalSunSpent) so this can't be abused for an inflated uproot refund
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }
    }

    private void HandlePlantPlaced(Plant plant)
    {
        if (!IsAlive) return;
        ApplyAuraToAllInRange();
    }

    // applied once on placement, on reaching Path2 max, or whenever any new plant appears on
    // the field (via Plant.OnPlantPlaced) — not re-scanned every tick. removal is handled
    // entirely by CalendulasEmberEffect itself (PlantAuraBuffEffect base)
    private void ApplyAuraToAllInRange()
    {
        if (!IsPath2Maxed) return;
        float fireDamageBonus = CData?.maxLevelFireDamageBonus ?? 0.06f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > lightEmissionRange) continue;
            plant.ApplyEffect(new CalendulasEmberEffect(plant, 1, this, lightEmissionRange, fireDamageBonus));
        }
    }

    // Borrowed Light: every plant currently carrying a Floral Glow cast by this Calendula acts as
    // a second illumination source, sharing its passive's max level bonus and Guiding Light/
    // Radiant Aura fork with anything standing in range of the target instead of Calendula herself
    private List<Plant> GetFloralGlowTargets()
    {
        List<Plant> targets = new List<Plant>();
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            FloralGlowEffect fg = plant.GetEffect<FloralGlowEffect>();
            if (fg != null && fg.source == this) targets.Add(plant);
        }
        return targets;
    }

    protected override bool ShowLight => DarknessManager.instance != null && (DarknessManager.instance.isDark || DarknessManager.instance.pitchBlack);
    public override bool ShowDarkCircle => false;

    public override void UpdateStats()
    {
        baseLightEmissionRange = baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier);
        coordinatedDamageAdder = IsPath1Maxed ? 0.15f : 0f;

        base.UpdateStats();
    }

    protected override void Update()
    {
        base.Update();

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();

        bool borrowedLight = SkillTreeManager.HasUnlock(this, BorrowedLightUnlock);

        // Guiding Light: periodically shreds Armor/Magic Armor on every insect currently
        // illuminated, refreshed often enough that it never lapses while they stay in range.
        // Borrowed Light extends the same shred to insects illuminated by a Floral Glow target
        if (SkillTreeManager.HasUnlock(this, GuidingLightUnlock))
        {
            _guidingLightTimer += Time.deltaTime;
            if (_guidingLightTimer >= GuidingLightInterval)
            {
                _guidingLightTimer = 0f;
                List<Insect> insects = new List<Insect>(Insect.allInsects);
                List<Plant> glowTargets = borrowedLight ? GetFloralGlowTargets() : null;

                foreach (Insect insect in insects)
                {
                    if (insect == null || !insect.IsAlive) continue;
                    if (Vector2.Distance(transform.position, insect.transform.position) > lightEmissionRange) continue;
                    insect.ApplyEffect(new GuidingLightEffect(insect, GuidingLightInterval * 2f, 1, this));
                }

                if (glowTargets != null)
                {
                    foreach (Plant glowTarget in glowTargets)
                    foreach (Insect insect in insects)
                    {
                        if (insect == null || !insect.IsAlive) continue;
                        if (Vector2.Distance(glowTarget.transform.position, insect.transform.position) > lightEmissionRange) continue;
                        insect.ApplyEffect(new GuidingLightEffect(insect, GuidingLightInterval * 2f, 1, this));
                    }
                }
            }
        }

        // Radiant Aura: periodically heals every OTHER plant currently illuminated. Borrowed Light
        // extends the same heal to plants illuminated by a Floral Glow target, deduplicated so a
        // plant lit by both this Calendula and a glow target only gets healed once per tick
        if (SkillTreeManager.HasUnlock(this, RadiantAuraUnlock))
        {
            _radiantAuraTimer += Time.deltaTime;
            if (_radiantAuraTimer >= RadiantAuraInterval)
            {
                _radiantAuraTimer = 0f;
                HashSet<Plant> healed = new HashSet<Plant>();
                foreach (Plant plant in Plant.allPlants)
                {
                    if (plant == null || !plant.IsAlive || plant == this) continue;
                    if (Vector2.Distance(transform.position, plant.transform.position) > lightEmissionRange) continue;
                    healed.Add(plant);
                    plant.Heal(plant.maxHealth * RadiantAuraHealPercent * RadiantAuraInterval, this);
                }

                if (borrowedLight)
                {
                    foreach (Plant glowTarget in GetFloralGlowTargets())
                    foreach (Plant plant in Plant.allPlants)
                    {
                        if (plant == null || !plant.IsAlive || plant == this || healed.Contains(plant)) continue;
                        if (Vector2.Distance(glowTarget.transform.position, plant.transform.position) > lightEmissionRange) continue;
                        healed.Add(plant);
                        plant.Heal(plant.maxHealth * RadiantAuraHealPercent * RadiantAuraInterval, this);
                    }
                }
            }
        }

        // Borrowed Light: independent of the Guiding Light/Radiant Aura fork, Calendula's Ember
        // (the Path2-max Fire Damage aura) also radiates from every current Floral Glow target
        if (borrowedLight && IsPath2Maxed)
        {
            _borrowedLightTimer += Time.deltaTime;
            if (_borrowedLightTimer >= BorrowedLightInterval)
            {
                _borrowedLightTimer = 0f;
                float fireDamageBonus = CData?.maxLevelFireDamageBonus ?? 0.06f;
                foreach (Plant glowTarget in GetFloralGlowTargets())
                foreach (Plant plant in Plant.allPlants)
                {
                    if (plant == null || !plant.IsAlive) continue;
                    if (Vector2.Distance(glowTarget.transform.position, plant.transform.position) > lightEmissionRange) continue;
                    plant.ApplyEffect(new CalendulasEmberEffect(plant, 1, this, lightEmissionRange, fireDamageBonus, glowTarget.transform));
                }
            }
        }

        if (autoCastEnabled)
        {
            // resolved live from the tile (not a pinned instance), so if the target plant dies
            // and gets revived, the auto-cast picks the new instance back up on its own
            Plant currentTarget = Plant.GetPlantOnTile(autoCastTargetTile);
            if (currentTarget != null && currentTarget.IsAlive && SkillReady && !HasForeignFloralGlow(currentTarget))
                CastFloralGlow(currentTarget, effectivePath3Level + 1);
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
    // sweep takes to expand from 0 to attackRange
    private const float FireBurstLifetime = 0.3f;

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

    // each insect beyond the first hit by one sweep costs 10% damage, down to a floor of 40% -
    // e.g. 1st insect = 100%, 2nd = 90%, 3rd = 80%, ... 7th+ = 40%. hit.Count at the moment an
    // insect is reached is exactly how many were already hit before it this sweep, so it doubles
    // as the falloff index with no separate counter needed
    private const float MultiTargetDamageReductionPerInsect = 0.1f;
    private const float MinMultiTargetDamageMultiplier = 0.4f;

    // the fire spreads outward from Calendula over FireBurstLifetime. every frame, ANY insect
    // currently within the growing radius is damaged — including one that wasn't even in range
    // when the attack fired but wanders into the expanding burst zone partway through. each
    // insect can only be hit once per attack
    // one sweep can hit many insects at once - past this many, further hits still land normally
    // but stop adding another overlapping copy of the hit sound on top of the rest
    private const int MaxHitSoundsPerSweep = 3;
    private static readonly DamageTag[] SweepTags       = { DamageTag.AoE, DamageTag.Attack };
    private static readonly DamageTag[] SweepTagsSilent = { DamageTag.AoE, DamageTag.Attack, DamageTag.SilentHit };

    private IEnumerator SweepAttackDamage(float damage, DamageType dmgType, ElementalType elemType)
    {
        HashSet<Insect> hit = new HashSet<Insect>();
        int soundsPlayed = 0;
        float elapsed = 0f;

        void DamageInsect(Insect insect)
        {
            float multiplier = Mathf.Max(MinMultiTargetDamageMultiplier, 1f - hit.Count * MultiTargetDamageReductionPerInsect);
            hit.Add(insect);
            bool playSound = soundsPlayed < MaxHitSoundsPerSweep;
            if (playSound) soundsPlayed++;
            insect.Damage(damage * multiplier, dmgType, elemType, this, true, playSound ? SweepTags : SweepTagsSilent);
        }

        while (elapsed < FireBurstLifetime)
        {
            float currentRadius = (elapsed / FireBurstLifetime) * attackRange;
            foreach (Insect insect in new List<Insect>(Insect.allInsects))
            {
                if (insect == null || !insect.IsAlive || hit.Contains(insect)) continue;
                if (Vector3.Distance(transform.position, insect.transform.position) > currentRadius) continue;
                DamageInsect(insect);
            }
            yield return null;
            elapsed += Time.deltaTime;
        }

        // catch anyone the wavefront should have reached by now but a frame gap missed
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || hit.Contains(insect)) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) > attackRange) continue;
            DamageInsect(insect);
        }
    }

    // same fire burst visual used by the attack
    private void SpawnFireBurst(Vector3 position, float radius, float particleScale = 1f)
    {
        if (fireBurstPrefab == null) return;
        GameObject burst = Instantiate(fireBurstPrefab, position, Quaternion.identity);
        ParticleSystem ps = burst.GetComponent<ParticleSystem>();
        if (ps == null) return;

        if (particleScale != 1f)
        {
            var emission = ps.emission;
            int burstCount = emission.burstCount;
            if (burstCount > 0)
            {
                ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[burstCount];
                emission.GetBursts(bursts);
                for (int i = 0; i < bursts.Length; i++)
                {
                    ParticleSystem.Burst b = bursts[i];
                    b.count = new ParticleSystem.MinMaxCurve(b.count.constant * particleScale);
                    bursts[i] = b;
                }
                emission.SetBursts(bursts);
            }
        }

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

        // can't target a plant that's already glowing from another Calendula - the click simply
        // doesn't go through, same as any other invalid target, and targeting stays open so the
        // player can immediately pick a different plant without re-clicking the skill
        if (HasForeignFloralGlow(targetPlant))
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
            return;
        }

        CastFloralGlow(targetPlant, effectivePath3Level + 1);
    }

    private bool HasForeignFloralGlow(Plant plant)
    {
        FloralGlowEffect existing = plant.GetEffect<FloralGlowEffect>();
        return existing != null && existing.source != this;
    }

    // shared by the manual skill cast and auto cast, does not reopen targeting on its own. the
    // buff is no longer granted here directly - a projectile is fired at the target instead, and
    // FloralGlowProjectile.Arrive() is what actually applies it once it physically gets there
    private void CastFloralGlow(Plant targetPlant, int level)
    {
        skillCooldownTimer = skillCooldown;

        if (floralGlowProjectilePrefab == null)
        {
            // safety fallback so the skill still does something if the delivery prefab hasn't
            // been assigned yet, rather than silently consuming the cooldown for nothing
            targetPlant.ApplyEffect(new FloralGlowEffect(targetPlant, skillDuration, level, this, this));
            return;
        }

        GameObject obj = Instantiate(floralGlowProjectilePrefab, transform.position, Quaternion.identity);
        FloralGlowProjectile proj = obj.GetComponent<FloralGlowProjectile>();
        proj?.Initialize(this, targetPlant, skillDuration, level, CData?.floralGlowProjectileSpeed ?? 8f);
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
        => $"Releases flaming petals dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects within range. Each insect beyond the first hit reduces damage by <color=green><b>10%</b></color>, down to a minimum of <color=green><b>40%</b></color>.";

    public override string GetPassiveDescription() =>
        $"Illuminate the surrounding area allowing plants to see insects.\n\n" +
        $"<color=green><b>Base Illumination Range</b></color> is equal to <color=green><b>Attack Range</b></color>.";

    public override string GetSkillDesription() =>
        $"Target a plant anywhere on the field, sending a petal to it that grants <color=orange>Floral Glow</color> for <color=green><b>{skillDuration:F0}s</b></color> on arrival. The plant's projectile attacks deal an additional <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage * FloralGlowDamageScaling:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} on hit. Emits light equal to <b><color=orange>Calendula</color></b>'s <color=green><b>Base Illumination Range</b></color>.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl   = CData?.path1AttackDamagePerLevel ?? 5f;
        float firepl = CData?.path1FireDamagePerLevel    ?? 0.05f;
        string desc = details
            ? $"Releases flaming petals dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects within range. Each insect beyond the first hit reduces damage by <color=green><b>10%</b></color>, down to a minimum of <color=green><b>40%</b></color>."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=orange><b>Fire Damage</b></color> by <color=green><b>{firepl * 100f:F0}%</b></color> per level. [<color=green><b>+{firepl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Increase <color=#6495ED><b>Coordinated Damage</b></color> by <color=green><b>15%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float rangepl = CData?.path2AttackRangePerLevel ?? 0.175f;
        float fireDamageBonus = CData?.maxLevelFireDamageBonus ?? 0.06f;
        string p2Bonus = $"Plants within illumination range gain <color=orange><b>Calendula's Ember</b></color>, increasing <color=orange><b>Fire Damage</b></color> by <color=green><b>{fireDamageBonus * 100f:F0}%</b></color>.";
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
        float maxLevelASBonus = CData?.floralGlowMaxLevelAttackSpeedBonus ?? 0.15f;
        string desc = details
            ? $"Target a plant anywhere on the field, sending a petal to it that grants <color=orange>Floral Glow</color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds on arrival. The plant's projectile attacks deal an additional <color=green><b>[({dmgScalingBase * 100f:F0}%) + ({dmgScalingPerLevel * 100f:F0}%/Lvl.) Attack Damage + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.DamageTypeLabel(damageType)} on hit. Emits light equal to <b><color=orange>Calendula</color></b>'s <color=green><b>Base Illumination Range</b></color>."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Attack Damage</b></color> scaling by <color=green><b>{dmgScalingPerLevel * 100f:F0}%</b></color> per level. [<color=green><b>+{dmgScalingPerLevel * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"<color=orange><b>Floral Glow</b></color> now also increases <color=green><b>Attack Speed</b></color> by <color=green><b>{maxLevelASBonus * 100f:F0}%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
