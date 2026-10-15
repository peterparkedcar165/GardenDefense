using UnityEngine;
using System.Collections.Generic;

public class NeriumOleander : Shooter
{
    public NeriumOleanderData OleanderData => data as NeriumOleanderData;

    [SerializeField] private GameObject sproutPrefab;

    private OleanderSprout currentSprout;

    private bool autoCastEnabled = false;
    private Vector3 autoCastPosition;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    // bounce distance is just the oleander's own attack range; not shown separately in tooltips
    public float BounceSearchRadius => attackRange;
    private float SproutDuration   => (OleanderData?.baseSproutDuration ?? 15f) + (OleanderData?.path3SproutDurationPerLevel ?? 3f) * effectivePath3Level;
    private float CurseReduction   => (OleanderData?.baseCursePoisonResistReduction ?? 0.04f) + (OleanderData?.path3CurseReductionPerLevel ?? 0.01f) * effectivePath3Level;

    // Passive - Oleandic Ward: protects nearby allies instead of afflicting insects. duration
    // scales the same way the old Toxin duration did (Path2 flat growth, times the generic
    // passiveDuration multiplier); the heal on application is deliberately weak, the real
    // value is WardRegenPerLock (see OleandicWardEffect.OnTick)
    private float WardDuration     => ((OleanderData?.baseWardDuration ?? 6f) + (OleanderData?.path2WardDurationPerLevel ?? 2f) * effectivePath2Level) * (1 + passiveDuration);
    private float WardHealAmount   => OleanderData?.baseWardHealAmount ?? 5f;
    private float WardRegenPerLock => OleanderData?.wardRegenPerLock ?? 3f;

    private const float WardAuraTickInterval = 0.5f;
    private float _wardAuraTickTimer;

    // skill tree unlock ids
    public const string VenomChainUnlock   = "oleander_venom_chain";
    public const string ThornedPetalsUnlock = "oleander_thorned_petals";
    public const string InstantSkillUnlock  = "oleander_instant_skill";
    private const float ThornedPetalsAttackSpeedBonus = 0.25f;

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

    protected override void Update()
    {
        base.Update();
        UpdateAutoCast();

        _wardAuraTickTimer += Time.deltaTime;
        if (_wardAuraTickTimer >= WardAuraTickInterval)
        {
            _wardAuraTickTimer -= WardAuraTickInterval;
            ApplyWardToAllInRange();
        }
    }

    private void UpdateAutoCast()
    {
        if (!autoCastEnabled) return;
        if (SkillReady) OnTargetConfirmed(autoCastPosition);
    }

    // Passive - Oleandic Ward: every allied plant within attack range gets its Ward
    // (re)confirmed, same continuous-presence shape Holly's Snow Ward aura uses
    private void ApplyWardToAllInRange()
    {
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;

            OleandicWardEffect existing = plant.GetEffect<OleandicWardEffect>();
            if (existing != null)
                existing.RefreshPresence(this);
            else
                plant.ApplyEffect(new OleandicWardEffect(plant, WardDuration, 1, this, WardHealAmount, WardRegenPerLock));
        }
    }

    public override void UpdateStats()
    {
        // Thorned Petals (skill tree): bonus Attack Speed while her own Oleander Sprout stands -
        // has to go in before base.UpdateStats() actually folds attackSpeedTotalMultiplier into
        // attackSpeed, same trick Zinnia's Solar Flare uses
        bool thornedPetalsActive = currentSprout != null && SkillTreeManager.HasUnlock(this, ThornedPetalsUnlock);
        float thornedPetalsBonus = thornedPetalsActive ? ThornedPetalsAttackSpeedBonus : 0f;
        attackSpeedTotalMultiplier += thornedPetalsBonus;
        base.UpdateStats();
        attackSpeedTotalMultiplier -= thornedPetalsBonus;

        piercing      += (OleanderData?.path1BouncePerLevel ?? 1) * effectivePath1Level;
    }

    // a dead oleander can't sustain its sprout: it must go with it. Kill() catches death
    // immediately (before any death animation delay); OnDestroy catches every other way the
    // oleander can leave the field (sold, scene unload, etc.)
    public override void Kill()
    {
        DestroyCurrentSprout();
        base.Kill();
    }

    public override void Kill(Entity source)
    {
        DestroyCurrentSprout();
        base.Kill(source);
    }

    protected override void OnDestroy()
    {
        DestroyCurrentSprout();
        base.OnDestroy();
    }

    private void DestroyCurrentSprout()
    {
        if (currentSprout != null)
            Destroy(currentSprout.gameObject);
        currentSprout = null;
    }

    // the oleander targets normally with its own eyes (direct sight, attackRange) same as any
    // shooter. any sprout it can reach acts as a second eye: insects only visible through a
    // sprout chain are added to the same candidate pool, and the usual targeting rule (First/
    // Nearest/Last/Strongest) picks the single most valid target across both. only if that
    // winner is sprout-only does the oleander aim at the sprout instead, so the petal can
    // travel to it first
    protected override GameObject FindTarget()
    {
        List<Insect> candidates = new List<Insect>();
        Dictionary<Insect, OleanderSprout> viaSprout = GatherChainInsects();

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, insect.transform.position); // see Plant.FindNearest
            if (dist <= attackRange && IsValidNightTarget(insect, dist))
            {
                candidates.Add(insect);
                viaSprout.Remove(insect); // directly visible: no need to route through a sprout
            }
        }
        foreach (Insect insect in viaSprout.Keys)
            candidates.Add(insect);

        Insect winner = PickBestCandidate(candidates);
        if (winner == null) return null;
        return viaSprout.TryGetValue(winner, out OleanderSprout sprout) ? sprout.gameObject : winner.gameObject;
    }

    // extends the base single-circle check with the same sprout-chain reach FindTarget() itself
    // considers - used by FindTarget's own direct-visibility pass, and by anything else (e.g.
    // Carrot's Soil Bond) asking whether this oleander could target a specific insect right now
    public override bool CanReachInsect(Insect insect)
    {
        if (base.CanReachInsect(insect)) return true;
        return GatherChainInsects().ContainsKey(insect);
    }

    // BFS out from every sprout within attackRange (this oleander's own bounce search radius,
    // the same radius its petal uses once airborne), collecting every insect reachable through
    // that chain and which first-hop sprout leads to it
    private Dictionary<Insect, OleanderSprout> GatherChainInsects()
    {
        Dictionary<Insect, OleanderSprout> result = new Dictionary<Insect, OleanderSprout>();
        float range = BounceSearchRadius;

        foreach (OleanderSprout first in OleanderSprout.allSprouts)
        {
            if (first == null) continue;
            if (Vector3.Distance(transform.position, first.transform.position) > attackRange) continue;

            HashSet<OleanderSprout> visited = new HashSet<OleanderSprout> { first };
            Queue<OleanderSprout> frontier = new Queue<OleanderSprout>();
            frontier.Enqueue(first);

            while (frontier.Count > 0)
            {
                OleanderSprout current = frontier.Dequeue();

                foreach (Insect i in Insect.allInsects)
                {
                    if (i == null || !i.IsAlive || result.ContainsKey(i)) continue;
                    if (Vector3.Distance(current.transform.position, i.transform.position) <= range && IsVisibleToChain(i))
                        result[i] = first;
                }

                foreach (OleanderSprout other in OleanderSprout.allSprouts)
                {
                    if (other == null || visited.Contains(other)) continue;
                    if (Vector3.Distance(current.transform.position, other.transform.position) <= range)
                    {
                        visited.Add(other);
                        frontier.Enqueue(other);
                    }
                }
            }
        }
        return result;
    }

    // mirrors Plant's FindFirst/FindNearest/FindLast/FindStrongest scoring, but over a pool
    // that's already been admitted (direct insects passed attackRange, chained ones passed
    // chain reachability), so no range re-check happens here
    private Insect PickBestCandidate(List<Insect> candidates)
    {
        switch (targeting)
        {
            case TARGETING.Nearest:
            {
                Insect best = null;
                float bestDist = Mathf.Infinity;
                foreach (Insect insect in candidates)
                {
                    float dist = Vector3.Distance(transform.position, insect.transform.position);
                    if (dist < bestDist) { bestDist = dist; best = insect; }
                }
                return best;
            }
            case TARGETING.Strongest:
            {
                Insect best = null;
                float bestHealth = -1f;
                foreach (Insect insect in candidates)
                    if (insect.maxHealth > bestHealth) { bestHealth = insect.maxHealth; best = insect; }
                return best;
            }
            case TARGETING.Last:
            {
                Insect best = null;
                int lowestWaypointIndex = int.MaxValue;
                float furthestDistToNext = -1f;
                foreach (Insect insect in candidates)
                {
                    Transform waypoint = insect.GetCurrentWaypoint();
                    if (waypoint == null) continue;
                    if (insect.currentWaypointIndex < lowestWaypointIndex)
                    {
                        lowestWaypointIndex = insect.currentWaypointIndex;
                        furthestDistToNext = Vector3.Distance(insect.transform.position, waypoint.position);
                        best = insect;
                    }
                    else if (insect.currentWaypointIndex == lowestWaypointIndex)
                    {
                        float d = Vector3.Distance(insect.transform.position, waypoint.position);
                        if (d > furthestDistToNext) { furthestDistToNext = d; best = insect; }
                    }
                }
                return best;
            }
            default: // First
            {
                Insect best = null;
                int highestWaypointIndex = -1;
                float closestDistToNext = Mathf.Infinity;
                foreach (Insect insect in candidates)
                {
                    Transform waypoint = insect.GetCurrentWaypoint();
                    if (waypoint == null) continue;
                    if (insect.currentWaypointIndex > highestWaypointIndex)
                    {
                        highestWaypointIndex = insect.currentWaypointIndex;
                        closestDistToNext = Vector3.Distance(insect.transform.position, waypoint.position);
                        best = insect;
                    }
                    else if (insect.currentWaypointIndex == highestWaypointIndex)
                    {
                        float d = Vector3.Distance(insect.transform.position, waypoint.position);
                        if (d < closestDistToNext) { closestDistToNext = d; best = insect; }
                    }
                }
                return best;
            }
        }
    }

    // strict visibility for the sprout chain: no close-range exception like the base
    // IsValidNightTarget has, since "close to a sprout" doesn't mean the oleander can see it.
    // in the dark, an insect is only reachable through the chain if it's actually illuminated
    public static bool IsVisibleToChain(Insect insect)
    {
        if (DarknessManager.instance == null || !DarknessManager.instance.isDark) return true;
        return DarknessManager.instance.IsIlluminated(insect.transform.position);
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        NeriumOleanderProjectile petal = proj.GetComponent<NeriumOleanderProjectile>();
        if (petal != null)
        {
            petal.SetTarget(FindTarget());
            petal.Initialize(target, attackDamage, projectileSpeed, maxRange, 0, damageType, elementalType, this);
            // Venom Chain (skill tree): +1 bounce whenever her own Oleander Sprout is standing
            int venomChainBonus = (currentSprout != null && SkillTreeManager.HasUnlock(this, VenomChainUnlock)) ? 1 : 0;
            petal.SetBounceData(1 + piercing + venomChainBonus, BounceSearchRadius, OleanderData?.bounceDamageReduction ?? 0.1f);
        }
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginTargeting(BounceSearchRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        skillCooldownTimer = skillCooldown;

        // only one sprout per Oleander at a time; recasting replaces it
        DestroyCurrentSprout();

        if (sproutPrefab == null) return;
        GameObject obj = Instantiate(sproutPrefab, position, Quaternion.identity);
        OleanderSprout sprout = obj.GetComponent<OleanderSprout>();
        if (sprout == null) sprout = obj.AddComponent<OleanderSprout>();
        sprout.Initialize(this, BounceSearchRadius, SproutDuration, CurseReduction);
        currentSprout = sprout;
    }

    // click Auto Cast to lock in an area, click again to turn it off
    public override void ToggleAutoCast()
    {
        if (autoCastEnabled)
        {
            autoCastEnabled = false;
            return;
        }
        SkillTargetingManager.instance.BeginTargeting(BounceSearchRadius, OnAutoCastTargetConfirmed);
    }

    private void OnAutoCastTargetConfirmed(Vector3 position)
    {
        autoCastEnabled = true;
        autoCastPosition = position;
    }

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled, targetPosition = autoCastPosition };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled) return;
        autoCastEnabled = true;
        autoCastPosition = state.targetPosition;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (OleanderData?.path1AttackDamagePerLevel ?? 10f) * level;
        baseAttackRange  = data.baseAttackRange  + (OleanderData?.path1AttackRangePerLevel  ?? 0.5f) * level;
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override string GetName() => $"<b><color=purple>{(data != null ? data.displayName : "Nerium Oleander")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} fires toxic petals that bounce between insects, while nearby allies are granted <color=#2ECC71>Oleandic Ward</color>, which strips and locks out their debuffs - a dedicated answer to debuffer insects.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = OleanderData?.path1AttackDamagePerLevel  ?? 10f;
        float arpl = OleanderData?.path1AttackRangePerLevel   ?? 0.5f;
        int   bpl  = OleanderData?.path1BouncePerLevel        ?? 1;
        string desc = details
            ? $"Fires a toxic petal at the target dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}. The petal bounces to <color=green><b>[(1) + Piercing]</b></color> additional target(s). The petal deals <color=green><b>{(OleanderData?.bounceDamageReduction ?? 0.1f) * 100f:F0}%</b></color> reduced damage per bounce."
            : $"Fires a toxic petal at the target dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}. The petal bounces to <color=green><b>{1 + piercing}</b></color> additional target(s). The petal deals <color=green><b>{(OleanderData?.bounceDamageReduction ?? 0.1f) * 100f:F0}%</b></color> reduced damage per bounce.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{arpl:F1}</b></color> per level. [<color=green><b>+{arpl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Increase <color=green><b>Piercing</b></color> by <color=green><b>{bpl}</b></color> per level. [<color=green><b>+{bpl * effectivePath1Level}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Attacks extend <color=purple><b>Poison</b></color>-Element negative status effects on targets by <color=green><b>{OleanderData?.path1MaxPoisonExtendPerHit ?? 1f:F0}</b></color> second per hit. Petals may bounce back to older targets.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float durpl = OleanderData?.path2WardDurationPerLevel ?? 2f;
        string desc = details
            ? $"Allies within <color=green><b>Attack Range</b></color> are granted <color=#2ECC71><b>Oleandic Ward</b></color> for <color=green><b>[({OleanderData?.baseWardDuration ?? 6f:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds.\n\n" +
              $"<color=#2ECC71><b><u>Oleandic Ward</u></b></color>\n" +
              $"Cleanses a random debuff, and locks them out of receiving that debuff while the effect is active. Grants a weak heal of <color=green><b>{WardHealAmount:F0}</b></color> on application, and regenerates <color=green><b>{WardRegenPerLock:F0}</b></color> Health per second for every debuff currently locked."
            : $"Allies within <color=green><b>Attack Range</b></color> are granted <color=#2ECC71><b>Oleandic Ward</b></color> for <color=green><b>{WardDuration:F1}</b></color> seconds.\n\n" +
              $"<color=#2ECC71><b><u>Oleandic Ward</u></b></color>\n" +
              $"Cleanses a random debuff, and locks them out of receiving that debuff while the effect is active. Grants a weak heal of <color=green><b>{WardHealAmount:F0}</b></color> on application, and regenerates <color=green><b>{WardRegenPerLock:F0}</b></color> Health per second for every debuff currently locked.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"For each debuff locked, also grant <color=#FF69B4><b>{(OleanderData?.path2MaxMagicResistancePerLock ?? 0.05f) * 100f:F0}% Magic Resistance</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl   = OleanderData?.path3SproutDurationPerLevel   ?? 3f;
        float curvepl = OleanderData?.path3CurseReductionPerLevel   ?? 0.01f;
        int   bouncepl = OleanderData?.path3MaxBounceBonus          ?? 3;
        string desc = details
            ? $"Place down an <color=purple><b>Oleander Sprout</b></color> for <color=green><b>[({OleanderData?.baseSproutDuration ?? 15f:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. Attacks may bounce on and off the sprout without reducing the bounce count. While the sprout is active, insects within its <color=green><b>Attack Range</b></color> are inflicted with <color=#9B59B6><b>Oleandic Curse</b></color>, reducing <color=purple><b>Poison Resistance</b></color> by <color=green><b>[({(OleanderData?.baseCursePoisonResistReduction ?? 0.04f) * 100f:F0}%) + ({curvepl * 100f:F0}%/Lvl.)]</b></color>."
            : $"Place down an <color=purple><b>Oleander Sprout</b></color> for <color=green><b>{SproutDuration:F0}</b></color> seconds. Attacks may bounce on and off the sprout without reducing the bounce count. While the sprout is active, insects within its <color=green><b>Attack Range</b></color> are inflicted with <color=#9B59B6><b>Oleandic Curse</b></color>, reducing <color=purple><b>Poison Resistance</b></color> by <color=green><b>{CurseReduction * 100f:F0}%</b></color>.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase sprout duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase curse strength by <color=green><b>{curvepl * 100f:F0}%</b></color> per level. [<color=green><b>+{curvepl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"When attacks hit the sprout, increase bounce count by <color=green><b>{bouncepl}</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
