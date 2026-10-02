using UnityEngine;

public class Rhodiola : Shooter
{
    [SerializeField] private GameObject healProjectilePrefab;

    private RhodiolaData RData => data as RhodiolaData;

    private Entity _mainTarget;

    // Shooter doesn't show an attack bar by default (most shooters don't need one) - Rhodiola
    // did as an Aura before, so opt back in the same way Gloriosa does
    protected override bool GetAttackBarVisible() => attackCooldown > 0f;

    // skill tree node unlock ids
    public const string RevivalAoeUnlock      = "rhodiola_revival_aoe";
    public const string RevivalEmpowerUnlock  = "rhodiola_revival_empower";
    public const string InstantSkillUnlock    = "rhodiola_instant_skill";
    public const string OvergrowthUnlock      = "rhodiola_overgrowth";
    public const string WidespreadBloomUnlock = "rhodiola_widespread_bloom";

    // counts attacks toward Overgrowth's every-8th-attack healing burst
    private int _overgrowthAttackCount = 0;

    // healing is just attackDamage (the standard stat, scaled per level via OnPath1Upgrade below)
    // plus a flat magic power contribution, applied once per shot - attack cadence is the
    // standard 1/attackSpeed (handled by Shooter.Update())
    public float HealMP     => (RData?.attackHealMPScaling ?? 0.05f) * magicPower;
    public float HealAmount => attackDamage + HealMP;

    public float GrassConversion => (RData?.baseGrassConversion ?? 0.5f) + (RData?.path2GrassConversionPerLevel ?? 0.1f) * effectivePath2Level;

    // Symbiosis max level bonus: full health favors more Grass Damage (see UpdateStats), otherwise
    // a portion of healing granted through the attack or Rejuvenating Burgeon returns to the Rhodiola
    public float MaxLevelGrassDamageBonus => RData?.maxLevelGrassDamageBonus ?? 0.25f;
    public float MaxLevelHealingReturn    => RData?.maxLevelHealingReturn    ?? 0.33f;

    // Rejuvenating Burgeon is baseline now (used to be a Path2 max-level-only bonus) and its
    // potency scales per level, unlike the fixed-rate duration/tick interval below
    public float BurgeonHealPerSecond => (RData?.burgeonHealPerSecond ?? 12f) + (RData?.path2BurgeonHealPerLevel ?? 2f) * effectivePath2Level;
    public float BurgeonDuration      => RData?.baseBurgeonDuration  ?? 4f;
    public float BurgeonTickInterval  => RData?.burgeonTickInterval   ?? 0.5f;

    public float MissingHealthPercent => RData?.maxMissingHealthPercent ?? 0.05f;

    public float RevivalBaseHeal     => RData?.revivalBaseHeal     ?? 40f;
    public float RevivalHealPerLevel => RData?.revivalHealPerLevel ?? 20f;
    public float RevivalHealFlat     => RevivalBaseHeal + RevivalHealPerLevel * effectivePath3Level;
    public float RevivalMPHeal       => (RData?.skillHealMPScaling ?? 0.30f) * magicPower;
    public float RevivalHeal         => RevivalHealFlat + RevivalMPHeal;

    protected override void Awake()
    {
        base.Awake();
        LoadData();

        // free skill readiness on placement - deliberately bypasses UnlockPath3() (which spends
        // sun and adds to totalSunSpent) so this can't be abused for an inflated uproot refund
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }
    }

    // Symbiosis max level bonus: while not fully healthy, a portion of healing granted through the
    // attack or Rejuvenating Burgeon returns to the Rhodiola itself. called directly at those heal
    // sites (rather than via the global OnHeal event) so the revival heal and skill-sourced regen
    // are never included - the return itself is sourceless so it can never trigger another return
    public void ReturnMaxLevelHealing(float amount)
    {
        if (!IsPath2Maxed || health >= maxHealth) return;
        Heal(amount * MaxLevelHealingReturn);
    }

    public override void UpdateStats()
    {
        base.UpdateStats();

        // Symbiosis max level bonus: fully healthy favors more Grass Damage instead of the return above.
        // additive, like every other elemental damage bonus in the codebase - Rhodiola's base Grass
        // Damage is 0, so a multiplier here would have nothing to scale and always be a no-op
        if (IsPath2Maxed && health >= maxHealth)
            grassDamage += MaxLevelGrassDamageBonus;

        // passive, heals and shields given are increased by a portion of grass damage
        healingBonus += grassDamage * GrassConversion;
    }

    // priority: 1) any plant under 25% health (lowest % wins), always overrides everything else.
    // 2) whichever plant is already being healed, kept as the target until it reaches full
    // health, so healing does not flicker between similarly injured plants. 3) the single lowest
    // health plant, once no plant qualifies for the tiers above. 4) friendly insects, lowest
    // health, only once no plant needs healing at all
    private const float CriticalHealthPercent = 0.25f;

    protected override GameObject FindTarget()
    {
        _mainTarget = FindMostInjuredHealable();
        return _mainTarget != null ? _mainTarget.gameObject : null;
    }

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

    protected override void Shoot(Vector3 target)
    {
        if (healProjectilePrefab == null || _mainTarget == null) return;
        GameObject obj = Instantiate(healProjectilePrefab, transform.position, Quaternion.identity);
        RhodiolaProjectile proj = obj.GetComponent<RhodiolaProjectile>();
        proj?.Initialize(this, _mainTarget, projectileSpeed, HealAmount, SkillTreeManager.HasUnlock(this, WidespreadBloomUnlock));
    }

    // heals one plant or friendly insect. isPrimaryTarget gates the Path1 max bonus and
    // Overgrowth's burst - both only ever applied to the shot's actual target, never to a
    // Widespread Bloom bounce. Rejuvenating Burgeon (baseline) applies either way
    public void HealTick(Entity entity, bool isPrimaryTarget)
    {
        float amount = HealAmount;
        if (isPrimaryTarget && IsPath1Maxed)
            // flat percent of missing health, added on top of the normal heal for this one hit -
            // not a per-second rate, so it doesn't scale with attack speed
            amount += (entity.maxHealth - entity.health) * MissingHealthPercent;

        entity.Heal(amount, this);
        ReturnMaxLevelHealing(amount);
        entity.ApplyEffect(new RejuvenatingBurgeonEffect(entity, BurgeonDuration, 1, this, BurgeonHealPerSecond * BurgeonTickInterval, BurgeonTickInterval));

        if (!isPrimaryTarget) return;

        if (SkillTreeManager.HasUnlock(this, OvergrowthUnlock))
        {
            _overgrowthAttackCount++;
            if (_overgrowthAttackCount >= 8)
            {
                _overgrowthAttackCount = 0;
                float burst = HealAmount * 4f;
                entity.Heal(burst, this);
                ReturnMaxLevelHealing(burst);
            }
        }
    }

    // Widespread Bloom: after landing on its real target, the projectile bounces once more to
    // the next most injured plant/ally in range, healing it for the same amount
    public void SpawnBounceProjectile(Vector3 fromPos, Entity target, float healAmount)
    {
        if (healProjectilePrefab == null || target == null) return;
        GameObject obj = Instantiate(healProjectilePrefab, fromPos, Quaternion.identity);
        RhodiolaProjectile proj = obj.GetComponent<RhodiolaProjectile>();
        if (proj == null) return;
        proj.Initialize(this, target, projectileSpeed, healAmount, false);
        proj.MarkAsBounce();
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage    = data.baseAttackDamage    + level * (RData?.path1AttackDamagePerLevel    ?? 2f);
        baseProjectileSpeed = data.baseProjectileSpeed + level * (RData?.path1ProjectileSpeedPerLevel ?? 0.5f);
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
        // nothing to revive at the targeted tile - don't consume the skill (the player can
        // just re-target immediately, no cooldown/sun/cast wasted on an empty click)
        Plant revived = Plant.RevivePlant(tile);
        if (revived == null) return;

        skillCooldownTimer = skillCooldown;
        revived.Heal(RevivalHeal, this);

        if (SkillTreeManager.HasUnlock(this, RevivalAoeUnlock))
            ReviveNeighbors(tile);

        if (SkillTreeManager.HasUnlock(this, RevivalEmpowerUnlock))
            revived.ApplyEffect(new VerdantEmpowermentEffect(revived, 8f, 1, this));

        if (IsPath3Maxed)
        {
            float shield = RData?.verdantGuardianShield ?? 200f;
            float regen  = RData?.verdantGuardianRegen  ?? 20f;
            revived.ApplyEffect(new VerdantGuardianEffect(revived, skillDuration, this, shield, regen));
        }
    }

    // Verdant Resurgence: also revives any fallen plant on the 8 tiles surrounding the target
    private void ReviveNeighbors(Tile centerTile)
    {
        Vector3 center = centerTile.transform.position;
        Vector3[] offsets =
        {
            Vector3.right, Vector3.left, Vector3.up, Vector3.down,
            new Vector3(1, 1, 0), new Vector3(1, -1, 0), new Vector3(-1, 1, 0), new Vector3(-1, -1, 0)
        };
        foreach (Vector3 offset in offsets)
        {
            if (!Tile.allTiles.TryGetValue(Tile.TileKey(center + offset), out Tile neighbor)) continue;
            if (neighbor.deadPlant == null) continue;
            Plant revivedNeighbor = Plant.RevivePlant(neighbor);
            if (revivedNeighbor != null)
                revivedNeighbor.Heal(RevivalHeal, this);
        }
    }

    public override string GetName() => $"<b><color=green>Rhodiola</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} breathes life into its allies, mending wounds with rejuvenating energy.";

    public override string GetAttackDescription() =>
        $"Fires a rejuvenating seed at the most injured nearby plant, healing it for <color=green><b>{attackDamage:F0}</b></color> " +
        $"[<color=#FFB6C1><b>+{HealMP:F0}</b></color>] health and leaving behind a regenerating bloom.";

    public override string GetPassiveDescription() =>
        $"Increase <color=#FF6B81><b>Heals & Shields</b></color> given by <color=green><b>{GrassConversion * 100f:F0}%</b></color> of <color=green><b>Grass Damage</b></color>.\n\n" +
        $"Healing applies <color=green><b>Rejuvenating Burgeon</b></color>, healing <color=green><b>{BurgeonHealPerSecond:F0}</b></color> health per second for <color=green><b>{BurgeonDuration:F0}s</b></color>.";

    public override string GetSkillDesription() =>
        $"Target a tile where a plant has fallen to resurrect it. The plant is then healed for <color=green><b>{RevivalHealFlat:F0}</b></color> [<color=#FFB6C1><b>+{RevivalMPHeal:F0}</b></color>] Health.";

    public override string GetPath1Name() => "Verdance";
    public override string GetPath2Name() => "Symbiosis";
    public override string GetPath3Name() => "Revival";

    public override string GetPath1Description(bool details = false)
    {
        float speedpl = RData?.path1ProjectileSpeedPerLevel ?? 0.5f;
        float dmgpl   = RData?.path1AttackDamagePerLevel    ?? 2f;
        string desc = details
            ? $"Fires a rejuvenating seed at the most injured nearby plant, healing <color=green><b>[100% Attack Damage]</b></color> [<color=#FFB6C1><b>+{(RData?.attackHealMPScaling ?? 0.05f) * 100f:F0}% Magic Power</b></color>] health.\n\n*Unaffected by Piercing"
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Projectile Speed</b></color> by <color=green><b>{speedpl:F1}</b></color> per level. [<color=green><b>+{speedpl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Each projectile heals an additional <color=green><b>{MissingHealthPercent * 100f:F0}%</b></color> of the target's missing health.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float convpl = RData?.path2GrassConversionPerLevel ?? 0.1f;
        float burgpl = RData?.path2BurgeonHealPerLevel      ?? 2f;
        string desc = details
            ? $"Increase <color=#FF6B81><b>Heals & Shields</b></color> given by <color=green><b>[({(RData?.baseGrassConversion ?? 0.5f) * 100f:F0}%) + ({convpl * 100f:F0}%/Lvl.)]</b></color> of <color=green><b>Grass Damage</b></color>.\n\n" +
              $"Healing applies <color=green><b>Rejuvenating Burgeon</b></color>, healing <color=green><b>[({RData?.burgeonHealPerSecond ?? 12f:F0}) + ({burgpl:F0}/Lvl.)]</b></color> health per second for <color=green><b>{BurgeonDuration:F0}s</b></color>."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=#FF6B81><b>Heals & Shields</b></color> conversion by <color=green><b>{convpl * 100f:F0}%</b></color> per level. [<color=green><b>+{convpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Rejuvenating Burgeon</b></color> healing by <color=green><b>{burgpl:F0}</b></color> per second per level. [<color=green><b>+{burgpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"If fully healthy, increase <color=green><b>Grass Damage</b></color> by <color=green><b>{MaxLevelGrassDamageBonus * 100f:F0}%</b></color>. Otherwise, <color=green><b>{MaxLevelHealingReturn * 100f:F0}%</b></color> of healing granted through the attack and <color=green><b>Rejuvenating Burgeon</b></color> is returned to the {GetName()}.")}\n\n" +
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
