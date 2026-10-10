using UnityEngine;
using System.Collections.Generic;

public class Snowdrop : Shooter
{
    [SerializeField] private GameObject iceBeamProjectilePrefab;

    private SnowdropData SData => data as SnowdropData;

    public float SnowMarkFlatDamage => (SData?.snowMarkFlatDamageBase ?? 12f) + (SData?.snowMarkFlatDamagePerLevel ?? 6f) * effectivePath2Level;
    public float SnowMarkPercent   => (SData?.snowMarkDamagePercentBase ?? 0.02f) + (SData?.snowMarkDamagePercentPerLevel ?? 0.01f) * effectivePath2Level;
    public float SnowMarkDuration  => SData?.snowMarkDuration ?? 6f;
    private float IceBeamAttackSpeed => SData?.iceBeamAttackSpeed ?? 4f;
    private int IceBeamPiercingBonus => (SData?.iceBeamPiercingBase ?? 1) + (SData?.iceBeamPiercingPerLevel ?? 1) * effectivePath3Level;

    private static readonly DamageTag[] bonusTags = { DamageTag.Coordinated, DamageTag.OnHit, DamageTag.NoPrimer, DamageTag.PassiveDamage };

    // Ice Beam: a timer rather than a bool so ActivateSkill/Update have one single source of
    // truth for "how much longer is this active" - ticks down in Update, never below 0
    private float _iceBeamTimer;
    public bool IceBeamActive => _iceBeamTimer > 0f;

    // Snow Bond: a permanent link to any plant on the field, resolved live from the tile it sits
    // on (like Calendula's auto-cast target / Carrot's Soil Bond) so a bonded plant that dies and
    // revives - a brand new instance - gets automatically picked back up instead of leaving a
    // stale link
    private Tile boundTile;
    private Plant boundPartner;
    private Plant _bondHighlighted;
    private static readonly Color BondHighlightColor = new Color(0.4f, 0.9f, 1f);

    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => boundTile != null;
    public override string AutoCastLabel => "Bond";

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();

        if (_iceBeamTimer > 0f)
        {
            _iceBeamTimer -= Time.deltaTime;
            if (_iceBeamTimer <= 0f) UpdateStats();
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
        FireProjectile(target, primaryTarget);

        // Path1 max: a second shot fires at whatever the next-most-valid target is (same
        // targeting rule as the primary shot, just excluding it), rather than hitting the same
        // insect twice
        if (!IsPath1Maxed) return;
        GameObject secondaryTarget = FindTarget(excluding: primaryTarget);
        if (secondaryTarget == null) return;
        FireProjectile(PredictTargetPosition(secondaryTarget), secondaryTarget);
    }

    private void FireProjectile(Vector3 target, GameObject targetObj)
    {
        GameObject prefab = IceBeamActive ? iceBeamProjectilePrefab : projectilePrefab;
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, transform.position, Quaternion.identity);
        Projectile proj = obj.GetComponent<Projectile>();
        if (proj == null) return;

        int effectivePiercing = IceBeamActive ? piercing + IceBeamPiercingBonus : piercing;

        proj.SetTarget(targetObj);
        proj.Initialize(target, attackDamage, projectileSpeed, maxRange, effectivePiercing, damageType, elementalType, this);
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
        insect.Damage(bonus, DamageType.Magic, ElementalType.Ice, this, false, bonusTags);
        if (!insect.IsAlive) return;

        insect.ApplyEffect(new SnowMarkEffect(insect, SnowMarkDuration, 1, this));
    }

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

        float bonus = SnowMarkFlatDamage + insect.maxHealth * SnowMarkPercent;
        insect.Damage(bonus, DamageType.Magic, ElementalType.Ice, this, false, bonusTags);
        insect.RemoveEffect<SnowMarkEffect>();
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        _iceBeamTimer = skillDuration;
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
        if (targetPlant == this)
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnBondTargetConfirmed, this);
            return;
        }
        boundTile = targetPlant.occupiedTile;
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
                Entity.OnEntityHit += HandleBoundPartnerHit;
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
        $"Fires an ice projectile at its target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        $"Attacks that hit an unmarked target also deal <color=green><b>{SnowMarkFlatDamage:F0}</b></color> + <color=green><b>{SnowMarkPercent * 100f:F0}%</b></color> of its Max Health as bonus <color=#00FFFF>Ice</color> Magic damage and apply <color=#00FFFF><b>Snow Mark</b></color> for <color=green><b>{SnowMarkDuration:F0}s</b></color>. Attacking an already-marked target refreshes the Mark instead of dealing the bonus again.\n\n" +
        $"Select a plant anywhere on the field to form a <color=#00FFFF><b>Snow Bond</b></color>. The bonded plant prioritizes the Snow Marked insect above all other targets, and detonating the Mark with an attack, passive, or skill hit deals the same bonus damage, sourced from {GetName()}. Only the bonded partner can detonate it.";

    public override string GetSkillDesription() =>
        $"For <color=green><b>{skillDuration:F0}s</b></color>, {GetName()}'s attacks become a piercing <color=#00FFFF><b>Ice Beam</b></color>, gaining <color=green><b>{IceBeamPiercingBonus}</b></color> Piercing and no longer losing damage against subsequent targets while piercing. Attack Speed becomes a fixed <color=green><b>{IceBeamAttackSpeed:F1}</b></color>, which cannot be increased or reduced while active.";

    public override string GetPath1Name() => "Frost Shot";
    public override string GetPath2Name() => "Snow Mark";
    public override string GetPath3Name() => "Ice Beam";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = SData?.path1AttackDamagePerLevel ?? 3f;
        float aspl = SData?.path1AttackSpeedPerLevel ?? 0.05f;
        float arpl = SData?.path1AttackRangePerLevel ?? 0.2f;
        string desc = details
            ? $"Fires an ice projectile at its target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{arpl:F1}</b></color> per level. [<color=green><b>+{arpl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Fires an additional ice projectile at a second, next most valid target.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float flatpl = SData?.snowMarkFlatDamagePerLevel ?? 6f;
        float pctpl  = SData?.snowMarkDamagePercentPerLevel ?? 0.01f;
        string desc = details
            ? $"Attacks that hit an unmarked target also deal <color=green><b>[({SData?.snowMarkFlatDamageBase ?? 12f:F0}) + ({flatpl:F0}/Lvl.)]</b></color> + <color=green><b>[({(SData?.snowMarkDamagePercentBase ?? 0.02f) * 100f:F0}%) + ({pctpl * 100f:F1}%/Lvl.)]</b></color> of its Max Health as bonus <color=#00FFFF>Ice</color> Magic damage and apply <color=#00FFFF><b>Snow Mark</b></color> for <color=green><b>{SnowMarkDuration:F0}s</b></color>. Attacking an already-marked target refreshes the Mark instead of dealing the bonus again.\n\n" +
              $"Select a plant anywhere on the field to form a <color=#00FFFF><b>Snow Bond</b></color>. The bonded plant prioritizes the Snow Marked insect above all other targets, and detonating the Mark with an attack, passive, or skill hit deals the same bonus damage, sourced from {GetName()}. Only the bonded partner can detonate it."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase flat bonus damage by <color=green><b>{flatpl:F0}</b></color> per level. [<color=green><b>+{flatpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase bonus damage by <color=green><b>{pctpl * 100f:F1}%</b></color> of Max Health per level. [<color=green><b>+{pctpl * effectivePath2Level * 100f:F1}%</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl = SData?.iceBeamDurationPerLevel ?? 1f;
        int piercepl = SData?.iceBeamPiercingPerLevel ?? 1;
        string desc = details
            ? $"For <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds, {GetName()}'s attacks become a piercing <color=#00FFFF><b>Ice Beam</b></color>, gaining <color=green><b>[({SData?.iceBeamPiercingBase ?? 1}) + ({piercepl}/Lvl.)]</b></color> Piercing and no longer losing damage against subsequent targets while piercing. Attack Speed becomes a fixed <color=green><b>{IceBeamAttackSpeed:F1}</b></color>, which cannot be increased or reduced while active."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase Ice Beam Piercing by <color=green><b>{piercepl}</b></color> per level. [<color=green><b>+{piercepl * effectivePath3Level}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
