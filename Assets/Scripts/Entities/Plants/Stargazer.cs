using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class Stargazer : Aura
{
    // cone edge indicators (at +/- half the cone angle) -- disabled for now, may re-add later
    // [SerializeField] private SpriteRenderer facingLineLeft;
    // [SerializeField] private SpriteRenderer facingLineRight;
    [SerializeField] private GameObject fireWavePrefab;
    [SerializeField] private GameObject skillIndicatorPrefab; // beam aim indicator for the ultimate
    [SerializeField] private ParticleSystem fireConeParticles; // continuous cone of fire while attacking
    [SerializeField] private float fireConeTravelTime = 0.25f; // seconds for a particle to cross the attack range (lower = faster, shorter)

    private StargazerData SData => data as StargazerData;
    private Vector2 _facingDir = Vector2.right;
    private GameObject _skillIndicatorInstance;

    private const float ScorcherDuration = 12f;
    private const float MelterDuration   = 12f;

    private float ConeAngle              => SData?.coneAngle ?? 60f;
    private float FlammableBonusPerStack => SData?.flammableBonusPerStack ?? 0.08f;
    private int   FlammableMaxStacks     => SData?.flammableMaxStacks ?? 5;
    private float FlammableDuration       => SData?.flammableDuration ?? 4f;
    private int   StacksPerHit           => (SData?.baseStacksPerHit ?? 1) + Mathf.RoundToInt((SData?.path2StacksPerLevel ?? 1) * effectivePath2Level);
    private float BurnDurationBonus       => (SData?.baseBurnDurationBonus ?? 0.5f) + (SData?.path2BurnDurationPerLevel ?? 0.1f) * effectivePath2Level;

    private float SkillLength       => SData?.skillLength ?? 50f;
    private float SkillWaveWidth    => (SData?.skillWaveWidth ?? 4f) + (SData?.path3WaveWidthPerLevel ?? 0.5f) * effectivePath3Level;
    private float SkillBurnMultiplier => (SData?.skillBurnMultiplier ?? 2f) + (SData?.path3BurnMultiplierPerLevel ?? 0.1f) * effectivePath3Level;
    private int   SkillFlammableStacks => Mathf.RoundToInt((SData?.skillFlammableStacks ?? 2) + (SData?.path3FlammableStacksPerLevel ?? 0.5f) * effectivePath3Level);
    private float SkillDamage    => (SData?.skillBaseDamage ?? 200f) + (SData?.path3SkillDamagePerLevel ?? 40f) * effectivePath3Level + skillDamageMultiplier * magicPower;
    private float SkillDelay     => SData?.skillDelay ?? 1f;

    // this plant aims its cone using the standard targeting modes
    public override bool UsesTargeting => true;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        Entity.OnEffectApplied += OnAnyEffectApplied;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Entity.OnEffectApplied -= OnAnyEffectApplied;
    }

    private void OnAnyEffectApplied(StatusEffect effect)
    {
        if (!IsPath2Maxed) return;
        if (!(effect is BurnEffect) || effect.source != this) return;
        MelterEffect existing = GetEffect<MelterEffect>();
        int newStacks = Mathf.Min((existing?.level ?? 0) + 1, MelterEffect.MaxStacks);
        ApplyEffect(new MelterEffect(this, MelterDuration, newStacks, this));
    }

    public override void UpdateStats()
    {
        if (IsPath1Maxed) attackDamageTotalMultiplier *= 0.67f;
        base.UpdateStats();
        if (IsPath1Maxed) attackDamageTotalMultiplier /= 0.67f;
        burnDurationBonus = BurnDurationBonus;
    }

    protected override void Update()
    {
        base.Update();
        UpdateFacing();
        UpdateSkillIndicator();

        bool canAttack = FacingTarget() != null && !IsStunned && !IsChanneling;   // webbed/stunned/channeling can't spit fire
        UpdateFireCone(canAttack);   // stream fire while a target is in the cone

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (canAttack)   // only fires when there's a visible target in range (respects darkness)
            Attack();
    }

    private void UpdateFacing()
    {
        Insect aim = FacingTarget();
        if (aim != null)
            _facingDir = ((Vector2)aim.transform.position - (Vector2)transform.position).normalized;

        // cone edge indicators disabled for now, may re-add later
        // if (_facingDir.sqrMagnitude < 0.0001f) return;
        // // the two lines mark the cone's edges, at +/- half the cone angle from the aim direction
        // float half = ConeAngle * 0.5f;
        // OrientLine(facingLineLeft,  Rotate(_facingDir,  half));
        // OrientLine(facingLineRight, Rotate(_facingDir, -half));
    }

    // rectangle line: width 0.15 (X), height (Y) stretched to the cone's reach.
    // -90 so the sprite's +Y axis points along the given direction
    // private void OrientLine(SpriteRenderer line, Vector2 dir)
    // {
    //     if (line == null) return;
    //     float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
    //     line.transform.position = transform.position + (Vector3)(dir * attackRange * 0.5f);
    //     line.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    //     line.transform.localScale = new Vector3(0.05f, attackRange, 1f);
    // }

    // private static Vector2 Rotate(Vector2 v, float degrees)
    // {
    //     float rad = degrees * Mathf.Deg2Rad;
    //     float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
    //     return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    // }

    // the cone tracks the targeting-mode pick among insects within range only
    private Insect FacingTarget()
    {
        GameObject go;
        switch (targeting)
        {
            case TARGETING.Nearest: go = FindNearest(Insect.allInsects); break;
            case TARGETING.Last:    go = FindLast(Insect.allInsects);    break;
            default:                go = FindFirst(Insect.allInsects);   break;
        }
        return go != null ? go.GetComponent<Insect>() : null;
    }

    protected override void Attack()
    {
        base.Attack();   // resets the attack timer

        // _facingDir is kept current by UpdateFacing (tracks the target even out of range)
        float halfAngle = ConeAngle * 0.5f;
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            Vector2 to = (Vector2)insect.transform.position - (Vector2)transform.position;
            if (to.magnitude > attackRange) continue;
            if (Vector2.Angle(_facingDir, to) > halfAngle) continue;

            insect.Damage(attackDamage, damageType, elementalType, this, true,
                new DamageTag[] { DamageTag.AoE, DamageTag.Attack });
            ApplyFlammable(insect);
        }

        if (IsPath1Maxed)
        {
            ScorcherEffect existing = GetEffect<ScorcherEffect>();
            int newStacks = Mathf.Min((existing?.level ?? 0) + 1, ScorcherEffect.MaxStacks);
            ApplyEffect(new ScorcherEffect(this, ScorcherDuration, newStacks, this));
        }
    }

    private const float FireConeRate = 250f;   // particles per second while firing
    private const float FireConeReachMin = 1.1f; // each particle overshoots the range by a random
    private const float FireConeReachMax = 1.2f; // factor in this interval for a ragged flame edge
    private float _fireEmitAccumulator;

    // emits the fire cone directly in 2D: each particle gets a velocity at a random angle within
    // the cone around the target direction. this sidesteps the 3D Shape entirely, so the fan is
    // always flat, symmetric, evenly spread, and centered on the target. the prefab's Shape/Arc/
    // rotation no longer matter; Emission Rate over Time should be 0 (we emit manually here)
    private void UpdateFireCone(bool active)
    {
        if (fireConeParticles == null) return;

        // disable automatic emission so the shape never spawns stationary particles on the plant.
        // all fire comes from the manual Emit() calls below, which ignore this module
        ParticleSystem.EmissionModule emission = fireConeParticles.emission;
        emission.enabled = false;

        // reach: a particle crosses attackRange (plus a random overshoot so it fades past the edge)
        // in fireConeTravelTime seconds. lifetime is fixed, speed is randomized per particle below
        float travel = fireConeTravelTime > 0f ? fireConeTravelTime : 0.25f;
        ParticleSystem.MainModule main = fireConeParticles.main;
        main.startLifetime = travel;
        main.startSpeed    = 0f;   // velocity is assigned per particle below

        if (!active) { _fireEmitAccumulator = 0f; return; }

        // accumulate fractional emissions so the rate is smooth across frames
        _fireEmitAccumulator += FireConeRate * Time.deltaTime;
        int count = Mathf.FloorToInt(_fireEmitAccumulator);
        _fireEmitAccumulator -= count;

        float baseAngle = Mathf.Atan2(_facingDir.y, _facingDir.x);
        float half      = ConeAngle * 0.5f * Mathf.Deg2Rad;

        for (int i = 0; i < count; i++)
        {
            float a = baseAngle + Random.Range(-half, half);
            float speed = attackRange * Random.Range(FireConeReachMin, FireConeReachMax) / travel;
            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
            {
                velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * speed
            };
            fireConeParticles.Emit(ep, 1);
        }
    }

    private void ApplyFlammable(Insect insect) => AddFlammable(insect, StacksPerHit);

    // adds `amount` Flammable stacks to the target (capped), refreshing duration.
    // public so the fire wave (skill) can apply stacks too
    public void AddFlammable(Insect insect, int amount)
    {
        if (insect == null || !insect.IsAlive || amount <= 0) return;
        FlammableEffect existing = insect.GetEffect<FlammableEffect>();
        int stacks = Mathf.Min((existing?.level ?? 0) + amount, FlammableMaxStacks);
        insect.ApplyEffect(new FlammableEffect(insect, FlammableDuration, stacks, this, FlammableBonusPerStack));
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        if (_skillIndicatorInstance != null) return;
        SkillTargetingManager.instance.BeginTargeting(0f, OnTargetConfirmed);
        if (skillIndicatorPrefab != null)
        {
            _skillIndicatorInstance = Instantiate(skillIndicatorPrefab, transform.position, Quaternion.identity);
            SpriteRenderer sr = _skillIndicatorInstance.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }
    }

    // a beam indicator pivoted at the plant, spanning the whole screen (plant in the middle),
    // aimed at the mouse, same idea as WindGust/Blizzard but centered on the plant
    private void UpdateSkillIndicator()
    {
        if (_skillIndicatorInstance == null) return;

        if (!SkillTargetingManager.instance.IsTargeting)
        {
            Destroy(_skillIndicatorInstance);
            _skillIndicatorInstance = null;
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld  = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        mouseWorld.z = 0f;
        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = _facingDir;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        _skillIndicatorInstance.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, 0f, angle));
        _skillIndicatorInstance.transform.localScale = new Vector3(SkillLength, SkillWaveWidth, 1f);
        SpriteRenderer sr = _skillIndicatorInstance.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (_skillIndicatorInstance != null) { Destroy(_skillIndicatorInstance); _skillIndicatorInstance = null; }
        skillCooldownTimer = skillCooldown;
        Vector2 dir = ((Vector2)position - (Vector2)transform.position).normalized;
        StartCoroutine(FireSweep(dir));
    }

    private IEnumerator FireSweep(Vector2 dir)
    {
        yield return new WaitForSeconds(SkillDelay);
        if (fireWavePrefab == null) yield break;

        // start the wave at the back of the lane (behind the plant) so it sweeps the full screen.
        // the wave is bounded to the lane width, matching the indicator
        Vector2 startPos = (Vector2)transform.position - dir * (SkillLength * 0.5f);
        GameObject obj = Instantiate(fireWavePrefab, startPos, Quaternion.identity);
        obj.GetComponent<FireWave>()?.Initialize(
            startPos, dir,
            SData?.skillWaveSpeed ?? 12f,
            SkillWaveWidth,
            SData?.skillThickness ?? 3f,
            SkillDamage,
            SkillBurnMultiplier,
            SkillFlammableStacks,
            SkillLength,
            this,
            IsPath3Maxed);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (SData?.path1AttackDamagePerLevel ?? 3f)  * level;
        baseAttackRange  = data.baseAttackRange  + (SData?.path1AttackRangePerLevel  ?? 0.2f) * level;
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override string GetName() => "<b><color=orange>Stargazer</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} sprays a cone of fire that leaves enemies <color=#FF6B1A>Flammable</color>, and can call down a sweeping wall of flame across the entire map.";

    public override string GetAttackDescription() =>
        $"Sprays fire in a <color=green><b>{ConeAngle:F0}°</b></color> cone, dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} damage to all insects within it.";

    public override string GetPassiveDescription() =>
        $"Striking a target applies <color=green><b>{StacksPerHit}</b></color> stack{(StacksPerHit == 1 ? "" : "s")} of <color=#FF6B1A>Flammable</color> for <color=green><b>{FlammableDuration:F0}s</b></color>, increasing the <color=orange>Burn</color> damage it takes by <color=green><b>{FlammableBonusPerStack * 100f:F0}%</b></color> per stack. <color=orange>Burn</color> effects caused by the {GetName()} last <color=green><b>{BurnDurationBonus * 100f:F0}%</b></color> longer.";

    public override string GetSkillDesription() =>
        $"Aim a direction. After a brief delay, a <color=#FF6B1A><b>Fire Wave</b></color> sweeps across the entire map, dealing <color=green><b>{(SData?.skillBaseDamage ?? 200f) + (SData?.path3SkillDamagePerLevel ?? 40f) * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} damage to everything in its path and applying <color=green><b>{SkillFlammableStacks}</b></color> stack{(SkillFlammableStacks == 1 ? "" : "s")} of <color=#FF6B1A>Flammable</color>. Deals <color=green><b>{SkillBurnMultiplier:F1}x</b></color> damage to <color=orange>Burning</color> targets.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl  = SData?.path1AttackDamagePerLevel ?? 3f;
        float rngpl = SData?.path1AttackRangePerLevel  ?? 0.2f;
        string desc = details
            ? $"Sprays fire in a <color=green><b>{ConeAngle:F0}</b></color> degree cone, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} damage to all insects within it."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rngpl:F2}</b></color> per level. [<color=green><b>+{rngpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Decrease <color=green><b>Total Attack Damage</b></color> by <color=red><b>33%</b></color>, but every attack grants a stack of <color=orange><b>Scorcher</b></color>, increasing <color=orange><b>Fire Damage</b></color> by <color=green><b>4%</b></color> per stack, up to <color=green><b>10</b></color>, for <color=green><b>12s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        int   spl  = SData?.path2StacksPerLevel       ?? 1;
        float bdpl = SData?.path2BurnDurationPerLevel  ?? 0.1f;
        string desc = details
            ? $"Striking a target applies <color=green><b>[({SData?.baseStacksPerHit ?? 1}) + ({spl}/Lvl.)]</b></color> stack{(StacksPerHit == 1 ? "" : "s")} of <color=#FF6B1A>Flammable</color> for <color=green><b>{FlammableDuration:F0}</b></color> seconds, increasing the <color=orange>Burn</color> damage it takes by <color=green><b>{FlammableBonusPerStack * 100f:F0}%</b></color> per stack. <color=orange>Burn</color> effects caused by the {GetName()} last <color=green><b>[({SData?.baseBurnDurationBonus ?? 0.5f:F0}%) + ({bdpl * 100f:F0}%/Lvl.)]</b></color> longer."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=#FF6B1A>Flammable</color> stacks applied per hit by <color=green><b>{spl}</b></color> per level. [<color=green><b>+{Mathf.RoundToInt(spl * effectivePath2Level)}</b></color>]\n\n" +
               $"Increase <color=orange>Burn</color> duration by <color=green><b>{bdpl * 100f:F0}%</b></color> per level. [<color=green><b>+{bdpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, "Upon inflicting a <color=orange>Burn</color>, grants a stack of <color=#FF6B1A><b>Melter</b></color>, increasing <color=#FFD700><b>Elemental Affinity</b></color> by <color=green><b>6%</b></color> per stack, up to <color=green><b>10</b></color>, for <color=green><b>12s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float dpl = SData?.path3SkillDamagePerLevel     ?? 40f;
        float wpl = SData?.path3WaveWidthPerLevel        ?? 0.5f;
        float bpl = SData?.path3BurnMultiplierPerLevel   ?? 0.1f;
        float fpl = SData?.path3FlammableStacksPerLevel  ?? 0.5f;
        string desc = details
            ? $"Aim a direction. After a brief delay, a <color=#FF6B1A><b>Fire Wave</b></color> sweeps across the entire map, dealing <color=green><b>[({SData?.skillBaseDamage ?? 200f:F0}) + ({dpl:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} damage to everything in its path and applying <color=green><b>[({SData?.skillFlammableStacks ?? 2}) + ({fpl:F1}/Lvl.)]</b></color> stacks of <color=#FF6B1A>Flammable</color>. Deals <color=green><b>[({SData?.skillBurnMultiplier ?? 2f:F1}) + ({bpl:F1}/Lvl.)]x</b></color> damage to <color=orange>Burning</color> targets."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=#FF6B1A>Fire Wave</color> damage by <color=green><b>{dpl:F0}</b></color> per level. [<color=green><b>+{dpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=#FF6B1A>Fire Wave</color> width by <color=green><b>{wpl:F1}</b></color> per level. [<color=green><b>+{wpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase <color=orange>Burning</color>-target multiplier by <color=green><b>{bpl:F1}x</b></color> per level. [<color=green><b>+{bpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase <color=#FF6B1A>Flammable</color> stacks applied by <color=green><b>{fpl:F1}</b></color> per level. [<color=green><b>+{Mathf.RoundToInt(fpl * effectivePath3Level)}</b></color>]\n\n" +
               $"{Level5Section(path3Level, "The <color=#FF6B1A><b>Fire Wave</b></color> returns after reaching its end, sweeping back in the opposite direction.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
