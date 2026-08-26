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
    [SerializeField] private float fireConeTravelTime = 0.25f; // particle stream speed only: seconds for a particle to cross the attack range (lower = faster, shorter)
    [SerializeField] private float attackHitDelayTime = 0.25f; // damage timing only: seconds for a hit at the edge of attack range to land, scaled down by distance for closer targets

    private StargazerData SData => data as StargazerData;
    private Vector2 _facingDir = Vector2.right;
    private GameObject _skillIndicatorInstance;

    private float ConeAngle              => (SData?.coneAngle ?? 60f) + (IsPath1Maxed ? (SData?.path1MaxConeAngleBonus ?? 15f) : 0f);
    private float FlammableBonusPerStack => SData?.flammableBonusPerStack ?? 0.01f;
    private int   FlammableMaxStacks     => SData?.flammableMaxStacks ?? 5;
    private float FlammableDuration       => passiveDuration;
    private int   StacksPerHit           => (SData?.baseStacksPerHit ?? 1) + Mathf.RoundToInt((SData?.path2StacksPerLevel ?? 1) * effectivePath2Level);
    private float BurnDurationBonus       => (SData?.baseBurnDurationBonus ?? 0.5f) + (SData?.path2BurnDurationPerLevel ?? 0.1f) * effectivePath2Level;
    private float PassiveProcChance       => (SData?.passiveProcChance ?? 0.25f) + (SData?.path2ProcChancePerLevel ?? 0.05f) * effectivePath2Level;

    private float SkillLength       => SData?.skillLength ?? 50f;
    private float SkillWaveWidth    => (SData?.skillWaveWidth ?? 4f) + (SData?.path3WaveWidthPerLevel ?? 0.5f) * effectivePath3Level;
    private float SkillWaveRadius   => SkillWaveWidth * 0.5f;
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
        basePassiveDuration = 8f;
        Entity.OnEntityHit += OnAnyEntityHit;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Entity.OnEntityHit -= OnAnyEntityHit;
    }

    // covers every source of Fire damage this Stargazer causes: the cone attack, its own Burn
    // ticks, and the Fire Wave skill, not just the direct attack hit
    private void OnAnyEntityHit(EntityEventData data)
    {
        if (data.source != this || data.elementalType != ElementalType.Fire) return;
        if (data.target is Insect insect) ApplyFlammable(insect);
    }

    public override void UpdateStats()
    {
        float fireDamageBonus = IsPath1Maxed ? (SData?.path1MaxFireDamageBonus ?? 0.25f) : 0f;
        float eaBonus  = IsPath2Maxed ? (SData?.path2MaxElementalAffinityBonus     ?? 0.2f)  : 0f;
        float eecBonus = IsPath2Maxed ? (SData?.path2MaxElementalEffectChanceBonus ?? 0.06f) : 0f;
        if (IsPath1Maxed) attackDamageTotalMultiplier *= 0.67f;
        fireDamageAdder += fireDamageBonus;
        elementalAffinityAdder += eaBonus;
        elementalEffectChanceAdder += eecBonus;
        base.UpdateStats();
        if (IsPath1Maxed) attackDamageTotalMultiplier /= 0.67f;
        fireDamageAdder -= fireDamageBonus;
        elementalAffinityAdder -= eaBonus;
        elementalEffectChanceAdder -= eecBonus;
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
            case TARGETING.Nearest:   go = FindNearest(Insect.allInsects);   break;
            case TARGETING.Last:      go = FindLast(Insect.allInsects);      break;
            case TARGETING.Strongest: go = FindStrongest(Insect.allInsects); break;
            default:                  go = FindFirst(Insect.allInsects);    break;
        }
        return go != null ? go.GetComponent<Insect>() : null;
    }

    protected override void Attack()
    {
        base.Attack();   // resets the attack timer

        // _facingDir is kept current by UpdateFacing (tracks the target even out of range)
        float halfAngle = ConeAngle * 0.5f;

        // snapshot every target, its distance, and the damage it'll take before any of it
        // lands — same approach as Calendula's attack: each hit is delayed proportionally to
        // that snapshotted distance, using attackHitDelayTime (separate from fireConeTravelTime,
        // which only paces the continuous visual particle stream)
        float snapshotDamage = attackDamage;
        DamageType snapshotDamageType = damageType;
        ElementalType snapshotElementalType = elementalType;
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            Vector2 to = (Vector2)insect.transform.position - (Vector2)transform.position;
            if (to.magnitude > attackRange) continue;
            if (Vector2.Angle(_facingDir, to) > halfAngle) continue;

            float delay = attackRange > 0f ? Mathf.Clamp01(to.magnitude / attackRange) * attackHitDelayTime : 0f;
            StartCoroutine(DelayedAttackHit(insect, snapshotDamage, snapshotDamageType, snapshotElementalType, delay));
        }
    }

    private IEnumerator DelayedAttackHit(Insect insect, float damage, DamageType dmgType, ElementalType elemType, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (insect == null || !insect.IsAlive) yield break;
        insect.Damage(damage, dmgType, elemType, this, true,
            new DamageTag[] { DamageTag.AoE, DamageTag.Attack });
    }

    private ConeParticleEmitter _fireCone;

    // emission logic lives in the shared ConeParticleEmitter, see that class for details
    private void UpdateFireCone(bool active)
    {
        if (_fireCone == null) _fireCone = new ConeParticleEmitter(fireConeParticles);
        _fireCone.Update(active, _facingDir, ConeAngle, attackRange, fireConeTravelTime);
    }

    private void ApplyFlammable(Insect insect)
    {
        float procChance = PassiveProcChance * (1f + bonusEffectChance);
        if (Random.value >= procChance) return;
        AddFlammable(insect, StacksPerHit);
    }

    // adds `amount` Flammable stacks to the target (capped), refreshing duration.
    // public so the fire wave (skill) can apply stacks too
    public void AddFlammable(Insect insect, int amount)
    {
        if (insect == null || !insect.IsAlive || amount <= 0) return;
        FlammableEffect existing = insect.GetEffect<FlammableEffect>();
        int stacks = Mathf.Min((existing?.level ?? 0) + amount, FlammableMaxStacks);
        insect.ApplyEffect(new FlammableEffect(insect, FlammableDuration, stacks, this, FlammableBonusPerStack));
    }

    // guaranteed Burn applied by each Fire Wave hit, called after that hit's own damage is dealt.
    // public so the fire wave (skill) can apply it, same pattern as AddFlammable
    public void ApplySkillBurn(Insect insect)
    {
        if (insect == null || !insect.IsAlive) return;
        insect.ApplyEffect(new BurnEffect(insect, SData?.skillBurnDuration ?? 6f, 1, this));
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
            SkillWaveRadius,
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
        $"Dealing <color=orange><b>Fire Damage</b></color> has a <color=green><b>{PassiveProcChance * 100f:F0}%</b></color> chance to apply <color=green><b>{StacksPerHit}</b></color> stack{(StacksPerHit == 1 ? "" : "s")} of <color=#FF6B1A>Flammable</color> to the target for <color=green><b>{FlammableDuration:F0}s</b></color>, increasing the <color=orange>Burn</color> damage it takes by <color=green><b>{FlammableBonusPerStack * 100f:F0}%</b></color> per stack. <color=orange>Burn</color> effects caused by the {GetName()} last <color=green><b>{BurnDurationBonus * 100f:F0}%</b></color> longer.";

    public override string GetSkillDesription() =>
        $"Aim a direction. After a brief delay, a <color=#FF6B1A><b>Fire Wave</b></color> sweeps across the entire map, dealing <color=green><b>{(SData?.skillBaseDamage ?? 200f) + (SData?.path3SkillDamagePerLevel ?? 40f) * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} damage to everything in its path, applying <color=green><b>{SkillFlammableStacks}</b></color> stack{(SkillFlammableStacks == 1 ? "" : "s")} of <color=#FF6B1A>Flammable</color>. The wave deals <color=green><b>{(SkillBurnMultiplier - 1f) * 100f:F0}%</b></color> increased damage against <color=orange>Burning</color> targets.\n\nInflicts <color=orange>Burn</color> on targets hit.";

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
               $"{Level5Section(path1Level, $"Decrease <color=green><b>Total Attack Damage</b></color> by <color=red><b>33%</b></color>, but increase <color=orange><b>Fire Damage</b></color> by <color=green><b>{(SData?.path1MaxFireDamageBonus ?? 0.25f) * 100f:F0}%</b></color>. Increase <color=green><b>Cone Angle</b></color> by <color=green><b>{SData?.path1MaxConeAngleBonus ?? 15f:F0}°</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        int   spl  = SData?.path2StacksPerLevel       ?? 1;
        float bdpl = SData?.path2BurnDurationPerLevel  ?? 0.1f;
        float ppl  = SData?.path2ProcChancePerLevel    ?? 0.05f;
        string desc = details
            ? $"Dealing <color=orange><b>Fire Damage</b></color> has a <color=green><b>[({(SData?.passiveProcChance ?? 0.25f) * 100f:F0}%) + ({ppl * 100f:F0}%/Lvl.)]</b></color> chance to apply <color=green><b>[({SData?.baseStacksPerHit ?? 1}) + ({spl}/Lvl.)]</b></color> stack{(StacksPerHit == 1 ? "" : "s")} of <color=#FF6B1A>Flammable</color> to the target for <color=green><b>{FlammableDuration:F0}</b></color> seconds, increasing the <color=orange>Burn</color> damage it takes by <color=green><b>{FlammableBonusPerStack * 100f:F0}%</b></color> per stack. <color=orange>Burn</color> effects caused by the {GetName()} last <color=green><b>[({SData?.baseBurnDurationBonus ?? 0.5f:F0}%) + ({bdpl * 100f:F0}%/Lvl.)]</b></color> longer."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase Proc Chance by <color=green><b>{ppl * 100f:F0}%</b></color> per level. [<color=green><b>+{ppl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=#FF6B1A>Flammable</color> stacks applied per hit by <color=green><b>{spl}</b></color> per level. [<color=green><b>+{Mathf.RoundToInt(spl * effectivePath2Level)}</b></color>]\n\n" +
               $"Increase <color=orange>Burn</color> duration by <color=green><b>{bdpl * 100f:F0}%</b></color> per level. [<color=green><b>+{bdpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Increase <color=#FFD700><b>Elemental Affinity</b></color> by <color=green><b>{(SData?.path2MaxElementalAffinityBonus ?? 0.2f) * 100f:F0}%</b></color>, and <color=green><b>Elemental Effect Chance</b></color> by <color=green><b>{(SData?.path2MaxElementalEffectChanceBonus ?? 0.06f) * 100f:F0}%</b></color>.")}\n\n" +
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
            ? $"Aim a direction. After a brief delay, a <color=#FF6B1A><b>Fire Wave</b></color> sweeps across the entire map, dealing <color=green><b>[({SData?.skillBaseDamage ?? 200f:F0}) + ({dpl:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} damage to everything in its path, applying <color=green><b>[({SData?.skillFlammableStacks ?? 2}) + ({fpl:F1}/Lvl.)]</b></color> stacks of <color=#FF6B1A>Flammable</color>. The wave deals <color=green><b>[({SData?.skillBurnMultiplier ?? 2f:F1}) + ({bpl:F1}/Lvl.)]x</b></color> damage against <color=orange>Burning</color> targets.\n\nInflicts <color=orange>Burn</color> on targets hit."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=#FF6B1A>Fire Wave</color> damage by <color=green><b>{dpl:F0}</b></color> per level. [<color=green><b>+{dpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase <color=#FF6B1A>Fire Wave</color> radius by <color=green><b>{wpl * 0.5f:F1}</b></color> per level. [<color=green><b>+{wpl * 0.5f * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase <color=orange>Burning</color>-target bonus damage by <color=green><b>{bpl * 100f:F0}%</b></color> per level. [<color=green><b>+{bpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=#FF6B1A>Flammable</color> stacks applied by <color=green><b>{fpl:F1}</b></color> per level. [<color=green><b>+{Mathf.RoundToInt(fpl * effectivePath3Level)}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "The <color=#FF6B1A><b>Fire Wave</b></color> returns after reaching its end, sweeping back in the opposite direction.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
