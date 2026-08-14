using UnityEngine;
using System.Collections.Generic;

public class Rhodiola : Aura
{
    [SerializeField] private ParticleSystem healConeParticles;  // continuous cone of rejuvenating mist while healing
    [SerializeField] private float healConeTravelTime = 0.25f;  // seconds for a particle to cross the attack range
    [SerializeField] private float healConeRate = 100f;         // particles emitted per second while healing

    private RhodiolaData RData => data as RhodiolaData;

    private Entity _mainTarget;
    private Vector2 _facingDir = Vector2.right;
    private ConeParticleEmitter _healCone;

    public float HealPerSecondFlat => (RData?.baseHealPerSecond ?? 8f) + (RData?.path1HealPerSecondPerLevel ?? 2f) * effectivePath1Level;
    public float HealPerSecondMP   => (RData?.attackHealMPScaling ?? 0.05f) * magicPower;
    public float HealPerSecond     => HealPerSecondFlat + HealPerSecondMP;
    public float TickInterval      => RData?.healTickInterval ?? 0.5f;
    public float ConeAngle         => RData?.coneAngle ?? 40f;
    public float SplashMultiplier  => RData?.splashHealMultiplier ?? 0.5f;
    public float MissingHealthPerSecond => RData?.maxMissingHealthPerSecond ?? 0.08f;

    public float GrassConversion => (RData?.baseGrassConversion ?? 0.5f) + (RData?.path2GrassConversionPerLevel ?? 0.1f) * effectivePath2Level;
    public float HealingReturn   => (RData?.baseHealingReturn ?? 0.15f) + (RData?.path2HealingReturnPerLevel ?? 0.03f) * effectivePath2Level;

    // fixed rate, does not scale with levels or magic power
    public float BurgeonHealPerSecond => RData?.burgeonHealPerSecond ?? 12f;
    public float BurgeonDuration      => RData?.baseBurgeonDuration  ?? 4f;
    public float BurgeonTickInterval  => RData?.burgeonTickInterval   ?? 0.5f;

    public float RevivalBaseHeal     => RData?.revivalBaseHeal     ?? 40f;
    public float RevivalHealPerLevel => RData?.revivalHealPerLevel ?? 20f;
    public float RevivalHealFlat     => RevivalBaseHeal + RevivalHealPerLevel * effectivePath3Level;
    public float RevivalMPHeal       => (RData?.skillHealMPScaling ?? 0.30f) * magicPower;
    public float RevivalHeal         => RevivalHealFlat + RevivalMPHeal;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        Entity.OnHeal += OnAnyHeal;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Entity.OnHeal -= OnAnyHeal;
    }

    // passive, a portion of any healing this rhodiola grants to others returns to it.
    // covers the attack, burgeon ticks, the revival heal and any future regen it sources.
    // the return itself is sourceless so it can never trigger another return
    private void OnAnyHeal(EntityEventData data)
    {
        if (data.source != this || data.target == this) return;
        Heal(data.amount * HealingReturn);
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        // passive, heals and shields given are increased by a portion of grass damage
        healingBonus += grassDamage * GrassConversion;
    }

    protected override void Update()
    {
        base.Update();
        attackCooldown = TickInterval;

        _mainTarget = FindMostInjuredHealable();
        if (_mainTarget != null)
            _facingDir = ((Vector2)_mainTarget.transform.position - (Vector2)transform.position).normalized;

        bool canAttack = _mainTarget != null && !IsStunned && !IsChanneling;

        if (_healCone == null) _healCone = new ConeParticleEmitter(healConeParticles, healConeRate);
        _healCone.Update(canAttack, _facingDir, ConeAngle, attackRange, healConeTravelTime);

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (canAttack)
            Attack();
    }

    private const float CriticalHealthPercent = 0.25f;

    // targeting priority: 1) any plant under 25% health (lowest % wins), always overrides
    // everything else. 2) whichever plant is already being healed, kept as the target until
    // it reaches full health, so healing does not flicker between similarly injured plants.
    // 3) the single lowest health plant, once no plant qualifies for the tiers above.
    // 4) friendly insects and minions, lowest health, only once no plant needs healing at all
    private Entity FindMostInjuredHealable()
    {
        Plant critical = null;
        float criticalPercent = 1f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || plant == this || !plant.IsAlive) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > attackRange) continue;
            float percent = plant.health / plant.maxHealth;
            if (percent >= CriticalHealthPercent) continue;
            if (percent < criticalPercent) { criticalPercent = percent; critical = plant; }
        }
        if (critical != null) return critical;

        if (_mainTarget is Plant currentTarget && currentTarget.IsAlive
            && currentTarget.health < currentTarget.maxHealth
            && Vector2.Distance(transform.position, currentTarget.transform.position) <= attackRange)
            return currentTarget;

        Plant lowest = null;
        float lowestPercent = 1f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || plant == this || !plant.IsAlive) continue;
            if (plant.health >= plant.maxHealth) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > attackRange) continue;
            float percent = plant.health / plant.maxHealth;
            if (percent < lowestPercent) { lowestPercent = percent; lowest = plant; }
        }
        if (lowest != null) return lowest;

        Insect lowestAlly = null;
        float lowestAllyPercent = 1f;
        foreach (Insect ally in Insect.friendlyInsects)
        {
            if (ally == null || !ally.IsAlive) continue;
            if (ally.health >= ally.maxHealth) continue;
            if (Vector2.Distance(transform.position, ally.transform.position) > attackRange) continue;
            float percent = ally.health / ally.maxHealth;
            if (percent < lowestAllyPercent) { lowestAllyPercent = percent; lowestAlly = ally; }
        }
        return lowestAlly;
    }

    protected override void Attack()
    {
        base.Attack();
        if (_mainTarget == null) return;

        HealTick(_mainTarget, 1f);

        float halfAngle = ConeAngle * 0.5f;

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || plant == this || plant == _mainTarget || !plant.IsAlive) continue;
            if (plant.health >= plant.maxHealth) continue;
            Vector2 to = (Vector2)plant.transform.position - (Vector2)transform.position;
            if (to.magnitude > attackRange) continue;
            if (Vector2.Angle(_facingDir, to) > halfAngle) continue;
            HealTick(plant, SplashMultiplier);
        }

        foreach (Insect ally in new List<Insect>(Insect.friendlyInsects))
        {
            if (ally == null || ally == _mainTarget || !ally.IsAlive) continue;
            if (ally.health >= ally.maxHealth) continue;
            Vector2 to = (Vector2)ally.transform.position - (Vector2)transform.position;
            if (to.magnitude > attackRange) continue;
            if (Vector2.Angle(_facingDir, to) > halfAngle) continue;
            HealTick(ally, SplashMultiplier);
        }
    }

    // heals one plant, friendly insect, or minion for one tick, the healing return is handled by the OnHeal hook
    private void HealTick(Entity entity, float multiplier)
    {
        float amount = HealPerSecond * TickInterval * multiplier;
        if (IsPath1Maxed)
            amount += (entity.maxHealth - entity.health) * MissingHealthPerSecond * TickInterval * multiplier;

        entity.Heal(amount, this);

        if (IsPath2Maxed)
            entity.ApplyEffect(new RejuvenatingBurgeonEffect(entity, BurgeonDuration, 1, this, BurgeonHealPerSecond * BurgeonTickInterval, BurgeonTickInterval));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackRange = data.baseAttackRange + level * (RData?.path1AttackRangePerLevel ?? 0.2f);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillCooldown = data.baseSkillCooldown - level * (RData?.path3CooldownReductionPerLevel ?? 0f);
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        bool anyDead = false;
        foreach (var kvp in Tile.allTiles)
            if (kvp.Value.deadPlant != null) { anyDead = true; break; }
        if (!anyDead) return;
        SkillTargetingManager.instance.BeginDeadTileTargeting(OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Tile tile)
    {
        skillCooldownTimer = skillCooldown;
        Plant revived = Plant.RevivePlant(tile);
        if (revived != null)
            revived.Heal(RevivalHeal, this);
        if (revived != null && IsPath3Maxed)
        {
            float shield = RData?.verdantGuardianShield ?? 200f;
            float regen  = RData?.verdantGuardianRegen  ?? 20f;
            revived.ApplyEffect(new VerdantGuardianEffect(revived, skillDuration, this, shield, regen));
        }
    }

    public override string GetName() => $"<b><color=green>Rhodiola</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} breathes life into its allies, mending wounds with rejuvenating energy.";

    public override string GetAttackDescription() =>
        $"Breathes rejuvenating energy in a <color=green><b>{ConeAngle:F0}°</b></color> cone towards the most injured plant, " +
        $"healing it for <color=green><b>{HealPerSecondFlat:F0}</b></color> [<color=#FFB6C1><b>+{HealPerSecondMP:F0}</b></color>] health per second. " +
        $"Other plants within the cone are healed for <color=green><b>{SplashMultiplier * 100f:F0}%</b></color> of the amount.";

    public override string GetPassiveDescription() =>
        $"Increase <color=#FF6B81><b>Heals & Shields</b></color> given by <color=green><b>{GrassConversion * 100f:F0}%</b></color> of <color=green><b>Grass Damage</b></color>.\n\n" +
        $"<color=green><b>{HealingReturn * 100f:F0}%</b></color> of healing given to others is returned to the {GetName()}.";

    public override string GetSkillDesription() =>
        $"Target a tile where a plant has fallen to resurrect it. The plant is then healed for <color=green><b>{RevivalHealFlat:F0}</b></color> [<color=#FFB6C1><b>+{RevivalMPHeal:F0}</b></color>] Health.";

    public override string GetPath1Name() => "Verdance";
    public override string GetPath2Name() => "Symbiosis";
    public override string GetPath3Name() => "Revival";

    public override string GetPath1Description(bool details = false)
    {
        float rngpl  = RData?.path1AttackRangePerLevel   ?? 0.2f;
        float healpl = RData?.path1HealPerSecondPerLevel ?? 2f;
        float baseHeal = RData?.baseHealPerSecond ?? 8f;
        string desc = details
            ? $"Breathes rejuvenating energy in a <color=green><b>{ConeAngle:F0}°</b></color> cone towards the most injured plant, " +
              $"healing <color=green><b>[({baseHeal:F0}) + ({healpl:F0}/Lvl.) + <color=#FFB6C1>{(RData?.attackHealMPScaling ?? 0.05f) * 100f:F0}% Magic Power</color>]</b></color> health per second. " +
              $"Other plants within the cone are healed for <color=green><b>{SplashMultiplier * 100f:F0}%</b></color> of the amount."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rngpl:F2}</b></color> per level. [<color=green><b>+{rngpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Healing</b></color> by <color=green><b>{healpl:F0}</b></color> per second per level. [<color=green><b>+{healpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Heals an additional <color=green><b>{MissingHealthPerSecond * 100f:F0}%</b></color> of the target's missing health per second.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float convpl = RData?.path2GrassConversionPerLevel ?? 0.1f;
        float retpl  = RData?.path2HealingReturnPerLevel   ?? 0.03f;
        string desc = details
            ? $"Increase <color=#FF6B81><b>Heals & Shields</b></color> given by <color=green><b>[({(RData?.baseGrassConversion ?? 0.5f) * 100f:F0}%) + ({convpl * 100f:F0}%/Lvl.)]</b></color> of <color=green><b>Grass Damage</b></color>.\n\n" +
              $"<color=green><b>[({(RData?.baseHealingReturn ?? 0.15f) * 100f:F0}%) + ({retpl * 100f:F0}%/Lvl.)]</b></color> of healing given to others is returned to the {GetName()}."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=#FF6B81><b>Heals & Shields</b></color> conversion by <color=green><b>{convpl * 100f:F0}%</b></color> per level. [<color=green><b>+{convpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Healing Returned</b></color> by <color=green><b>{retpl * 100f:F0}%</b></color> per level. [<color=green><b>+{retpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"Healing from the attack applies <color=green><b>Rejuvenating Burgeon</b></color>, healing <color=green><b>{BurgeonHealPerSecond:F0}</b></color> health per second for <color=green><b>{BurgeonDuration:F0}s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float cdrpl = RData?.path3CooldownReductionPerLevel ?? 0.1f;
        string desc = details
            ? $"Target a tile where a plant has fallen to resurrect it. The plant is then healed for <color=green><b>[({RevivalBaseHeal:F0}) + ({RevivalHealPerLevel:F0}/Lvl.) + <color=#FFB6C1>{(RData?.skillHealMPScaling ?? 0.30f) * 100f:F0}% Magic Power</color>]</b></color> Health."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Revival Heal</b></color> by <color=green><b>{RevivalHealPerLevel:F0}</b></color> per level. [<color=green><b>+{RevivalHealPerLevel * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Reduce <color=green><b>Base Skill Cooldown</b></color> by <color=green><b>{Mathf.RoundToInt(cdrpl)}s</b></color> per level. [<color=green><b>-{Mathf.RoundToInt(cdrpl * effectivePath3Level)}s</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Upon reviving a plant, grant it <color=green><b>Verdant Guardian</b></color>, shielding it for <color=grey><b>{RData?.verdantGuardianShield ?? 200f:F0}</b></color> health and regenerating <color=green><b>{RData?.verdantGuardianRegen ?? 20f:F0}</b></color> health per second while the shield lasts, for <color=green><b>{skillDuration:F0}s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
