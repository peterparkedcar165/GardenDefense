using UnityEngine;
using System.Collections.Generic;

public class Tansy : Aura
{
    private TansyData TData => data as TansyData;
    [SerializeField] private GameObject orbitProjectilePrefab;

    // base attack: always-present orbiting projectile(s), see TansyProjectile. base count is 2,
    // +1 (3 total) at Path1 max level, always evenly spaced (360/count degrees apart)
    private readonly List<TansyProjectile> _orbitProjectiles = new List<TansyProjectile>();
    private int AttackProjectileCount => IsPath1Maxed ? 3 : 2;

    // Floral Glow: the single projectile that currently orbits whichever plant has Floral Glow,
    // plus its Path3-max second projectile. _floralGlowBearer tracks who it's orbiting (or heading
    // toward) so Update() can notice when that plant dies or the effect lapses and call it home.
    // _primaryQueued/_secondaryQueued track whether that slot's projectile was consumed and is
    // sitting in the target's abstract wait queue (see OnFloralGlowProjectileQueued) - without
    // this, UpdateFloralGlowProjectiles can't tell "queued, waiting for a slot" apart from "never
    // spawned" and would otherwise spawn a brand new one every single frame forever
    private TansyProjectile _floralGlowProjectile;
    private TansyProjectile _floralGlowProjectileSecondary;
    private bool _primaryQueued;
    private bool _secondaryQueued;
    private Plant _floralGlowBearer;

    public bool NurturingGlowActive => SkillTreeManager.HasUnlock(this, NurturingGlowUnlock);

    private bool autoCastEnabled = false;
    private Tile autoCastTargetTile = null;
    private Plant _autoCastHighlighted;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    // skill tree node unlock ids
    public const string GuidingLightUnlock  = "tansy_guiding_light";
    public const string RadiantAuraUnlock   = "tansy_radiant_aura";
    public const string InstantSkillUnlock  = "tansy_instant_skill";
    public const string BorrowedLightUnlock = "tansy_borrowed_light";
    public const string NurturingGlowUnlock = "tansy_nurturing_glow";

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

        EnsureOrbitProjectileCount();
    }

    private TansyProjectile SpawnOrbitProjectile()
    {
        if (orbitProjectilePrefab == null) return null;
        GameObject obj = Instantiate(orbitProjectilePrefab, transform.position, Quaternion.identity);
        TansyProjectile proj = obj.GetComponent<TansyProjectile>();
        proj?.InitializeOrbitingTansy(this);
        proj?.SetOrbitRadius(ClampedRadiusFor(this), snapImmediately: true);
        return proj;
    }

    // adds/removes one projectile at a time to reach the desired count (Path1 reaching/losing max
    // level) - TansyProjectile's own registry handles re-spacing the whole set evenly every
    // time one is added or removed, including anyone else's Floral Glow projectiles also orbiting
    // this Tansy, so there's no need to tear down and respawn everything here any more
    private void EnsureOrbitProjectileCount()
    {
        int desired = AttackProjectileCount;
        while (_orbitProjectiles.Count < desired)
            _orbitProjectiles.Add(SpawnOrbitProjectile());
        while (_orbitProjectiles.Count > desired)
        {
            int last = _orbitProjectiles.Count - 1;
            if (_orbitProjectiles[last] != null) Destroy(_orbitProjectiles[last].gameObject);
            _orbitProjectiles.RemoveAt(last);
        }
    }

    // a plant's own raw toggled value, clamped live against ITS OWN current Attack Range so a
    // skill tree respec (or attackRange simply changing) can't leave it stuck above MaxOrbitRadius
    // without the UI being open to notice. used for whichever plant a projectile is actually
    // orbiting, not necessarily Tansy herself
    private static int ClampedRadiusFor(Plant plant) =>
        plant != null ? Mathf.Clamp(plant.orbitRadius, 1, plant.MaxOrbitRadius) : 1;

    // Path2 base effect: each integer of orbit radius above 1 increases damage by this much (base
    // 6%, +1%/level) - a larger radius means slower revolutions (fewer hits/sec, see
    // TansyProjectile) but each hit deals more, a risk/reward tradeoff tied to the radius toggle.
    // applies to both the attack and the skill below, using whichever plant is actually being
    // orbited's own radius setting - so the damage bonus always matches the radius it's visibly
    // orbiting at, never Tansy's own setting when she's not the one being orbited
    public float DistanceBonusDamagePerRadius =>
        (TData?.baseDistanceBonusDamagePerRadius ?? 0.06f) + (TData?.path2DistanceBonusDamagePerLevel ?? 0.01f) * effectivePath2Level;

    private float DistanceBonusMultiplier(Plant orbitedPlant) =>
        1f + DistanceBonusDamagePerRadius * (ClampedRadiusFor(orbitedPlant ?? this) - 1);

    // what Tansy's own attack projectile(s) deal on hit - attackDamage plus the live distance
    // bonus above. orbitedPlant defaults to Tansy herself (the attack always orbits her), but
    // tooltips may omit it to just preview off her own current radius setting
    public float EffectiveAttackDamage(Plant orbitedPlant = null) => attackDamage * DistanceBonusMultiplier(orbitedPlant);

    // Floral Glow's own independent damage, separate from the attack: a flat base plus a flat
    // per-level amount (Path3), also scaled by the same distance bonus above - orbitedPlant should
    // be the actual bearer at hit time, or omitted for a tooltip preview off Tansy's own radius
    public float EffectiveSkillDamage(Plant orbitedPlant = null) =>
        ((TData?.floralGlowBaseDamage ?? 20f) + (TData?.floralGlowDamagePerLevel ?? 5f) * effectivePath3Level) * DistanceBonusMultiplier(orbitedPlant);

    // tagged Coordinated (see TansyProjectile), so Entity.Damage() applies this multiplier
    // automatically at hit time - this is just the same number for display in tooltips
    public float EffectiveSkillDamageCoordinated(Plant orbitedPlant = null) => EffectiveSkillDamage(orbitedPlant) * (1f + coordinatedDamage);

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

    // Borrowed Light: every plant currently carrying a Floral Glow cast by this Tansy acts as
    // a second illumination source, sharing its passive's max level bonus and Guiding Light/
    // Radiant Aura fork with anything standing in range of the target instead of Tansy herself
    private List<Plant> GetFloralGlowTargets()
    {
        List<Plant> targets = new List<Plant>();
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            TansyGlowEffect fg = plant.GetEffect<TansyGlowEffect>();
            if (fg != null && fg.source == this) targets.Add(plant);
        }
        return targets;
    }

    protected override bool ShowLight => DarknessManager.instance != null && (DarknessManager.instance.isDark || DarknessManager.instance.pitchBlack);
    public override bool ShowDarkCircle => false;

    public override void UpdateStats()
    {
        baseLightEmissionRange = baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier);

        base.UpdateStats();
    }

    protected override void Update()
    {
        base.Update();

        // Path1 max level: a third orbit projectile, all re-spaced evenly. spawned/removed lazily
        // here so a respec or in-progress level up is picked up without any extra event plumbing
        EnsureOrbitProjectileCount();

        UpdateFloralGlowProjectiles();

        // pushes each orbit's actual center's own radius toggle (clamped live) to its projectiles
        // every frame - cheap, and means a live toggle (by Tansy for her own attack, or by the
        // bearer for Floral Glow) or an attackRange change just takes effect on its own
        int ownRadius = ClampedRadiusFor(this);
        foreach (TansyProjectile proj in _orbitProjectiles)
            proj?.SetOrbitRadius(ownRadius);
        int bearerRadius = ClampedRadiusFor(_floralGlowBearer);
        _floralGlowProjectile?.SetOrbitRadius(bearerRadius);
        _floralGlowProjectileSecondary?.SetOrbitRadius(bearerRadius);

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
        // plant lit by both this Tansy and a glow target only gets healed once per tick
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

        // Borrowed Light: independent of the Guiding Light/Radiant Aura fork, Tansy's Light
        // (the Path2-max attack speed aura) also radiates from every current Floral Glow target
        if (borrowedLight && IsPath2Maxed)
        {
            _borrowedLightTimer += Time.deltaTime;
            if (_borrowedLightTimer >= BorrowedLightInterval)
            {
                _borrowedLightTimer = 0f;
                foreach (Plant glowTarget in GetFloralGlowTargets())
                foreach (Plant plant in Plant.allPlants)
                {
                    if (plant == null || !plant.IsAlive) continue;
                    if (Vector2.Distance(glowTarget.transform.position, plant.transform.position) > lightEmissionRange) continue;
                    plant.ApplyEffect(new CalendulasLightEffect(plant, 1, this, lightEmissionRange, 0.15f, glowTarget.transform));
                }
            }
        }

        if (autoCastEnabled)
        {
            // resolved live from the tile (not a pinned instance), so if the target plant dies
            // and gets revived, the auto-cast picks the new instance back up on its own
            Plant currentTarget = Plant.GetPlantOnTile(autoCastTargetTile);
            if (currentTarget != null && currentTarget.IsAlive && SkillReady)
            {
                int myLevel = effectivePath3Level + 1;
                // filtered to THIS Tansy's own instance (see OnTargetConfirmed) - another
                // Tansy's instance on the same target stacks additively, it never gets waited out
                TansyGlowEffect existing = currentTarget.GetEffect<TansyGlowEffect>(this);
                if (existing == null || existing.level <= myLevel)
                    CastFloralGlow(currentTarget);
            }
        }

        UpdateAutoCastHighlight();
    }

    // while this Tansy is selected and auto casting, highlight its locked target in yellow
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

        foreach (TansyProjectile proj in _orbitProjectiles)
            if (proj != null) Destroy(proj.gameObject);
        if (_floralGlowProjectile != null) Destroy(_floralGlowProjectile.gameObject);
        if (_floralGlowProjectileSecondary != null) Destroy(_floralGlowProjectileSecondary.gameObject);
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
        // can't lock auto-cast onto herself - silently ignore and keep waiting for a real target,
        // as if nothing was clicked
        if (targetPlant == this) { SkillTargetingManager.instance.BeginPlantTargeting(OnAutoCastTargetConfirmed, this); return; }
        autoCastEnabled = true;
        autoCastTargetTile = targetPlant.occupiedTile;
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
    }

    private void OnTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return;
        // can't cast Floral Glow on herself - silently ignore and keep targeting active, as if
        // nothing was clicked (no cooldown spent, no retry message, just wait for a real target)
        if (targetPlant == this) { SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this); return; }
        int myLevel = effectivePath3Level + 1;
        // filtered to THIS Tansy's own instance - Floral Glow projectiles from different
        // Tansys (or a target's own base-attack orbit, if it's a Tansy) stack additively,
        // so a stronger instance from someone else must never block this cast from landing too
        TansyGlowEffect existing = targetPlant.GetEffect<TansyGlowEffect>(this);
        if (existing != null && existing.level > myLevel)
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
            return;
        }
        CastFloralGlow(targetPlant);
    }

    // shared by the manual skill cast and auto cast, does not reopen targeting on its own. the
    // actual Floral Glow buff is NOT granted here - only once the projectile physically reaches
    // the target and claims an orbit slot there (see GrantTansyGlowEffect), so the level it ends
    // up applied at is whatever effectivePath3Level is at that later moment, not at cast time
    private void CastFloralGlow(Plant targetPlant)
    {
        skillCooldownTimer = skillCooldown;
        RetargetFloralGlowProjectiles(targetPlant);
    }

    // moves Floral Glow's projectile(s) onto their new bearer: spawned fresh the first time this
    // Tansy ever casts the skill, or simply redirected in flight/orbit if it's being recast on
    // a different plant - there is only ever one bearer (and its projectile(s)) at a time
    private void RetargetFloralGlowProjectiles(Plant targetPlant)
    {
        if (_floralGlowProjectile == null)
            _floralGlowProjectile = SpawnFloralGlowProjectile(targetPlant);
        else
            _floralGlowProjectile.TravelTo(targetPlant);

        if (!IsPath3Maxed) return;
        if (_floralGlowProjectileSecondary == null)
            _floralGlowProjectileSecondary = SpawnFloralGlowProjectile(targetPlant);
        else
            _floralGlowProjectileSecondary.TravelTo(targetPlant);
    }

    private TansyProjectile SpawnFloralGlowProjectile(Plant target)
    {
        if (orbitProjectilePrefab == null) return null;
        GameObject obj = Instantiate(orbitProjectilePrefab, transform.position, Quaternion.identity);
        TansyProjectile proj = obj.GetComponent<TansyProjectile>();
        proj?.InitializeAsFloralGlow(this, target);
        proj?.SetOrbitRadius(ClampedRadiusFor(target), snapImmediately: true);
        return proj;
    }

    // called by TansyProjectile the instant it actually claims an orbit slot on target - this
    // is the only moment the Floral Glow buff is granted, never at cast time, since the projectile
    // may still be traveling (or sitting in a full target's queue) for a while after the cast
    public void GrantTansyGlowEffect(Plant target)
    {
        if (target == null || !target.IsAlive) return;
        // already granted - the primary and secondary (Path3 max) projectiles travel in parallel
        // and can both successfully claim a slot moments apart, which would otherwise re-apply
        // (and re-trigger OnApply/the status popup) a second time for no reason
        if (_floralGlowBearer == target) return;
        _floralGlowBearer = target;
        int myLevel = effectivePath3Level + 1;
        target.ApplyEffect(new TansyGlowEffect(target, skillDuration, myLevel, this, this));
    }

    // called by a projectile right before it's consumed/destroyed because the target's orbit is
    // full, so this Tansy knows which slot (primary/secondary) is now sitting in that target's
    // abstract wait queue instead of just "missing" - critical so UpdateFloralGlowProjectiles can
    // tell that apart from "never spawned" and doesn't spawn a new one every frame forever
    public void OnFloralGlowProjectileQueued(TansyProjectile proj)
    {
        if (proj == _floralGlowProjectile) _primaryQueued = true;
        else if (proj == _floralGlowProjectileSecondary) _secondaryQueued = true;
    }

    // a slot on target just freed up and was reserved for this Tansy's queued request - respawn
    // a fresh projectile (the original was consumed/destroyed when it first found the target full)
    // and send it there, filling whichever of the primary/secondary slots was the one queued
    public void SpawnReservedFloralGlowProjectile(Plant target)
    {
        TansyProjectile proj = SpawnFloralGlowProjectile(target);
        proj?.ReserveSlot();
        if (_primaryQueued) { _primaryQueued = false; _floralGlowProjectile = proj; }
        else { _secondaryQueued = false; _floralGlowProjectileSecondary = proj; }
    }

    // polls the current bearer every frame: if it died, or this Tansy's own instance of Floral
    // Glow lapsed without a recast landing elsewhere, the projectile(s) fly home and self-destruct.
    // also grants/revokes the Path3 max level second projectile lazily, same as Path1's
    private void UpdateFloralGlowProjectiles()
    {
        if (_floralGlowBearer != null)
        {
            // filtered to THIS Tansy's own instance - with multiple sources now able to stack
            // additively on the same target, the unfiltered GetEffect() could return a DIFFERENT
            // Tansy's instance and wrongly conclude this one expired while it's still active
            TansyGlowEffect active = _floralGlowBearer.IsAlive ? _floralGlowBearer.GetEffect<TansyGlowEffect>(this) : null;
            if (active == null)
            {
                _floralGlowBearer = null;
                _floralGlowProjectile?.ReturnHomeAndDestroy();
                _floralGlowProjectile = null;
                _floralGlowProjectileSecondary?.ReturnHomeAndDestroy();
                _floralGlowProjectileSecondary = null;
                // this Tansy no longer cares about either slot - any outstanding queue entries
                // for them would otherwise sit around and get wastefully promoted later for nothing
                _primaryQueued = false;
                _secondaryQueued = false;
            }
        }

        if (_floralGlowBearer == null) return;

        if (IsPath3Maxed && _floralGlowProjectileSecondary == null && !_secondaryQueued)
            _floralGlowProjectileSecondary = SpawnFloralGlowProjectile(_floralGlowBearer);
        else if (!IsPath3Maxed && _floralGlowProjectileSecondary != null)
        {
            Destroy(_floralGlowProjectileSecondary.gameObject);
            _floralGlowProjectileSecondary = null;
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (TData?.path1AttackDamagePerLevel ?? 5f)  * level;
        RecomputeAttackRange();
    }

    public override void OnPath2Upgrade(int level)
    {
        RecomputeAttackRange();
        ApplyAuraToAllInRange();
    }

    // attack range is now fed by both Path1 and Path2 - recomputed from both live effective levels
    // whenever either one upgrades, so neither path's contribution can clobber the other's
    private void RecomputeAttackRange() =>
        baseAttackRange = data.baseAttackRange
                         + (TData?.path1AttackRangePerLevel ?? 0.1f)   * effectivePath1Level
                         + (TData?.path2AttackRangePerLevel ?? 0.175f) * effectivePath2Level;

    public override void OnPath3Unlock()
    {
        skillCooldownTimer = 0f;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (TData?.path3SkillDurationPerLevel ?? 2f) * level;
    }

    public override string GetName() => "<b><color=orange>Tansy</color></b>";
    public override string GetDescription() => $"The {GetName()} is circled by an orbiting petal projectile and can summon one to infuse allies with fire energy.";
    public override string GetPath1Name() => "Petals";
    public override string GetPath2Name() => "Illuminate";
    public override string GetPath3Name() => "Floral Glow";

    public override string GetAttackDescription()
        => $"Two orbiting petal projectiles, evenly spaced, each deal <color={PlantData.ElementalColor(elementalType)}><b>{EffectiveAttackDamage():F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to any insect they pass through.";

    public override string GetPassiveDescription() =>
        $"Illuminate the surrounding area allowing plants to see insects.\n\n" +
        $"<color=green><b>Base Illumination Range</b></color> is equal to <color=green><b>Attack Range</b></color>.\n\n" +
        $"The radius of the orbital may be toggled from <color=green><b>1</b></color> to <color=green><b>{MaxOrbitRadius}</b></color>.\n\n" +
        $"For each integer of radius above 1, increase <color=orange><b>Attack</b></color> and <color=orange><b>Skill</b></color> damage by <color=green><b>{DistanceBonusDamagePerRadius * 100f:F0}%</b></color>.\n\n" +
        $"Each insect an orbiting petal (<color=orange><b>Attack</b></color> or <color=orange><b>Skill</b></color>) hits reduces its damage by <color=green><b>10%</b></color>, down to a minimum of <color=green><b>50%</b></color> - resetting back to full the moment it completes a lap.";

    public override string GetSkillDesription() =>
        $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>{skillDuration:F0}s</b></color>, summoning an orbiting petal projectile around it that deals <color={PlantData.ElementalColor(elementalType)}><b>{EffectiveSkillDamageCoordinated():F0}</b></color> {PlantData.DamageTypeLabel(damageType)}, sourced from the {GetName()}, to anything it passes through. Emits light equal to <b><color=orange>Tansy</color></b>'s <color=green><b>Base Illumination Range</b></color>.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl    = TData?.path1AttackDamagePerLevel ?? 5f;
        float rangepl = TData?.path1AttackRangePerLevel  ?? 0.1f;
        string desc = details
            ? $"Two orbiting petal projectiles, evenly spaced, each deal <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} to any insect they pass through. Their orbit radius is a fixed, togglable value from <color=green><b>1</b></color> up to <color=green><b>{MaxOrbitRadius}</b></color> (Attack Range, rounded down), completing <color=green><b>[100% Attack Speed]</b></color> revolutions per second regardless of radius - a wider radius means a faster-moving projectile covering more ground per lap, not slower revolutions."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "A third petal projectile joins the orbit, all three staying evenly spaced.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float rangepl = TData?.path2AttackRangePerLevel ?? 0.175f;
        float distpl  = TData?.path2DistanceBonusDamagePerLevel ?? 0.01f;
        string p2Bonus = "Plants within illumination range gain <color=orange><b>Tansy's Light</b></color>, increasing <color=green><b>Attack Speed</b></color> by <color=green><b>15%</b></color>.";
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"Increase <color=green><b>Distance Bonus Damage</b></color> by <color=green><b>{distpl * 100f:F0}%</b></color> per level. [<color=green><b>+{distpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, p2Bonus)}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl = TData?.path3SkillDurationPerLevel ?? 2f;
        float dmgpl = TData?.floralGlowDamagePerLevel ?? 5f;
        string desc = details
            ? $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds, summoning an orbiting petal projectile around it that deals <color={PlantData.ElementalColor(elementalType)}><b>[({TData?.floralGlowBaseDamage ?? 20f:F0}) + ({dmgpl:F0}/Lvl.)]</b></color> {PlantData.DamageTypeLabel(damageType)}, sourced from the {GetName()}, to anything it passes through - its orbit speed is driven by the <b>bearer's</b> Attack Speed instead of Tansy's. Emits light equal to <b><color=orange>Tansy</color></b>'s <color=green><b>Base Illumination Range</b></color>."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Damage</b></color> by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "A second petal projectile orbits the bearer opposite the first, equidistant at all times.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
