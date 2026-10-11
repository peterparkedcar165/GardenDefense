using UnityEngine;
using System.Collections.Generic;

public class Snowdrop : Shooter
{
    // permanent children of this prefab (dragged in, starts inactive) - toggled active/inactive
    // and driven every frame from Update, rather than instantiated/destroyed per shot. iceBeam2
    // only ever lights up once Path1 max is granting a second shot while the skill is active
    [SerializeField] private IceBeam iceBeam;
    [SerializeField] private IceBeam iceBeam2;

    private SnowdropData SData => data as SnowdropData;

    public float SnowMarkFlatDamage => (SData?.snowMarkFlatDamageBase ?? 12f) + (SData?.snowMarkFlatDamagePerLevel ?? 6f) * effectivePath2Level;
    // snowMarkPercentAdder is fed by the Chilling Shot skill tree node (StatType.SnowMarkPercentFlat)
    public float snowMarkPercentAdder;
    public float SnowMarkPercent   => (SData?.snowMarkDamagePercentBase ?? 0.02f) + (SData?.snowMarkDamagePercentPerLevel ?? 0.01f) * effectivePath2Level + snowMarkPercentAdder;
    public float SnowMarkDuration  => SData?.snowMarkDuration ?? 6f;
    private float IceBeamAttackSpeed => (SData?.iceBeamAttackSpeed ?? 3f) + (SData?.iceBeamAttackSpeedPerLevel ?? 0.4f) * effectivePath3Level;
    // Path3 max: bonus Attack Speed granted to the bonded partner while the Ice Beam is active
    public float PartnerHasteBonus => SData?.path3MaxBondedPartnerHasteBonus ?? 0.15f;

    private static readonly DamageTag[] bonusTags = { DamageTag.Coordinated, DamageTag.OnHit, DamageTag.NoPrimer, DamageTag.PassiveDamage };

    // skill tree unlock ids
    public const string ChillingGraspUnlock = "snowdrop_chilling_grasp";
    public const string InstantSkillUnlock  = "snowdrop_instant_skill";
    public const string SecondTargetUnlock  = "snowdrop_second_target";
    public const string AssistRefundUnlock  = "snowdrop_assist_refund";
    public const string MarkCritUnlock      = "snowdrop_mark_crit";

    // Twin Shot (skill tree): grants the same bonus second shot Path1 max does, independently of
    // her actual Path1 level
    private bool HasSecondTarget => IsPath1Maxed || SkillTreeManager.HasUnlock(this, SecondTargetUnlock);

    // the generic effect system ages/expires this on its own - no separate timer needed
    public bool IceBeamActive => HasEffect<IceBeamEffect>();

    // Snow Bond: a permanent link to any plant on the field, resolved live from the tile it sits
    // on (like Calendula's auto-cast target / Carrot's Soil Bond) so a bonded plant that dies and
    // revives - a brand new instance - gets automatically picked back up instead of leaving a
    // stale link
    private Tile boundTile;
    private Plant boundPartner;
    private Plant _bondHighlighted;
    private static readonly Color BondHighlightColor = new Color(0.4f, 0.9f, 1f);

    public Plant BoundPartner => boundPartner;

    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => boundTile != null;
    public override string AutoCastLabel => "Bond";

    // Path2 max: insects visible from the bonded plant's attack radius are also visible to
    // this Snowdrop - mirrors Carrot's Soil Bond range-sharing pattern exactly
    protected override bool IsWithinAttackRange(Insect insect, float distance)
    {
        if (distance <= attackRange) return true;
        return IsPath2Maxed && boundPartner != null && boundPartner.IsAlive && boundPartner.CanReachInsect(insect);
    }

    // tracked ourselves rather than read back off iceBeam.gameObject.activeSelf, so a stale or
    // unexpected read of Unity's own active-state can never silently skip the SetActive call
    private bool _beamWasActive;
    private bool _beam2WasActive;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        if (iceBeam != null) iceBeam.gameObject.SetActive(false);
        if (iceBeam2 != null) iceBeam2.gameObject.SetActive(false);
        _beamWasActive = false;
        _beam2WasActive = false;

        // free skill readiness on placement - deliberately bypasses UnlockPath3() (which spends
        // sun and adds to totalSunSpent) so this can't be abused for an inflated uproot refund
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }

        Entity.OnEntityKilled += OnAnyEntityKilled;
    }

    // Lingering Frost (skill tree): scoring an assist (she hit the insect at some point, but
    // didn't land the killing blow herself) while the Skill is active refunds 1 second of the
    // Ice Beam's remaining duration
    private void OnAnyEntityKilled(EntityEventData data)
    {
        if (!IceBeamActive) return;
        if (!SkillTreeManager.HasUnlock(this, AssistRefundUnlock)) return;
        if (data.source == this) return;
        if (data.target is not Insect insect || !insect.attackerSet.Contains(this)) return;
        IceBeamEffect beam = GetEffect<IceBeamEffect>();
        if (beam != null) beam.duration += 1f;
    }

    protected override void Update()
    {
        base.Update();

        // strictly both: the skill has to be active AND she has to actually be attacking right
        // now (a valid target, and not stunned/channeling - the same conditions Shooter.Update
        // itself requires before it'll call Shoot at all) - fails either one and it's inactive,
        // no partial state. toggled every frame rather than only on a shot, so it
        // appears/disappears the instant either condition changes. SetActive is called
        // unconditionally every frame (cheap/idempotent in Unity) rather than guarded behind a
        // "did it change" check, so there's no way for that guard to end up skipping it
        bool isAttacking = !IsStunned && !IsChanneling;
        GameObject currentTarget = (IceBeamActive && isAttacking) ? FindTarget() : null;
        bool beamShouldBeActive = currentTarget != null;
        if (iceBeam != null)
        {
            iceBeam.gameObject.SetActive(beamShouldBeActive);

            if (beamShouldBeActive)
            {
                Insect targetInsect = currentTarget.GetComponent<Insect>();
                Vector3 aimPoint = targetInsect != null ? targetInsect.GetAimPoint() : currentTarget.transform.position;
                // just turned on this frame - snap to the target instantly rather than smoothly
                // sweeping in from whatever direction it was left facing last time
                if (!_beamWasActive) iceBeam.SnapToTarget(aimPoint);
                // capped to attackRange, not maxRange (Shooter's own maxRange is a separate
                // "furthest a projectile can travel" stat that can run longer than her actual
                // targeting range) - the beam should never reach further than she can actually see.
                // Path2 max removes that cap entirely (Mathf.Infinity, not just the current
                // target's own distance - otherwise anything standing behind that target would
                // get excluded from the piercing scan even though piercing should still reach it)
                float beamMaxRange = IsPath2Maxed ? Mathf.Infinity : attackRange;
                iceBeam.UpdateBeam(aimPoint, beamMaxRange);
            }
            _beamWasActive = beamShouldBeActive;
        }

        // Path1 max (or Twin Shot): the bonus second shot gets its own beam, aimed at whatever
        // the next-most-valid target is (excluding whatever the primary beam is on) - only ever
        // lit up while the primary beam is too, since there's no secondary shot without a primary one
        GameObject secondaryTarget = (HasSecondTarget && beamShouldBeActive) ? FindTarget(excluding: currentTarget) : null;
        bool beam2ShouldBeActive = secondaryTarget != null;
        if (iceBeam2 != null)
        {
            iceBeam2.gameObject.SetActive(beam2ShouldBeActive);

            if (beam2ShouldBeActive)
            {
                Insect targetInsect2 = secondaryTarget.GetComponent<Insect>();
                Vector3 aimPoint2 = targetInsect2 != null ? targetInsect2.GetAimPoint() : secondaryTarget.transform.position;
                if (!_beam2WasActive) iceBeam2.SnapToTarget(aimPoint2);
                float beam2MaxRange = IsPath2Maxed ? Mathf.Infinity : attackRange;
                iceBeam2.UpdateBeam(aimPoint2, beam2MaxRange);
            }
            _beam2WasActive = beam2ShouldBeActive;
        }

        ResyncBond();
        UpdateBondHighlight();
    }

    public override void UpdateStats()
    {
        base.UpdateStats();

        // fixed Attack Speed while the beam is active - a hard override applied after every
        // other source has already fed into the base calculation above, so nothing (fertilizers,
        // family passives, skill-tree nodes) can raise or lower it for as long as this lasts
        if (IceBeamActive) attackSpeed = IceBeamAttackSpeed;
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject primaryTarget = FindTarget();
        FireAttack(target, primaryTarget);

        // Path1 max (or Twin Shot): a second hit lands on whatever the next-most-valid target is
        // (same targeting rule as the primary shot, just excluding it), rather than hitting the
        // same insect twice
        if (!HasSecondTarget) return;
        GameObject secondaryTarget = FindTarget(excluding: primaryTarget);
        if (secondaryTarget == null) return;

        if (IceBeamActive)
        {
            // the bonus shot gets its own persistent beam (iceBeam2, driven every frame from
            // Update just like the primary one) - damage goes through it the same way FireAttack
            // routes the primary hit through iceBeam, so it pierces/hits whatever it's currently
            // touching rather than just the one insect FindTarget happened to pick this frame
            iceBeam2?.DealDamage(attackDamage, damageType, elementalType, this);
            return;
        }

        FireAttack(PredictTargetPosition(secondaryTarget), secondaryTarget);
    }

    private void FireAttack(Vector3 target, GameObject targetObj)
    {
        if (IceBeamActive)
        {
            iceBeam?.DealDamage(attackDamage, damageType, elementalType, this);
            return;
        }

        if (projectilePrefab == null) return;
        GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Projectile proj = obj.GetComponent<Projectile>();
        if (proj == null) return;

        proj.SetTarget(targetObj);
        proj.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
    }

    // same selection rule as the plant's normal targeting, just run over a pool that leaves out
    // one specific insect (the shot already aimed at it this attack)
    private GameObject FindTarget(GameObject excluding)
    {
        List<Insect> pool = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || insect.gameObject == excluding) continue;
            pool.Add(insect);
        }

        return targeting switch
        {
            TARGETING.Nearest   => FindNearest(pool),
            TARGETING.Last      => FindLast(pool),
            TARGETING.Strongest => FindStrongest(pool),
            _                   => FindFirst(pool)
        };
    }

    // called by SnowdropProjectile/IceBeamProjectile right after their own direct-damage hit lands.
    // a target not already carrying this Snowdrop's own Snow Mark takes the bonus % Max Health
    // hit and gets marked; one that already does just has its mark refreshed instead
    public void OnIceAttackHit(Insect insect)
    {
        if (insect == null || !insect.IsAlive) return;

        SnowMarkEffect existing = insect.GetEffect<SnowMarkEffect>();
        if (existing != null && existing.source == this)
        {
            insect.ApplyEffect(new SnowMarkEffect(insect, SnowMarkDuration, 1, this));
            return;
        }

        float bonus = SnowMarkFlatDamage + insect.maxHealth * SnowMarkPercent;
        insect.Damage(bonus, DamageType.Magic, ElementalType.Ice, this, SnowMarkCanCrit, bonusTags);
        if (!insect.IsAlive) return;

        insect.ApplyEffect(new SnowMarkEffect(insect, SnowMarkDuration, 1, this));
    }

    // Shattering Mark (skill tree): Snow Mark damage (both the initial application above and the
    // bonded partner's detonation below) can't critically strike by default - only this unlock
    // allows it
    private bool SnowMarkCanCrit => SkillTreeManager.HasUnlock(this, MarkCritUnlock);

    // Snow Bond: dealing the SAME bonus % Max Health hit, sourced from this Snowdrop, whenever
    // the bonded partner lands an Attack, PassiveDamage, or SkillDamage hit on the insect
    // currently carrying her Snow Mark - and only hers (see OnIceAttackHit). consumes the mark
    // either way the detonation resolves. subscribed to OnEntityHit rather than OnHit since OnHit
    // only ever fires for DamageTag.Attack hits, and PassiveDamage/SkillDamage-only hits (no
    // Attack tag) still need to qualify here
    private static readonly DamageTag[] detonatingTags = { DamageTag.Attack, DamageTag.PassiveDamage, DamageTag.SkillDamage };

    private void HandleBoundPartnerHit(EntityEventData data)
    {
        if (data.source != boundPartner) return;
        if (data.tags == null || !System.Array.Exists(data.tags, t => System.Array.IndexOf(detonatingTags, t) >= 0)) return;
        if (data.target is not Insect insect || !insect.IsAlive) return;

        SnowMarkEffect mark = insect.GetEffect<SnowMarkEffect>();
        if (mark == null || mark.source != this) return;

        // detonating with an Attack specifically (as opposed to a Passive/Skill hit) doubles
        // the % Max Health portion of the bonus - the flat portion is unaffected
        bool isAttack = System.Array.Exists(data.tags, t => t == DamageTag.Attack);
        float percentMultiplier = isAttack ? 2f : 1f;
        float bonus = SnowMarkFlatDamage + insect.maxHealth * SnowMarkPercent * percentMultiplier;
        insect.Damage(bonus, DamageType.Magic, ElementalType.Ice, this, SnowMarkCanCrit, bonusTags);
        insect.RemoveEffect<SnowMarkEffect>();
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        ApplyEffect(new IceBeamEffect(this, skillDuration, 1, this));
        UpdateStats();
    }

    // click Bond to pick any plant on the field to link with, click again to break the bond.
    // clicking itself is silently rejected and stays in targeting mode - spamming clicks on an
    // invalid plant never confirms anything
    public override void ToggleAutoCast()
    {
        if (boundTile != null)
        {
            boundTile = null;
            return;
        }
        SkillTargetingManager.instance.BeginPlantTargeting(OnBondTargetConfirmed, this);
    }

    private void OnBondTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return; // cancelled
        // Snow Bond is exclusive: can't bond another Snowdrop, and a plant already bonded to
        // a DIFFERENT Snowdrop can't be bonded again until that bond is released
        if (targetPlant == this || targetPlant is Snowdrop || IsBoundByAnotherSnowdrop(targetPlant))
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnBondTargetConfirmed, this);
            return;
        }
        boundTile = targetPlant.occupiedTile;
    }

    private bool IsBoundByAnotherSnowdrop(Plant candidate)
    {
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == this || plant is not Snowdrop other) continue;
            if (other.BoundPartner == candidate) return true;
        }
        return false;
    }

    // re-resolves whichever plant currently occupies boundTile every frame (cheap: a reference
    // compare, only doing real work when the occupant actually changed), keeps the
    // Entity.OnEntityHit subscription pointed at that live instance, and keeps its
    // forcedPriorityTarget synced to whichever insect currently carries this Snowdrop's own
    // Snow Mark (if any)
    private void ResyncBond()
    {
        Plant current = boundTile != null ? Plant.GetPlantOnTile(boundTile) : null;

        bool boundPartnerGone = boundPartner == null;
        if (boundPartnerGone || current != boundPartner)
        {
            if (!boundPartnerGone) Unbind();
            boundPartner = current;
            if (boundPartner != null)
            {
                Entity.OnEntityHit += HandleBoundPartnerHit;
                boundPartner.ApplyEffect(new SnowBondHasteEffect(boundPartner, this, this));
            }
        }

        if (boundPartner != null)
            boundPartner.forcedPriorityTarget = FindCurrentMark();
    }

    // at most one insect can carry this Snowdrop's own mark at a time (SnowMarkEffect is not
    // source-stackable), so a plain scan is enough - no need to track it separately
    private Insect FindCurrentMark()
    {
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            SnowMarkEffect mark = insect.GetEffect<SnowMarkEffect>();
            if (mark != null && mark.source == this) return insect;
        }
        return null;
    }

    private void Unbind()
    {
        if (boundPartner == null) return;
        Entity.OnEntityHit -= HandleBoundPartnerHit;
        if (boundPartner.forcedPriorityTarget != null) boundPartner.forcedPriorityTarget = null;
        boundPartner.RemoveEffect<SnowBondHasteEffect>(this);
        boundPartner = null;
    }

    // while this Snowdrop is selected, outline its bonded partner in icy blue and show its range
    // circle too, so it's obvious at a glance which plant is linked (mirrors Calendula/Carrot)
    private void UpdateBondHighlight()
    {
        Plant desired = IsSelected ? boundPartner : null;
        if (_bondHighlighted != null && _bondHighlighted != desired)
        {
            _bondHighlighted.ClearHighlight();
            _bondHighlighted.ShowExternalRangeCircle(false);
        }
        if (desired != null)
        {
            desired.SetHighlight(BondHighlightColor);
            desired.ShowExternalRangeCircle(true);
        }
        _bondHighlighted = desired;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Entity.OnEntityKilled -= OnAnyEntityKilled;
        Unbind();
        _bondHighlighted?.ClearHighlight();
        _bondHighlighted?.ShowExternalRangeCircle(false);
    }

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = boundTile != null, targetTile = boundTile };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled || state.targetTile == null) return;
        boundTile = state.targetTile;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (SData?.path1AttackDamagePerLevel ?? 3f) * level;
        baseAttackSpeed  = data.baseAttackSpeed  + (SData?.path1AttackSpeedPerLevel  ?? 0.05f) * level;
        baseAttackRange  = data.baseAttackRange  + (SData?.path1AttackRangePerLevel  ?? 0.2f)  * level;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (SData?.iceBeamDurationPerLevel ?? 1f) * level;
    }

    public override string GetName() => "<b><color=#00FFFF>Snowdrop</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a frosty flower who marks her prey for her bonded partner to finish off.";

    public override string GetAttackDescription() =>
        $"Fires a magical snowball at her target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        $"Bond with a plant, excluding other <color=#00FFFF><b>Snowdrops</b></color> or other plants already bound.\n\n" +
        $"Attacks against unmarked targets deal <color=#00FFFF><b>{SnowMarkFlatDamage:F0}</b></color> + <color=#00FFFF><b>{SnowMarkPercent * 100f:F1}%</b></color> of its <color=red><b>Max Health</b></color> as {PlantData.DamageTypeLabel(damageType)}, and apply <color=#00FFFF><b>Snow Mark</b></color> for <color=green><b>{SnowMarkDuration:F0}s</b></color>.\n\n" +
        $"The bonded plant will target at highest priority the <color=#00FFFF><b>Snow Marked</b></color> insect. Direct damage inflicted by the bonded plant against the <color=#00FFFF><b>Marked</b></color> insect detonates the <color=#00FFFF><b>Snow Mark</b></color>, dealing <color=#00FFFF><b>{SnowMarkFlatDamage:F0}</b></color> + <color=#00FFFF><b>{SnowMarkPercent * 100f:F1}%</b></color> of the target's <color=red><b>Max Health</b></color> as {PlantData.DamageTypeLabel(damageType)}. If the detonation was dealt by an <color=#00FFFF><b>Attack</b></color>, deal <color=#00FFFF><b>{SnowMarkPercent * 200f:F1}%</b></color> <color=red><b>Max Health</b></color> instead.";

    public override string GetSkillDesription() =>
        $"{GetName()}'s attacks become an <color=#00FFFF><b>Ice Beam</b></color> for <color=green><b>{skillDuration:F0}s</b></color>, within which her Attack Speed is fixed at <color=green><b>{IceBeamAttackSpeed:F1}</b></color>.";

    public override string GetPath1Name() => "Frost Shot";
    public override string GetPath2Name() => "Snow Mark";
    public override string GetPath3Name() => "Ice Beam";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = SData?.path1AttackDamagePerLevel ?? 3f;
        float aspl = SData?.path1AttackSpeedPerLevel ?? 0.05f;
        float arpl = SData?.path1AttackRangePerLevel ?? 0.2f;
        string desc = details
            ? $"Fires a magical snowball at her target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{arpl:F1}</b></color> per level. [<color=green><b>+{arpl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "+1 Attack target count.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float flatpl = SData?.snowMarkFlatDamagePerLevel ?? 6f;
        float pctpl  = SData?.snowMarkDamagePercentPerLevel ?? 0.01f;
        string desc = details
            ? $"Bond with a plant, excluding other <color=#00FFFF><b>Snowdrops</b></color> or other plants already bound.\n\n" +
              $"Attacks against unmarked targets deal <color=#00FFFF><b>[({SData?.snowMarkFlatDamageBase ?? 12f:F0}) + ({flatpl:F0}/Lvl.)]</b></color> + <color=#00FFFF><b>[({(SData?.snowMarkDamagePercentBase ?? 0.02f) * 100f:F1}%) + ({pctpl * 100f:F1}%/Lvl.)]</b></color> of its <color=red><b>Max Health</b></color> as {PlantData.DamageTypeLabel(damageType)}, and apply <color=#00FFFF><b>Snow Mark</b></color> for <color=green><b>{SnowMarkDuration:F0}s</b></color>.\n\n" +
              $"The bonded plant will target at highest priority the <color=#00FFFF><b>Snow Marked</b></color> insect. Direct damage inflicted by the bonded plant against the <color=#00FFFF><b>Marked</b></color> insect detonates the <color=#00FFFF><b>Snow Mark</b></color>, dealing the same bonus as <color=red><b>Max Health</b></color> {PlantData.DamageTypeLabel(damageType)}. If the detonation was dealt by an <color=#00FFFF><b>Attack</b></color>, deal double that <color=red><b>Max Health</b></color> percentage instead."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase damage by <color=green><b>{flatpl:F0}</b></color> per level. [<color=green><b>+{flatpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase <color=red><b>Max Health</b></color> damage by <color=green><b>{pctpl * 100f:F1}%</b></color> per level. [<color=green><b>+{pctpl * effectivePath2Level * 100f:F1}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, "Snowdrop can target insects within the bonded plant's attack radius.\n\n<color=#00FFFF><b>Ice Beam</b></color>'s range cap is removed.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl = SData?.iceBeamDurationPerLevel ?? 1f;
        float aspl = SData?.iceBeamAttackSpeedPerLevel ?? 0.4f;
        string desc = details
            ? $"{GetName()}'s attacks become an <color=#00FFFF><b>Ice Beam</b></color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds, within which her Attack Speed is fixed at <color=green><b>[({(SData?.iceBeamAttackSpeed ?? 3f):F0}) + ({aspl:F1}/Lvl.)]</b></color>."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase Attack Speed by <color=green><b>{aspl:F1}</b></color> per level. [<color=green><b>+{aspl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path3Level, $"While the Skill is active, the bonded plant gains <color=green><b>{PartnerHasteBonus * 100f:F0}%</b></color> Attack Speed.")}\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
