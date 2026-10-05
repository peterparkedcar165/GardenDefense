using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AcornKnight : Aura
{
    [SerializeField] private Transform swordVisual;
    [SerializeField] private Transform shieldVisual;
    [SerializeField] private GameObject shieldProjectilePrefab;
    [SerializeField] private GameObject shieldPrefab;

    private AcornKnightData AcornData => data as AcornKnightData;

    private const float SwingArcDegrees = 60f;
    // authored against data.baseAttackSpeed - scales inversely with current attackSpeed, same
    // convention as Plant.AttackChargeTime, so a faster Knight physically swings faster instead
    // of the hitbox sweep taking a fixed amount of real time regardless of attack speed
    private const float BaseSwingDuration = 0.18f;
    private float SwingDuration
    {
        get
        {
            float baseSpeed = data != null && data.baseAttackSpeed > 0f ? data.baseAttackSpeed : attackSpeed;
            return BaseSwingDuration * (baseSpeed / Mathf.Max(0.01f, attackSpeed));
        }
    }

    // "windshield wiper" sword hitbox: a thin rectangle, pivoted at the Knight's own position,
    // extending out to 1.15x attackRange (a little past the stated range, like a real swing's
    // reach). sweeps a full top-to-bottom semicircle on whichever side the target is on (same
    // side-restriction the old static semicircle used), alternating direction every attack like a
    // real wiper blade - one pass this attack, the reverse pass next attack
    private const float SwingHitboxWidth = 0.075f;
    private const float SwingHitboxLengthMultiplier = 1.15f;
    private float SwingHitboxLength => attackRange * SwingHitboxLengthMultiplier;
    // neither the sweep nor Bash cap how many insects they can hit - the 10%-per-extra falloff
    // just bottoms out at each one's own floor instead of ever reaching 0
    private const float SwingMinDamageMultiplier = 0.5f;
    private const float BashMinDamageMultiplier = 0.5f;
    private bool _nextSweepTopToBottom = true;
    private Coroutine _swordSweepRoutine;

    // fine enough that a thin insect can't get skipped between frames even at high game-speed
    // multipliers, where Time.deltaTime (and so how far the sweep advances per frame) is larger
    private const float MaxSweepAngleStepDegrees = 1f;

    // visible placeholder for the hitbox itself (a plain colored rectangle) until real sword art
    // exists - a 1x1 world-unit white sprite pivoted at its left edge, so scaling it by
    // (SwingHitboxLength, SwingHitboxWidth) stretches it out exactly to the real hitbox's dimensions,
    // pivoting from the Knight's own position same as the hitbox math does
    private static Sprite _hitboxSprite;
    private GameObject _hitboxVisualObj;
    private SpriteRenderer _hitboxVisualRenderer;

    private Insect _mainTarget;
    private Vector2 _facingDir = Vector2.right;
    private float _swordSwingTimer;
    private float _shieldSwingTimer;

    // counts down only while no insect is currently targeting the Knight (see IsTargetingMe).
    // reset to StanceExitDelay every frame at least one qualifying insect is targeting it, so the
    // guard drops StanceExitDelay seconds after the last one stops, not after the last hit landed
    private float _stanceExitTimer;

    private bool _shieldOut;
    private float _reequipTimer;
    private float shieldHealth, shieldRadius;
    // skill damage is a flat percent of Armor, no level scaling at all
    private const float SkillDamageArmorPercent = 1.5f;
    private AcornKnightShield _activeShield;

    // passive regen, no level scaling at all: a slow trickle normally, picking up to a faster
    // rate once nothing has damaged the Knight for a while. reset on every hit regardless of
    // source/amount, via the shared OnEntityHit event (filtered to hits where this is the target)
    private float _noDamageTimer;
    private float _regenTickTimer;
    private const float RegenTickInterval = 1f;

    // skill tree node unlock ids
    public const string InstantSkillUnlock    = "acorn_instant_skill";
    public const string CounterStanceUnlock   = "acorn_counter_stance";
    public const string EvasiveGuardUnlock    = "acorn_evasive_guard";
    public const string CleavingSwingsUnlock  = "acorn_cleaving_swings";
    public const string RelentlessBashUnlock  = "acorn_relentless_bash";

    public float ConeFalloffPerTarget   => AcornData?.coneFalloffPerTarget   ?? 0.10f;
    public float AttackWindupTime       => AcornData?.attackWindupTime       ?? 0.1f;
    public float StanceExitDelay        => AcornData?.stanceExitDelay        ?? 1f;
    public float SkillThrowRange        => AcornData?.skillThrowRange       ?? 7.5f;
    public float ShieldThrowSpeed       => AcornData?.shieldThrowSpeed       ?? 10f;
    public float ShieldStunDuration     => AcornData?.shieldStunDuration     ?? 1.5f;
    public float SkillAttackSpeedBonus =>
        (AcornData?.baseSkillAttackSpeedBonus ?? 0.2f) + (AcornData?.path3AttackSpeedBonusPerLevel ?? 0.05f) * effectivePath3Level;
    public float ShieldReequipDelay     => AcornData?.shieldReequipDelay     ?? 1f;

    public float DefensiveArmor =>
        (AcornData?.baseDefensiveArmor ?? 20f) + (AcornData?.path2DefensiveArmorPerLevel ?? 6f) * effectivePath2Level;
    public float DefensiveAttackSpeedPenalty => AcornData?.defensiveAttackSpeedPenalty ?? 0.75f;
    public float DefensiveAttackRangePenalty => AcornData?.defensiveAttackRangePenalty ?? 0.25f;

    // Path1's own Armor bonus (not Path2's Guard Stance bonus, which is already unavailable while
    // the shield is out) - lost entirely for as long as the shield is out
    public float AttackPathArmorBonus => (AcornData?.path1ArmorPerLevel ?? 4) * effectivePath1Level;

    // Shield Bash's own stun chance - max-level (Path2) only, see Level5Section in
    // GetPath2Description. flat, no per-level scaling, since it doesn't exist below max at all
    public float MaxLevelBashStunChance => AcornData?.maxLevelBashStunChance ?? 0.75f;
    public float BashStunDuration => AcornData?.bashStunDuration ?? 1f;
    // Shield Bash's own damage is this percent of Armor (for the first/closest insect hit this
    // cast) - the separate 10%-per-extra-insect falloff (see Bash()) still applies on top of this
    public float BashDamagePercent =>
        (AcornData?.baseBashDamagePercent ?? 0.5f) + (AcornData?.path2BashDamagePercentPerLevel ?? 0.10f) * effectivePath2Level;

    // percent of max health, ticking once per second, no level scaling at all - picks up to the
    // boosted rate once nothing has damaged the Knight for RegenBoostDelay seconds
    public float BaseRegenPercent => AcornData?.baseRegenPercentPerSecond ?? 0.01f;
    public float BoostedRegenPercent => AcornData?.boostedRegenPercentPerSecond ?? 0.03f;
    public float RegenBoostDelay => AcornData?.regenBoostDelay ?? 6f;

    // skill tree node 5a (Vengeful Guard): taking Physical damage while in Guard Stance refunds
    // a slice of Attack Cooldown, rewarding tanking hits instead of just sitting on the bonus armor
    public float CounterStanceCooldownReduction => AcornData?.counterStanceCooldownReduction ?? 0.2f;
    // skill tree node 5b (Evasive Guard): flat Evasion while in Guard Stance
    public float DefensiveEvasionBonus => AcornData?.defensiveEvasionBonus ?? 0.15f;

    // toggle target like any Shooter (Nearest/First/Last/Strongest), same UI affordance
    public override bool UsesTargeting => true;
    public override bool IsMeleeAttacker => true;

    // shield lifetime scales with Magic Power - a raw-seconds bonus, so no /100 division (see
    // feedback_magic_power_scaling memory: only decimal-percentage stats divide)
    public float SkillDurationMP => (AcornData?.skillDurationMPMultiplier ?? 0.10f) * magicPower;

    public bool IsDefensiveStance => !_shieldOut && _stanceExitTimer > 0f;

    // cooldown starts the instant the shield is thrown (see OnTargetConfirmed), but the skill
    // button must also stay locked out past that if the shield is still alive on the field -
    // cooldown alone isn't enough once skill cooldown reduction (skill tree, etc) brings it below
    // the shield's own lifetime
    public override bool SkillReady => base.SkillReady && _activeShield == null;

    protected override void Awake()
    {
        base.Awake();
        LoadData();

        OnPath1Upgrade(effectivePath1Level);
        OnPath3Upgrade(effectivePath3Level);

        // free skill readiness on placement - deliberately bypasses UnlockPath3() (which spends
        // sun and adds to totalSunSpent) so this can't be abused for an inflated uproot refund
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }

        // stays visible permanently (not just mid-swing) so it's always on screen to tune against
        EnsureHitboxVisual();
        _hitboxVisualObj.SetActive(true);

        _noDamageTimer = RegenBoostDelay; // starts already at the boosted rate, nothing's hit it yet
        Entity.OnEntityHit += HandleAnyEntityHit;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Entity.OnEntityHit -= HandleAnyEntityHit;
    }

    private void HandleAnyEntityHit(EntityEventData data)
    {
        if (data.target != this) return;
        _noDamageTimer = 0f;

        if (IsDefensiveStance && data.damageType == DamageType.Physical && SkillTreeManager.HasUnlock(this, CounterStanceUnlock))
            attackCooldownTimer = Mathf.Max(0f, attackCooldownTimer - CounterStanceCooldownReduction);
    }

    protected override void Update()
    {
        base.Update(); // Aura sets attackCooldown = 1 / attackSpeed

        TickTimers(Time.deltaTime);
        TickRegen(Time.deltaTime);

        _mainTarget = FindTarget();
        if (_mainTarget != null)
            SetFacing(_mainTarget.GetApproachPoint(transform.position));

        bool stunned = IsStunned || IsChanneling;
        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!stunned && _mainTarget != null)
            Attack();

        UpdateFacingVisual();
    }

    private void TickTimers(float dt)
    {
        if (_reequipTimer > 0f)
        {
            _reequipTimer -= dt;
            if (_reequipTimer <= 0f) _shieldOut = false;
        }

        // the guard stays up as long as at least one insect is currently targeting the Knight in
        // melee, and only starts counting down the exit delay once none are
        bool anyTargeting = false;
        foreach (Insect insect in Insect.allInsects)
        {
            if (!IsTargetingMe(insect)) continue;
            anyTargeting = true;
            break;
        }

        if (anyTargeting)
            _stanceExitTimer = StanceExitDelay;
        else if (_stanceExitTimer > 0f)
            _stanceExitTimer -= dt;
    }

    // percent-of-max-health regen, no level scaling: BaseRegenPercent normally, bumping up to
    // BoostedRegenPercent once nothing has damaged the Knight for RegenBoostDelay seconds
    // straight - reset on every hit via HandleAnyEntityHit, regardless of source or amount.
    // ticks once per RegenTickInterval (a real heal pulse, not a smooth per-frame trickle) - a
    // while loop in case a single frame's dt ever spans more than one full tick (lag spike, very
    // high game-speed multiplier)
    private void TickRegen(float dt)
    {
        if (!IsAlive) return;
        _noDamageTimer += dt;
        _regenTickTimer += dt;

        while (_regenTickTimer >= RegenTickInterval)
        {
            _regenTickTimer -= RegenTickInterval;
            float ratePercent = _noDamageTimer >= RegenBoostDelay ? BoostedRegenPercent : BaseRegenPercent;
            if (ratePercent > 0f) Heal(maxHealth * ratePercent, this);
        }
    }

    // an insect "engages in targeting" the Knight when its own live target resolves to this
    // plant (Insect.target already accounts for taunts, aggressivity and range) and it would
    // deal physical damage - ranged/magic attackers never trigger the guard, regardless of range
    private bool IsTargetingMe(Insect insect) =>
        insect != null && insect.IsAlive && insect.attackDamageType == DamageType.Physical
        && insect.target == (IAttackable)this;

    private void SetFacing(Vector2 towards)
    {
        Vector2 to = towards - (Vector2)transform.position;
        if (to.sqrMagnitude > 0.0001f)
            _facingDir = to.normalized;
    }

    // body flip is handled generically by Plant.UpdateFacingFlip (off the same
    // GetHighlightTarget() this plant already overrides) - only the sword/shield rotation
    // angle, which that generic mechanism doesn't need, is tracked here
    private void UpdateFacingVisual()
    {
        float baseAngle = Mathf.Atan2(_facingDir.y, _facingDir.x) * Mathf.Rad2Deg;

        if (swordVisual != null)
        {
            float angle = baseAngle;
            if (_swordSwingTimer > 0f)
            {
                _swordSwingTimer -= Time.deltaTime;
                float t01 = 1f - Mathf.Clamp01(_swordSwingTimer / SwingDuration);
                angle += Mathf.Sin(t01 * Mathf.PI) * SwingArcDegrees * 0.5f;
            }
            swordVisual.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (shieldVisual != null)
        {
            shieldVisual.gameObject.SetActive(!_shieldOut);
            float angle = baseAngle;
            if (_shieldSwingTimer > 0f)
            {
                _shieldSwingTimer -= Time.deltaTime;
                float t01 = 1f - Mathf.Clamp01(_shieldSwingTimer / SwingDuration);
                angle += Mathf.Sin(t01 * Mathf.PI) * SwingArcDegrees * 0.5f;
            }
            shieldVisual.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // the sword hitbox is irrelevant while in Guard Stance (attacks become Shield Bash
        // instead), so hide it there and bring it back the instant the Knight returns to its
        // normal sword-swinging stance
        if (_hitboxVisualObj != null)
            _hitboxVisualObj.SetActive(!IsDefensiveStance);
    }

    // same toggle-target helpers every Shooter uses (Plant.FindNearest/FindFirst/FindLast/
    // FindStrongest already filter by attackRange and night visibility)
    private Insect FindTarget()
    {
        GameObject target;
        switch (targeting)
        {
            case TARGETING.First:     target = FindFirst(Insect.allInsects);     break;
            case TARGETING.Nearest:   target = FindNearest(Insect.allInsects);   break;
            case TARGETING.Last:      target = FindLast(Insect.allInsects);      break;
            case TARGETING.Strongest: target = FindStrongest(Insect.allInsects); break;
            default:                  target = null;                            break;
        }
        return target != null ? target.GetComponent<Insect>() : null;
    }

    protected override GameObject GetHighlightTarget() => _mainTarget != null ? _mainTarget.gameObject : null;

    // windup/resolve split (see Plant.BeginAttackWindup): committing to an attack and actually
    // dealing its damage are now two separate moments. base.Attack() fires immediately - that's
    // windup start, so the cooldown reset and attack sound both still happen the instant the
    // swing begins, same as before. which action (bash vs sword) and who the target is are both
    // locked in right here, matching whatever visual starts playing now, rather than re-read at
    // resolve time when Update() may have already moved _mainTarget on. only aliveness is
    // re-checked at resolve, since that's the one thing a windup can invalidate
    protected override void Attack()
    {
        base.Attack();
        Insect target = _mainTarget;
        if (target == null) return;

        bool bash = IsDefensiveStance;
        if (bash) _shieldSwingTimer = SwingDuration;
        else      _swordSwingTimer = SwingDuration;

        BeginAttackWindup(AttackWindupTime, () =>
        {
            if (target == null || !target.IsAlive) return;
            if (bash) Bash(target);
            else      StartSwordSweep(target);
        });
    }

    // kicks off the wiper sweep on whichever side (left/right) the target is on, relative to the
    // Knight's own position - same side-restriction the old static semicircle used. direction
    // alternates every attack (top-to-bottom, then bottom-to-top, then top-to-bottom again),
    // like a real wiper blade rather than resetting to the same start every time
    private void StartSwordSweep(Insect target)
    {
        Vector2 targetApproach = target.GetApproachPoint(transform.position);
        float facingSign = Mathf.Sign(targetApproach.x - transform.position.x);
        if (facingSign == 0f) facingSign = 1f;

        bool topToBottom = _nextSweepTopToBottom;
        _nextSweepTopToBottom = !_nextSweepTopToBottom;

        if (_swordSweepRoutine != null) StopCoroutine(_swordSweepRoutine);
        _swordSweepRoutine = StartCoroutine(SwordSweepRoutine(facingSign, topToBottom));
    }

    // sweeps a thin rectangle hitbox (pivoted at the Knight's own position, length
    // SwingHitboxLength) through a 180-degree arc over SwingDuration. each insect can only be hit
    // once per sweep (tracked in hit), with 10%-per-extra falloff bottoming out at
    // SwingMinDamageMultiplier - no cap on how many insects can be hit in one sweep
    private IEnumerator SwordSweepRoutine(float facingSign, bool topToBottom)
    {
        HashSet<Insect> hit = new HashSet<Insect>();
        float elapsed = 0f;
        float lastT = 0f;

        float AngleAt(float t)
        {
            float angleFromTop = topToBottom ? Mathf.Lerp(0f, 180f, t) : Mathf.Lerp(180f, 0f, t);
            return 90f - facingSign * angleFromTop;
        }

        void SampleAt(float t)
        {
            float worldAngle = AngleAt(Mathf.Clamp01(t));
            UpdateHitboxVisual(worldAngle);
            HitInsectsInWiper(worldAngle, hit);
        }

        SampleAt(0f);

        while (elapsed < SwingDuration)
        {
            float duration = Mathf.Max(0.0001f, SwingDuration);
            float newElapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            float newT = newElapsed / duration;

            // at high game-speed multipliers Time.deltaTime (and so how far the sweep advances
            // in one frame) gets much larger - without this, a single big frame step could rotate
            // straight past a thin insect without ever sampling an angle that overlapped it.
            // substepping keeps the angular resolution the same regardless of frame size
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(newT - lastT) * 180f / MaxSweepAngleStepDegrees));
            for (int i = 1; i <= steps; i++)
                SampleAt(Mathf.Lerp(lastT, newT, (float)i / steps));

            lastT = newT;
            elapsed = newElapsed;
            yield return null;
        }

        _swordSweepRoutine = null;
    }

    // worldAngleDegrees: 0 = facing +X, 90 = straight up, 180/-180 = straight down's mirror,
    // -90 = straight down - mirrors Calendula's own SweepAttackDamage falloff pattern (hit.Count
    // at the moment an insect is reached doubles as the falloff index, no separate counter needed)
    private void HitInsectsInWiper(float worldAngleDegrees, HashSet<Insect> hit)
    {
        float worldAngleRad = worldAngleDegrees * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(worldAngleRad), Mathf.Sin(worldAngleRad));
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float halfWidth = SwingHitboxWidth * 0.5f;
        bool noFalloff = SkillTreeManager.HasUnlock(this, CleavingSwingsUnlock);

        // snapshot first: Insect.Kill() removes the insect from allInsects synchronously, so
        // killing one with this very hit would otherwise mutate the list mid-foreach and throw,
        // silently killing this coroutine - which looked exactly like the swing "just stopping"
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || hit.Contains(insect)) continue;

            Vector2 toInsect = (Vector2)insect.transform.position - (Vector2)transform.position;
            float along = Vector2.Dot(toInsect, dir);
            if (along < 0f || along > SwingHitboxLength) continue;
            float across = Vector2.Dot(toInsect, perp);
            if (Mathf.Abs(across) > halfWidth) continue;

            float multiplier = noFalloff ? 1f : Mathf.Max(SwingMinDamageMultiplier, 1f - ConeFalloffPerTarget * hit.Count);
            hit.Add(insect);
            insect.Damage(attackDamage * multiplier, damageType, elementalType, this, false, new DamageTag[] { DamageTag.Melee, DamageTag.Attack });
        }
    }

    private static Sprite GetHitboxSprite()
    {
        if (_hitboxSprite != null) return _hitboxSprite;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        // pivot at the left edge (0, 0.5) rather than center, so the sprite's own position IS
        // the pivot point the hitbox math rotates around, and scaling along X only ever extends
        // outward from there instead of growing in both directions
        _hitboxSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), 1f);
        return _hitboxSprite;
    }

    private void EnsureHitboxVisual()
    {
        if (_hitboxVisualObj != null) return;
        _hitboxVisualObj = new GameObject("SwordHitboxVisual");
        _hitboxVisualObj.transform.SetParent(transform, false);
        _hitboxVisualRenderer = _hitboxVisualObj.AddComponent<SpriteRenderer>();
        _hitboxVisualRenderer.sprite = GetHitboxSprite();
        _hitboxVisualRenderer.color = new Color(0.15f, 1f, 0.15f, 0.4f);
        // same sorting layer the Knight's own body sprite uses - without this it defaults to
        // "Default", which sits behind every other layer in this project's sorting order
        // regardless of sortingOrder, so it was rendering fully hidden
        _hitboxVisualRenderer.sortingLayerName = "Plants";
        _hitboxVisualRenderer.sortingOrder = 1;

        // starts parked at 12 o'clock (world angle 90) before any attack has ever fired, matching
        // where the very first sweep (top-to-bottom) begins
        UpdateHitboxVisual(90f);
    }

    private void UpdateHitboxVisual(float worldAngleDegrees)
    {
        if (_hitboxVisualObj == null) return;
        _hitboxVisualObj.transform.localPosition = Vector3.zero;
        _hitboxVisualObj.transform.localRotation = Quaternion.Euler(0f, 0f, worldAngleDegrees);
        _hitboxVisualObj.transform.localScale = new Vector3(SwingHitboxLength, SwingHitboxWidth, 1f);
    }

    // omnidirectional: the target plus every insect within the Knight's own attack range, closest
    // to THAT target (not to the Knight) first - no cap on how many can be hit. since this lands
    // all at once instead of being discovered over time like the sweep, distance-to-target order
    // stands in for "order hit": same 10%-per-extra falloff as the swing, down to its own floor,
    // scaling off Armor instead of Attack Damage and tagged as counter damage. at Path2 max only,
    // each insect hit also gets an independent roll at MaxLevelBashStunChance to be stunned for
    // BashStunDuration - this doesn't exist at all below max level
    private void Bash(Insect target)
    {
        Vector2 targetApproach = target.GetApproachPoint(transform.position);

        List<Insect> others = new List<Insect>();
        // snapshot: Insect.Kill() removes itself from allInsects synchronously, which would
        // otherwise mutate this list mid-foreach if an earlier hit below kills something
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || insect == target) continue;
            Vector2 approach = insect.GetApproachPoint(transform.position);
            if (Vector2.Distance(transform.position, approach) > attackRange) continue;
            others.Add(insect);
        }
        others.Sort((a, b) => Vector2.Distance(targetApproach, a.GetApproachPoint(transform.position))
            .CompareTo(Vector2.Distance(targetApproach, b.GetApproachPoint(transform.position))));

        List<Insect> candidates = new List<Insect> { target };
        candidates.AddRange(others);
        bool noFalloff = SkillTreeManager.HasUnlock(this, RelentlessBashUnlock);

        for (int i = 0; i < candidates.Count; i++)
        {
            Insect insect = candidates[i];
            float falloff = noFalloff ? 1f : Mathf.Max(BashMinDamageMultiplier, 1f - ConeFalloffPerTarget * i);
            insect.Damage(armor * BashDamagePercent * falloff, damageType, elementalType, this, false, new DamageTag[] { DamageTag.Melee, DamageTag.Attack, DamageTag.Counter });

            if (insect.IsAlive && IsPath2Maxed && Random.value < MaxLevelBashStunChance)
                insect.ApplyEffect(new StunEffect(insect, BashStunDuration, 1, this));
        }
    }

    public override void UpdateStats()
    {
        bool defensive = IsDefensiveStance;

        // total-attack-speed changes go through attackSpeedTotalMultiplier (bumped around
        // base.UpdateStats(), like every other total-multiplier bonus in this codebase) rather
        // than post-multiplying the already-computed attackSpeed, so they compose correctly
        // instead of also scaling flat adders applied below
        float speedBonus = 0f;
        if (defensive)  speedBonus -= DefensiveAttackSpeedPenalty;
        if (_shieldOut) speedBonus += SkillAttackSpeedBonus;

        float rangeMultiplierDelta = defensive ? -DefensiveAttackRangePenalty : 0f;
        // goes through armorAdder (bumped around base.UpdateStats(), same convention as every
        // other temporary stat change here) rather than subtracting from the already-computed
        // armor directly, so it composes correctly with armor from other sources instead of
        // clobbering them
        float armorAdderDelta = _shieldOut ? -AttackPathArmorBonus : 0f;
        float evasionAdderDelta = (defensive && SkillTreeManager.HasUnlock(this, EvasiveGuardUnlock)) ? DefensiveEvasionBonus : 0f;

        attackSpeedTotalMultiplier += speedBonus;
        attackRangeTotalMultiplier += rangeMultiplierDelta;
        armorAdder += armorAdderDelta;
        evasionAdder += evasionAdderDelta;
        base.UpdateStats();
        attackSpeedTotalMultiplier -= speedBonus;
        attackRangeTotalMultiplier -= rangeMultiplierDelta;
        armorAdder -= armorAdderDelta;
        evasionAdder -= evasionAdderDelta;

        if (defensive)
            armor += Mathf.RoundToInt(DefensiveArmor);

        skillDuration += SkillDurationMP;

        if (IsPath1Maxed)
            attackDamage += armor * 0.33f;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * (AcornData?.path1AttackDamagePerLevel ?? 8f);
        baseAttackSpeed  = data.baseAttackSpeed  + level * (AcornData?.path1AttackSpeedPerLevel  ?? 0.05f);
        baseArmor        = data.baseArmor        + level * (AcornData?.path1ArmorPerLevel        ?? 4);
    }

    public override void OnPath3Upgrade(int level)
    {
        float durPerLevel     = AcornData?.path3SkillDurationPerLevel ?? 2f;
        float hpPerLevel      = AcornData?.path3HealthPerLevel        ?? 50f;

        baseSkillDuration = data.baseSkillDuration + durPerLevel * level;
        shieldHealth      = data.baseSkillHealth   + hpPerLevel  * level;
        shieldRadius      = data.baseSkillRadius; // flat, no longer scales with level
    }

    // line skill shot: player aims a point (clamped to SkillThrowRange, a flat distance
    // independent of melee attackRange), the shield is fired straight at it and stops at the
    // first insect it touches (ShieldThrowProjectile has no piercing), or at the clamped point
    // itself (max range) if it hits nothing - either way it then falls to the ground there
    public override void ActivateSkill()
    {
        if (!SkillReady || _shieldOut) return;
        SkillTargetingManager.instance.BeginTargeting(SkillThrowRange, OnTargetConfirmed, transform.position, SkillThrowRange, asLine: true, lineWidth: GetProjectileLineWidth());
    }

    // reads straight off the projectile prefab's own CircleCollider2D so the targeting line always
    // matches whatever the projectile's actual hit width is tuned to, rather than a second
    // hardcoded number that can silently drift out of sync with it
    private float GetProjectileLineWidth()
    {
        CircleCollider2D col = shieldProjectilePrefab != null ? shieldProjectilePrefab.GetComponent<CircleCollider2D>() : null;
        return col != null ? col.radius * 2f : 0.08f;
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (shieldProjectilePrefab == null) return;

        Vector2 toTarget = (Vector2)position - (Vector2)transform.position;
        Vector2 landPos = toTarget.magnitude > SkillThrowRange
            ? (Vector2)transform.position + toTarget.normalized * SkillThrowRange
            : (Vector2)position;

        // flat percent of current Armor, no level scaling - captured right now before _shieldOut
        // flips true below, since Guard Stance's armor bonus (if currently up) drops out of armor
        // the instant the shield is out, so reading it after that point would silently undercut
        // the throw's own damage
        float damage = armor * SkillDamageArmorPercent;

        GameObject obj = Instantiate(shieldProjectilePrefab, transform.position, Quaternion.identity);
        ShieldThrowProjectile proj = obj.GetComponent<ShieldThrowProjectile>();
        proj?.Initialize(landPos, damage, damageType, elementalType, ShieldThrowSpeed, this, OnShieldLanded);

        SetFacing(landPos);
        _shieldOut = true;
        _shieldSwingTimer = SwingDuration;
        skillCooldownTimer = skillCooldown; // cooldown starts the instant the shield is thrown
    }

    // called by the flying shield once it hits an insect or reaches its clamped landing point.
    // stun does not benefit from piercing (there is none here) and is applied once, routed
    // through the ordinary StunEffect like any other hard CC in the game
    private void OnShieldLanded(Vector3 position, Insect hitInsect)
    {
        if (hitInsect != null && hitInsect.IsAlive)
            hitInsect.ApplyEffect(new StunEffect(hitInsect, ShieldStunDuration, 1, this));

        if (shieldPrefab == null) { _shieldOut = false; return; }

        GameObject obj = Instantiate(shieldPrefab, position, Quaternion.identity);
        AcornKnightShield shield = obj.GetComponent<AcornKnightShield>();
        if (shield == null) { _shieldOut = false; return; }

        shield.Initialize(shieldRadius, shieldHealth, skillDuration, this);
        _activeShield = shield;
        shield.OnShieldGone += HandleShieldGone;
    }

    // cooldown itself already started the instant the shield was thrown (see OnTargetConfirmed) -
    // this just clears the active-shield reference (which SkillReady also gates on, so the skill
    // stays locked out even if the cooldown already ran out while the shield was still up) and
    // starts the re-equip delay, which holds the Knight in the shield-out state a little longer
    // before it can enter defensive stance or lose the skill's attack speed bonus
    private void HandleShieldGone()
    {
        if (_activeShield != null) _activeShield.OnShieldGone -= HandleShieldGone;
        _activeShield = null;
        _reequipTimer = ShieldReequipDelay;
    }

    public override string GetName() => $"<b><color=green>{(data != null ? data.displayName : "Acorn Knight")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a melee tank with a sword and shield, raising its guard under pressure and able to hurl its shield to stun a target and block the path.";

    public override string GetAttackDescription() =>
        $"Whenever {GetName()} is not being targeted by a Physical damaging insect, it holds an <color=orange><b>Attack Stance</b></color>.\n\n" +
        $"<color=orange><b>Attack Stance</b></color>: {GetName()} swings his sword in a semi-circle, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects caught in the swing.";

    public override string GetPassiveDescription() =>
        $"{GetName()} regenerates <color=green><b>{BaseRegenPercent * 100f:F0}%</b></color> Health per second. After not being damaged for <color=green><b>{RegenBoostDelay:F0}s</b></color>, the regeneration increases to <color=green><b>{BoostedRegenPercent * 100f:F0}%</b></color> per second.\n\n" +
        $"If {GetName()} is being targeted by a Physical damaging insect, it changes into <color=orange><b>Guard Stance</b></color>, during which it gains <color=#00CED1><b>+{DefensiveArmor:F0} Armor</b></color>, while losing <color=green><b>{DefensiveAttackSpeedPenalty * 100f:F0}%</b></color> Attack Speed and <color=green><b>{DefensiveAttackRangePenalty * 100f:F0}%</b></color> Attack Range.\n\n" +
        $"While in <color=orange><b>Guard Stance</b></color>, its attacks become an omnidirectional <color=green><b>Shield Bash</b></color> dealing <color={PlantData.ElementalColor(elementalType)}><b>{armor * BashDamagePercent:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}.";

    public override string GetSkillDesription() =>
        $"The {GetName()} throws his shield, stopping at the first insect hit and dealing <color={PlantData.ElementalColor(elementalType)}><b>{armor * SkillDamageArmorPercent:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} and stunning it for <color=green><b>{ShieldStunDuration:F1}s</b></color>. " +
        $"The shield then sits at the hit location for <color=green><b>{skillDuration:F1}</b></color> [<color=#FFB6C1><b>+{SkillDurationMP:F1}</b></color>] seconds, blocking and taunting insects, with <color=green><b>{shieldHealth:F0}</b></color> health.\n\n" +
        $"While the shield is out, {GetName()} cannot guard, loses <color=#00CED1><b>{AttackPathArmorBonus:F0} Armor</b></color>, and gains <color=green><b>+{SkillAttackSpeedBonus * 100f:F0}%</b></color> Attack Speed.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = AcornData?.path1AttackDamagePerLevel ?? 8f;
        float aspl = AcornData?.path1AttackSpeedPerLevel  ?? 0.05f;
        int   armorpl = AcornData?.path1ArmorPerLevel     ?? 4;
        string desc = details
            ? $"Whenever {GetName()} is not being targeted by a Physical damaging insect, it holds an <color=orange><b>Attack Stance</b></color>.\n\n" +
              $"<color=orange><b>Attack Stance</b></color>: {GetName()} swings his sword in a semi-circle through the side its target is on, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} to all insects caught in the swing, with <color=green><b>{ConeFalloffPerTarget * 100f:F0}%</b></color> falloff per extra insect hit down to a <color=green><b>{SwingMinDamageMultiplier * 100f:F0}%</b></color> minimum."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=#00CED1><b>Armor</b></color> by <color=green><b>{armorpl}</b></color> per level. [<color=green><b>+{armorpl * effectivePath1Level}</b></color>]\n\n" +
               $"{Level5Section(path1Level, details ? "Increase Attack Damage by 33% of <color=#00CED1><b>Armor</b></color>." : $"Increase Attack Damage by <color=#00CED1><b>{armor * 0.33f:F0}</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float armorpl = AcornData?.path2DefensiveArmorPerLevel ?? 6f;
        float bashDmgPl = AcornData?.path2BashDamagePercentPerLevel ?? 0.10f;
        string desc = details
            ? $"{GetName()} regenerates <color=green><b>{BaseRegenPercent * 100f:F0}%</b></color> Health per second. After not being damaged for <color=green><b>{RegenBoostDelay:F0}s</b></color>, the regeneration increases to <color=green><b>{BoostedRegenPercent * 100f:F0}%</b></color> per second.\n\n" +
              $"If {GetName()} is being targeted by a Physical damaging insect, it changes into <color=orange><b>Guard Stance</b></color>, during which it gains <color=#00CED1><b>[({AcornData?.baseDefensiveArmor ?? 20f:F0}) + ({armorpl:F0}/Lvl.)]</b></color> Armor, while losing <color=green><b>{DefensiveAttackSpeedPenalty * 100f:F0}%</b></color> Attack Speed and <color=green><b>{DefensiveAttackRangePenalty * 100f:F0}%</b></color> Attack Range.\n\n" +
              $"While in <color=orange><b>Guard Stance</b></color>, its attacks become an omnidirectional Shield Bash dealing <color={PlantData.ElementalColor(elementalType)}><b>[({(AcornData?.baseBashDamagePercent ?? 0.5f) * 100f:F0}%) + ({bashDmgPl * 100f:F0}%/Lvl.)] Armor</b></color> {PlantData.DamageTypeLabel(damageType)}."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase guard <color=#00CED1><b>Armor</b></color> by <color=green><b>{armorpl:F0}</b></color> per level. [<color=green><b>+{armorpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Shield Bash</b></color> Armor scaling by <color=green><b>{bashDmgPl * 100f:F0}%</b></color> per level. [<color=green><b>+{bashDmgPl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"<color=green><b>Shield Bash</b></color> gains a <color=green><b>{MaxLevelBashStunChance * 100f:F0}%</b></color> chance to inflict a <color=green><b>{BashStunDuration:F0}</b></color> second Stun on insects hit.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl     = AcornData?.path3SkillDurationPerLevel ?? 2f;
        float hppl      = AcornData?.path3HealthPerLevel        ?? 50f;
        float durMP     = AcornData?.skillDurationMPMultiplier  ?? 0.10f;
        string skillMaxBonus = details
            ? "Whenever the <color=green><b>Shield</b></color> is healed, its lifetime is extended by 2% of the healing amount, in seconds.\n\nThe <color=green><b>Shield</b></color> inherits the Acorn Knight's <color=#00CED1><b>Armor</b></color>."
            : $"Whenever the <color=green><b>Shield</b></color> is healed, its lifetime is extended by 2% of the healing amount, in seconds.\n\nThe <color=green><b>Shield</b></color> gains <color=#00CED1><b>{armor:F0} Base Armor</b></color>.";
        float aspl = AcornData?.path3AttackSpeedBonusPerLevel ?? 0.05f;
        string desc = details
            ? $"The {GetName()} throws his shield, stopping at the first insect hit and dealing <color={PlantData.ElementalColor(elementalType)}><b>[{SkillDamageArmorPercent * 100f:F0}% Armor]</b></color> {PlantData.DamageTypeLabel(damageType)}, stunning it for <color=green><b>{ShieldStunDuration:F1}s</b></color>. " +
              $"The <color=green><b>Shield</b></color> then sits at the hit location for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> <color=#FFB6C1><b>[+{durMP * 100f:F0}% Magic Power]</b></color> seconds, blocking and taunting insects who stop at it. The <color=green><b>Shield</b></color> has <color=green><b>[({data.baseSkillHealth:F0}) + ({hppl:F0}/Lvl.)]</b></color> health.\n\n" +
              $"While the shield is out, {GetName()} cannot guard, loses <color=#00CED1><b>{AttackPathArmorBonus:F0} Armor</b></color>, and gains <color=green><b>[({(AcornData?.baseSkillAttackSpeedBonus ?? 0.2f) * 100f:F0}%) + ({aspl * 100f:F0}%/Lvl.)]</b></color> Attack Speed."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Shield</b></color> lifetime by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Shield</b></color> health by <color=green><b>{hppl:F0}</b></color> per level. [<color=green><b>+{hppl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Attack Speed</b></color> bonus by <color=green><b>{aspl * 100f:F0}%</b></color> per level. [<color=green><b>+{aspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, skillMaxBonus)}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
