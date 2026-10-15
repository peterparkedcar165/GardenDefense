using UnityEngine;

public class Cactus : Aura
{
    private float _tauntTickTimer;
    private const float TauntTickInterval  = 0.25f;
    private const float TauntEffectDuration = 0.35f;

    private CactusData CactData => data as CactusData;
    // Shield also scales with his own Magic Power, on top of the flat per-level Path3 growth
    private float ShieldAmount  => (CactData?.baseShieldAmount ?? 100f) + (CactData?.path3ShieldPerLevel ?? 10f) * effectivePath3Level + magicPower * ShieldMagicPowerScaling;
    private float ShieldMagicPowerScaling => CactData?.shieldMagicPowerScaling ?? 0.5f;
    // skillDurationAdder is generic (fed by the Fortified Stance skill tree node) - SkillDuration
    // is otherwise a fully bespoke computed value (doesn't read the generic skillDuration field
    // at all), so this is the only thing that makes that stat type do anything for him
    private float SkillDuration => data.baseSkillDuration + (CactData?.path3SkillDurationPerLevel ?? 2f) * effectivePath3Level + skillDurationAdder;

    // skill tree unlock ids
    public const string BarbedRetortUnlock = "cactus_barbed_retort";
    public const string RetaliatorySpikesUnlock = "cactus_retaliatory_spikes";
    public const string InstantSkillUnlock = "cactus_instant_skill";

    // Attack: melee AoE damage also scales with a % of his own Armor - both his Armor and the
    // scaling percentage itself grow per Path1 level
    private float ArmorScalingPercent => (CactData?.baseArmorScalingPercent ?? 0.5f) + (CactData?.path1ArmorScalingPerLevel ?? 0.05f) * effectivePath1Level;

    // Skill: Countering Stance - counter damage dealt as a % of the attacker's own base Attack
    // Damage (not a % of the damage Cactus actually received), grows per Path3 level, plus a flat
    // % of his own Armor on top (same idea as his Attack's own Armor scaling)
    private float CounterReflectPercent => (CactData?.baseCounterReflectPercent ?? 0.5f) + (CactData?.path3CounterReflectPerLevel ?? 0.1f) * effectivePath3Level;
    private float CounterArmorScalingPercent => CactData?.counterArmorScalingPercent ?? 0.5f;

    // Passive: taking Melee Physical Damage refunds part of his own attack cooldown
    private float CooldownRefundOnHit => (CactData?.baseCooldownRefundOnHit ?? 0.3f) + (CactData?.path2CooldownRefundPerLevel ?? 0.05f) * effectivePath2Level;

    // melee now - fertilizers/skill nodes/auras can't push his AoE radius past his own base
    // range, same as Acorn Knight/Bird of Paradise
    public override bool IsMeleeAttacker => true;

    // no targeting needed: the skill just shields Cactus himself
    private bool autoCastEnabled = false;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        basePassiveDuration = 8f;
        Entity.OnEntityHit += HandleIncomingHit;
        Entity.OnEntityHit += HandleSelfDamaged;

        // free skill readiness on placement - deliberately bypasses UnlockPath3() (which spends
        // sun and adds to totalSunSpent) so this can't be abused for an inflated uproot refund
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Entity.OnEntityHit -= HandleIncomingHit;
        Entity.OnEntityHit -= HandleSelfDamaged;
    }

    // Countering Stance (skill): a melee hit landing on Cactus while the stance is active
    // reflects a % of the attacker's own base Attack Damage, plus a flat % of his own Armor, back
    // at them - regardless of how much damage the hit itself actually dealt
    private void HandleIncomingHit(EntityEventData data)
    {
        // IsAlive itself is declared separately on Plant/Insect, not the shared Entity base -
        // health > 0 is the one check both rely on, so it works regardless of which kind source is
        if (data.target != this || !IsAlive || data.source == null || data.source.health <= 0) return;
        if (!HasEffect<CounteringStanceEffect>()) return;
        bool isMelee = data.tags != null && System.Array.Exists(data.tags, t => t == DamageTag.Melee);
        if (!isMelee) return;
        float reflectedDamage = data.source.baseAttackDamage * CounterReflectPercent + armor * CounterArmorScalingPercent;
        data.source.Damage(reflectedDamage, DamageType.Physical, elementalType, this, false,
            new DamageTag[] { DamageTag.SkillDamage, DamageTag.Counter });

        // Retaliatory Spikes (skill tree): the reflected hit also leaves a Punctured stack
        if (SkillTreeManager.HasUnlock(this, RetaliatorySpikesUnlock))
            data.source.ApplyEffect(new PuncturedEffect(data.source, passiveDuration, 1, this));
    }

    // Passive: taking Melee Physical Damage refunds part of his own attack cooldown
    private void HandleSelfDamaged(EntityEventData data)
    {
        if (data.target != this || !IsAlive) return;
        if (data.damageType != DamageType.Physical) return;
        bool isMelee = data.tags != null && System.Array.Exists(data.tags, t => t == DamageTag.Melee);
        if (!isMelee) return;
        attackCooldownTimer += CooldownRefundOnHit;

        // Barbed Retort (skill tree): any melee hit taken also leaves a Punctured stack on the
        // attacker, stance active or not - unlike Retaliatory Spikes, which only fires off the
        // reflected damage during Countering Stance
        if (SkillTreeManager.HasUnlock(this, BarbedRetortUnlock) && data.source != null && data.source.health > 0)
            data.source.ApplyEffect(new PuncturedEffect(data.source, passiveDuration, 1, this));
    }

    protected override void Update()
    {
        base.Update();

        if (autoCastEnabled && SkillReady && HasInsectsInRange())
            TriggerSkill();

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();

        if (HasEffect<ShieldEffect>())
        {
            _tauntTickTimer += Time.deltaTime;
            if (_tauntTickTimer >= TauntTickInterval)
            {
                _tauntTickTimer -= TauntTickInterval;
                ApplyTauntInRange();
            }
        }
        else
        {
            _tauntTickTimer = 0f;
        }
    }

    // click Auto Cast to toggle it on, click again to turn it off — no target to pick
    public override void ToggleAutoCast() => autoCastEnabled = !autoCastEnabled;

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled) return;
        autoCastEnabled = true;
    }

    // melee AoE, same pattern as Holly: everything within attackRange gets hit at once, no
    // targeting or projectile involved. tagged Counter so any Counter Damage bonus (fertilizer,
    // skill tree, etc.) applies to his regular attack now, not just a separate retaliation hit.
    // Punctured is still applied per hit automatically by Entity.HandleOnHitEffects (the
    // centralized on-hit dispatcher already checks "data.source is Cactus" unconditionally)
    protected override void Attack()
    {
        base.Attack();
        float armorBonus = armor * ArmorScalingPercent;
        foreach (Insect insect in GetInsectsInRange())
            insect.Damage(attackDamage + armorBonus, damageType, elementalType, this, false,
                new DamageTag[] { DamageTag.AoE, DamageTag.Attack, DamageTag.Melee, DamageTag.Counter });
    }

    // Passive: whenever Cactus is protected by ANY shield (his own skill included), nearby
    // insects are taunted onto him
    private void ApplyTauntInRange()
    {
        foreach (Insect insect in Insect.allInsects)
        {
            if (!insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= attackRange)
                insect.ApplyEffect(new TauntEffect(insect, TauntEffectDuration, 1, this, this) { tauntStrength = 2 });
        }
    }

    // the self-shield is a separate effect with its own lifetime from the stance - draining it
    // early doesn't cut the stance's resistance/reflection short, see CounteringStanceEffect
    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        ApplyEffect(new CounteringStanceEffect(this, SkillDuration, 1, this));
        ApplyEffect(new CactusShieldEffect(this, SkillDuration, 1, this, ShieldAmount));
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        if (HasEffect<CounteringStanceEffect>())
        {
            physicalResistance += CounteringStanceEffect.PhysicalResistanceBonus;
            // Skill Max Level Bonus: +25% total Armor while the stance is active
            if (IsPath3Maxed)
            {
                armor = Mathf.RoundToInt(armor * 1.25f);
                physicalResistance = armor / (ArmorCoefficient + armor);
            }
        }
        temperatureMax = comfortMax;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseArmor = data.baseArmor + Mathf.RoundToInt((CactData?.path1ArmorPerLevel ?? 5f) * level);
        // Attack Max Level Bonus: +25% base Attack Range
        baseAttackRange = IsPath1Maxed ? data.baseAttackRange * 1.25f : data.baseAttackRange;
    }

    public override void OnPath2Upgrade(int level)
    {
        float hppl = CactData?.path2HealthPerLevel ?? 60f;
        baseMaxHealth = (data?.baseMaxHealth ?? 250f) + hppl * level;
        health += hppl;
        UpdateHealthBar();
    }

    public override string GetName() => "<b><color=green>Cactus</color></b>";

    public override string GetDescription() =>
        $"A hardy desert plant that slams everything around it and counters harder the more it's hit.";

    // simple view: the combined total (Attack Damage + Armor bonus) as one number, tinted
    // Cactus's own elemental colour (Poison) - the breakdown only shows up in the detailed
    // (shift-held) Path1 description below
    public override string GetAttackDescription() =>
        $"Slams all insects within range, dealing <color=purple><b>{attackDamage + armor * ArmorScalingPercent:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}. Each hit applies <color=green><b>1</b></color> <color=purple><b>Punctured</b></color> stack.";

    public override string GetPassiveDescription() =>
        $"<color=purple><b>[Punctured]</b></color>: Reduce <color=green><b>Attack Damage</b></color> by <color=red><b>1</b></color>, up to <color=red><b>10</b></color>.\n\n" +
        $"Whenever the {GetName()} is protected by a <color=#00CED1>Shield</color>, he taunts insects within his attack range.\n\n" +
        $"Taking <color=green>Melee</color> <color=#A0522D>Physical Damage</color> refunds <color=green><b>{CooldownRefundOnHit:F1}s</b></color> of his attack cooldown.";

    public override string GetSkillDesription() =>
        $"The {GetName()} enters a <color=green><b>Countering</b></color> stance for <color=green><b>{SkillDuration:F0}s</b></color>, and gains a <color=#00CED1><b>Shield</b></color> for <color=grey><b>{ShieldAmount:F0}</b></color> [<color=#FFB6C1><b>+{magicPower * ShieldMagicPowerScaling:F0}</b></color>] health. " +
        $"He gains <color=green><b>{CounteringStanceEffect.PhysicalResistanceBonus * 100f:F0}%</b></color> <color=#A0522D><b>Physical Resistance</b></color>. Taking <color=green>Melee Damage</color> while in this stance deals " +
        $"<color=green><b>{CounterReflectPercent * 100f:F0}%</b></color> of the attacker's own <color=green><b>Base Attack Damage</b></color>, plus <color=#00CED1><b>{CounterArmorScalingPercent * 100f:F0}%</b></color> of his own <color=#00CED1><b>Armor</b></color> [<color=#00CED1><b>+{armor * CounterArmorScalingPercent:F0}</b></color>], back to them.";

    public override string GetPath1Description(bool details = false)
    {
        float armorpl   = CactData?.path1ArmorPerLevel        ?? 5f;
        float scalingpl = CactData?.path1ArmorScalingPerLevel ?? 0.05f;
        string desc = details
            ? $"Slams all insects within range, dealing <color=#00CED1><b>[100% Attack Damage + {ArmorScalingPercent * 100f:F0}% Armor]</b></color> {PlantData.DamageTypeLabel(damageType)}. Each hit applies <color=green><b>1</b></color> <color=purple><b>Punctured</b></color> stack."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=#00CED1><b>Base Armor</b></color> by <color=#00CED1><b>{armorpl:F0}</b></color> per level. [<color=#00CED1><b>+{armorpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=#00CED1><b>Armor</b></color> scaling by <color=green><b>{scalingpl * 100f:F0}%</b></color> per level. [<color=green><b>+{scalingpl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Increase base <color=#00CED1><b>Attack Range</b></color> by <color=green><b>25%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float hppl = CactData?.path2HealthPerLevel ?? 60f;
        float cdrpl = CactData?.path2CooldownRefundPerLevel ?? 0.05f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Increase <color=green><b>Base Max Health</b></color> by <color=green><b>{hppl:F0}</b></color> per level. [<color=green><b>+{hppl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase Cooldown Refund by <color=green><b>{cdrpl:F2}s</b></color> per level. [<color=green><b>+{cdrpl * effectivePath2Level:F2}s</b></color>]\n\n" +
               $"{Level5Section(path2Level, "Attacks now apply <color=green><b>2</b></color> <color=purple><b>Punctured</b></color> stacks.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float shieldpl   = CactData?.path3ShieldPerLevel            ?? 10f;
        float durpl      = CactData?.path3SkillDurationPerLevel     ?? 2f;
        float reflectpl  = CactData?.path3CounterReflectPerLevel    ?? 0.05f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Increase counter damage by <color=green><b>{reflectpl * 100f:F0}%</b></color> per level. [<color=green><b>+{reflectpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase Shield by <color=grey><b>{shieldpl:F0}</b></color> per level. [<color=grey><b>+{shieldpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Increase total <color=#00CED1><b>Armor</b></color> by <color=green><b>25%</b></color> while the skill is active.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
