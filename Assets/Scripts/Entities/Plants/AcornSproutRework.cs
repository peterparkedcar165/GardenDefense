using UnityEngine;
using System.Collections.Generic;

// placeholder clone of AcornSprout, used as a scratch space for the Acorn Knight kit rework -
// see AcornSproutReworkData/AcornSproutReworkSkillTree for the matching cloned data/skill tree
// assets, and AcornSproutRework.prefab for the placed plant. not the live plant yet
public class AcornSproutRework : Aura
{
    [SerializeField] private Transform bodyVisual;
    [SerializeField] private Transform swordVisual;
    [SerializeField] private Transform shieldVisual;
    [SerializeField] private GameObject shieldProjectilePrefab;
    [SerializeField] private GameObject shieldPrefab;

    private AcornSproutReworkData AcornData => data as AcornSproutReworkData;

    // fixed constant, not a stat: no node or piercing stat may ever raise this
    private const int ConeMaxTargets = 5;
    private const float SwingArcDegrees = 60f;
    private const float SwingDuration = 0.18f;

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
    private float shieldHealth, shieldRadius, shieldFlatDamage;
    private AcornSproutReworkShield _activeShield;

    // skill tree node unlock ids, kept from the old kit so existing skillPurchases stay valid
    public const string StunSpecialistUnlock = "acorn_stun_specialist";
    public const string PiercerUnlock        = "acorn_piercer";
    public const string InstantSkillUnlock   = "acorn_instant_skill";

    public float ConeFalloffPerTarget   => AcornData?.coneFalloffPerTarget   ?? 0.10f;
    public float AttackWindupTime       => AcornData?.attackWindupTime       ?? 0.1f;
    public float StanceExitDelay        => AcornData?.stanceExitDelay        ?? 2.5f;
    public float ThrowRangeMultiplier   => AcornData?.throwRangeMultiplier   ?? 1.5f;
    public float ShieldThrowSpeed       => AcornData?.shieldThrowSpeed       ?? 10f;
    public float ShieldStunDuration     => AcornData?.shieldStunDuration     ?? 1.5f;
    public float SkillAttackSpeedBonus  => AcornData?.skillAttackSpeedBonus  ?? 0.4f;
    public float ShieldReequipDelay     => AcornData?.shieldReequipDelay     ?? 1f;

    public float DefensiveArmor =>
        (AcornData?.baseDefensiveArmor ?? 20f) + (AcornData?.path2DefensiveArmorPerLevel ?? 6f) * effectivePath2Level;
    public float DefensiveAttackSpeedPenalty => AcornData?.defensiveAttackSpeedPenalty ?? 0.3f;
    public float DefensiveAttackRangePenalty => AcornData?.defensiveAttackRangePenalty ?? 0.25f;

    // toggle target like any Shooter (Nearest/First/Last/Strongest), same UI affordance
    public override bool UsesTargeting => true;

    // shield lifetime scales with Magic Power - a raw-seconds bonus, so no /100 division (see
    // feedback_magic_power_scaling memory: only decimal-percentage stats divide)
    public float SkillDurationMP => (AcornData?.skillDurationMPMultiplier ?? 0.10f) * magicPower;

    public bool IsDefensiveStance => !_shieldOut && _stanceExitTimer > 0f;

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
    }

    protected override void Update()
    {
        base.Update(); // Aura sets attackCooldown = 1 / attackSpeed

        TickTimers(Time.deltaTime);

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

    private void UpdateFacingVisual()
    {
        if (bodyVisual != null && Mathf.Abs(_facingDir.x) > 0.01f)
        {
            Vector3 scale = bodyVisual.localScale;
            scale.x = Mathf.Abs(scale.x) * (_facingDir.x < 0f ? -1f : 1f);
            bodyVisual.localScale = scale;
        }

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
            else      SwingSword(target);
        });
    }

    // semicircle: whichever side (left or right) the target is on, relative to the Knight's own
    // position. the target always counts as one of the up to 5 hits; up to 4 more of the nearest
    // other insects on that same side, within range, fill the rest. same falloff as before
    private void SwingSword(Insect target)
    {
        Vector2 targetApproach = target.GetApproachPoint(transform.position);
        float facingSign = Mathf.Sign(targetApproach.x - transform.position.x);
        if (facingSign == 0f) facingSign = 1f;

        List<Insect> others = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect == target) continue;
            Vector2 approach = insect.GetApproachPoint(transform.position);
            Vector2 to = approach - (Vector2)transform.position;
            if (to.magnitude > attackRange) continue;
            if (Mathf.Sign(to.x == 0f ? facingSign : to.x) != facingSign) continue;
            others.Add(insect);
        }
        others.Sort((a, b) => Vector2.Distance(transform.position, a.GetApproachPoint(transform.position))
            .CompareTo(Vector2.Distance(transform.position, b.GetApproachPoint(transform.position))));

        List<Insect> candidates = new List<Insect> { target };
        int extra = Mathf.Min(others.Count, ConeMaxTargets - 1);
        for (int i = 0; i < extra; i++) candidates.Add(others[i]);

        float multiplier = Mathf.Max(0f, 1f - ConeFalloffPerTarget * (candidates.Count - 1));
        float damage = attackDamage * multiplier;

        foreach (Insect insect in candidates)
            insect.Damage(damage, damageType, elementalType, this, false, new DamageTag[] { DamageTag.Melee, DamageTag.Attack });
    }

    // omnidirectional: the same target as the sword swing, plus the 4 insects closest to THAT
    // target (not to the Knight), as long as they're still within the Knight's own attack range.
    // same 5-target cap and 10%-per-extra falloff as the swing, but scales off Armor instead of
    // Attack Damage, and is tagged as counter damage
    private void Bash(Insect target)
    {
        Vector2 targetApproach = target.GetApproachPoint(transform.position);

        List<Insect> others = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect == target) continue;
            Vector2 approach = insect.GetApproachPoint(transform.position);
            if (Vector2.Distance(transform.position, approach) > attackRange) continue;
            others.Add(insect);
        }
        others.Sort((a, b) => Vector2.Distance(targetApproach, a.GetApproachPoint(transform.position))
            .CompareTo(Vector2.Distance(targetApproach, b.GetApproachPoint(transform.position))));

        List<Insect> candidates = new List<Insect> { target };
        int extra = Mathf.Min(others.Count, ConeMaxTargets - 1);
        for (int i = 0; i < extra; i++) candidates.Add(others[i]);

        float multiplier = Mathf.Max(0f, 1f - ConeFalloffPerTarget * (candidates.Count - 1));
        float damage = armor * multiplier;

        foreach (Insect insect in candidates)
            insect.Damage(damage, damageType, elementalType, this, false, new DamageTag[] { DamageTag.Melee, DamageTag.Attack, DamageTag.Counter });
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

        attackSpeedTotalMultiplier += speedBonus;
        attackRangeTotalMultiplier += rangeMultiplierDelta;
        base.UpdateStats();
        attackSpeedTotalMultiplier -= speedBonus;
        attackRangeTotalMultiplier -= rangeMultiplierDelta;

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
        float flatDmgPerLevel = AcornData?.path3FlatDamagePerLevel    ?? 30f;
        float durPerLevel     = AcornData?.path3SkillDurationPerLevel ?? 2f;
        float hpPerLevel      = AcornData?.path3HealthPerLevel        ?? 50f;
        float radiusPerLevel  = AcornData?.path3RadiusPerLevel        ?? 0.15f;

        shieldFlatDamage  = flatDmgPerLevel * level;
        baseSkillDuration = data.baseSkillDuration + durPerLevel * level;
        shieldHealth      = data.baseSkillHealth   + hpPerLevel  * level;
        shieldRadius      = data.baseSkillRadius   * (1f + radiusPerLevel * level);
    }

    // line skill shot: player aims a point (clamped to throw range), the shield is fired
    // straight at it and stops at the first insect it touches (ShieldThrowProjectile has no
    // piercing), or at the clamped point itself if it hits nothing
    public override void ActivateSkill()
    {
        if (!SkillReady || _shieldOut) return;
        float throwRange = attackRange * ThrowRangeMultiplier;
        SkillTargetingManager.instance.BeginTargeting(throwRange, OnTargetConfirmed, transform.position, throwRange);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (shieldProjectilePrefab == null) return;

        float throwRange = attackRange * ThrowRangeMultiplier;
        Vector2 toTarget = (Vector2)position - (Vector2)transform.position;
        Vector2 landPos = toTarget.magnitude > throwRange
            ? (Vector2)transform.position + toTarget.normalized * throwRange
            : (Vector2)position;

        float damage = attackDamage + shieldFlatDamage;

        GameObject obj = Instantiate(shieldProjectilePrefab, transform.position, Quaternion.identity);
        ShieldThrowProjectile proj = obj.GetComponent<ShieldThrowProjectile>();
        proj?.Initialize(landPos, damage, damageType, elementalType, ShieldThrowSpeed, this, OnShieldLanded);

        SetFacing(landPos);
        _shieldOut = true;
        _shieldSwingTimer = SwingDuration;
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
        AcornSproutReworkShield shield = obj.GetComponent<AcornSproutReworkShield>();
        if (shield == null) { _shieldOut = false; return; }

        shield.Initialize(shieldRadius, shieldHealth, skillDuration, this);
        _activeShield = shield;
        shield.OnShieldGone += HandleShieldGone;
    }

    // cooldown starts only once the shield is actually gone (destroyed or expired), never on
    // toggle or cast, then the re-equip delay holds the Knight in the shield-out state a little
    // longer before it can enter defensive stance or lose the skill's attack speed bonus
    private void HandleShieldGone()
    {
        if (_activeShield != null) _activeShield.OnShieldGone -= HandleShieldGone;
        _activeShield = null;
        skillCooldownTimer = skillCooldown;
        _reequipTimer = ShieldReequipDelay;
    }

    public override string GetName() => $"<b><color=green>{(data != null ? data.displayName : "Acorn Knight")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a melee tank with a sword and shield, raising its guard under pressure and able to hurl its shield to stun a target and block the path.";

    public override string GetAttackDescription() =>
        $"Swings its sword in a semicircle toward its target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to up to <color=green><b>{ConeMaxTargets}</b></color> insects: its target plus the nearest others on that side. Each insect beyond the first reduces the swing's damage on every target hit by <color=green><b>{ConeFalloffPerTarget * 100f:F0}%</b></color>.";

    public override string GetPassiveDescription() =>
        $"While an insect is currently targeting {GetName()} to deal Physical damage, it raises its guard: gaining <color=#00CED1><b>+{DefensiveArmor:F0} Armor</b></color>, losing <color=green><b>{DefensiveAttackSpeedPenalty * 100f:F0}%</b></color> Attack Speed, and losing <color=green><b>{DefensiveAttackRangePenalty * 100f:F0}%</b></color> Attack Range. " +
        $"While guarding, its attack becomes an omnidirectional <color=green><b>Shield Bash</b></color> that scales with <color=#00CED1><b>Armor</b></color> instead of Attack Damage, hitting its target plus the up to <color=green><b>{ConeMaxTargets - 1}</b></color> insects closest to that target, within its own attack range. " +
        $"The guard drops <color=green><b>{StanceExitDelay:F1}s</b></color> after no insect is targeting it that way anymore.";

    public override string GetSkillDesription() =>
        $"Fires the shield in a line toward the targeted point, up to <color=green><b>{ThrowRangeMultiplier:F1}x</b></color> melee range. It stops at the first insect hit, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> [<color=green><b>+{shieldFlatDamage:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} and stunning it for <color=green><b>{ShieldStunDuration:F1}s</b></color>. " +
        $"The shield then sits at the hit location for <color=green><b>{skillDuration:F1}</b></color> [<color=#FFB6C1><b>+{SkillDurationMP:F1}</b></color>] seconds, blocking and taunting insects, with <color=green><b>{shieldHealth:F0}</b></color> health. " +
        $"While the shield is out, {GetName()} cannot guard, loses its guard's armor bonus, and gains <color=green><b>+{SkillAttackSpeedBonus * 100f:F0}%</b></color> Attack Speed.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = AcornData?.path1AttackDamagePerLevel ?? 8f;
        float aspl = AcornData?.path1AttackSpeedPerLevel  ?? 0.05f;
        int   armorpl = AcornData?.path1ArmorPerLevel     ?? 4;
        string desc = details
            ? $"Swings its sword in a semicircle toward its target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} to up to <color=green><b>{ConeMaxTargets}</b></color> insects (its target plus the nearest others on that side), with <color=green><b>{ConeFalloffPerTarget * 100f:F0}%</b></color> falloff per extra insect hit."
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
        string desc = details
            ? $"While an insect is targeting {GetName()} to deal Physical damage, it raises its guard, gaining <color=#00CED1><b>[({AcornData?.baseDefensiveArmor ?? 20f:F0}) + ({armorpl:F0}/Lvl.)]</b></color> Armor, losing <color=green><b>{DefensiveAttackSpeedPenalty * 100f:F0}%</b></color> Attack Speed, and losing <color=green><b>{DefensiveAttackRangePenalty * 100f:F0}%</b></color> Attack Range. " +
              $"Its attack becomes an omnidirectional Shield Bash scaling with <color=#00CED1><b>Armor</b></color> instead of Attack Damage, hitting its target plus the up to <color=green><b>{ConeMaxTargets - 1}</b></color> insects closest to that target within its own attack range, with the same falloff as the sword swing but no stun. The guard drops <color=green><b>{StanceExitDelay:F1}s</b></color> after no insect is targeting it that way anymore."
            : GetPassiveDescription();
        // path 2's old max bonus (piercing bounce) no longer applies to a melee kit - pending a
        // wide-arc-vs-bash-focused redesign (TREE PENDING), left honest here in the meantime
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase guard <color=#00CED1><b>Armor</b></color> by <color=green><b>{armorpl:F0}</b></color> per level. [<color=green><b>+{armorpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, "Max Level bonus pending redesign for the new kit.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float flatDmgPl = AcornData?.path3FlatDamagePerLevel    ?? 30f;
        float durpl     = AcornData?.path3SkillDurationPerLevel ?? 2f;
        float hppl      = AcornData?.path3HealthPerLevel        ?? 50f;
        float radiuspl  = AcornData?.path3RadiusPerLevel        ?? 0.15f;
        float durMP     = AcornData?.skillDurationMPMultiplier  ?? 0.10f;
        string skillMaxBonus = details
            ? "Whenever the <color=green><b>Shield</b></color> is healed, its lifetime is extended by 2% of the healing amount, in seconds.\n\nThe <color=green><b>Shield</b></color> inherits the Acorn Knight's <color=#00CED1><b>Armor</b></color>."
            : $"Whenever the <color=green><b>Shield</b></color> is healed, its lifetime is extended by 2% of the healing amount, in seconds.\n\nThe <color=green><b>Shield</b></color> gains <color=#00CED1><b>{armor:F0} Base Armor</b></color>.";
        string desc = details
            ? $"Fires the shield in a line toward the targeted point, up to <color=green><b>{ThrowRangeMultiplier:F1}x</b></color> melee range, stopping at the first insect hit and dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> [<color=green><b>+{flatDmgPl:F0}/Lvl.</b></color>] {PlantData.DamageTypeLabel(damageType)}, stunning it for <color=green><b>{ShieldStunDuration:F1}s</b></color>. " +
              $"The <color=green><b>Shield</b></color> then sits at the hit location for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> <color=#FFB6C1><b>[+{durMP * 100f:F0}% Magic Power]</b></color> seconds, blocking and taunting insects who stop at it. The <color=green><b>Shield</b></color> has <color=green><b>[({data.baseSkillHealth:F0}) + ({hppl:F0}/Lvl.)]</b></color> health."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase impact damage by <color=green><b>{flatDmgPl:F0}</b></color> per level. [<color=green><b>+{flatDmgPl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Shield</b></color> lifetime by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Shield lifetime scaling: <color=#FFB6C1><b>{durMP * 100f:F0}%</b></color> Magic Power. [<color=#FFB6C1><b>+{SkillDurationMP:F1}s</b></color>]\n\n" +
               $"Increase <color=green><b>Shield</b></color> health by <color=green><b>{hppl:F0}</b></color> per level. [<color=green><b>+{hppl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Shield</b></color> taunt radius by <color=green><b>{radiuspl * 100f:F0}%</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, skillMaxBonus)}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
