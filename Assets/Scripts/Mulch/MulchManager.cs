using UnityEngine;
using System.Collections.Generic;

// mulch counterpart to FertilizerManager's mid-level queue - session-only by design, same
// lifecycle (ResetMulch is called per level start). unlike fertilizers, mulch has no numeric
// roll: a draft is 3 eligible MulchData assets of the queued tier, weighted by rollWeight, and
// picking one just activates its paired MulchEffect for the rest of the level. mulches stack -
// each grant adds another active effect on top of whatever was already picked
public class MulchManager : MonoBehaviour
{
    public static MulchManager instance;

    // how many choices one draft offers - shared with MulchSelectionUI, which pads the display
    // with "EMPTY" placeholder cards if GenerateChoicesForOldest comes up short of this many
    public const int DraftChoiceCount = 3;

    // global modifiers a mulch effect can nudge instead of needing a per-instance hook into
    // every insect/entity (there's no central "ApplyTo" pass for insects the way plants have
    // one) - read directly by the base game code they modify, reset to their neutral value by
    // ResetMulch. additive ones default to 0, multiplicative ones default to 1
    public static float GlobalSlowBonus = 0f;
    public static float LeakDamageMultiplier = 1f;
    public static float PrimerCooldownMultiplier = 1f;
    public static float BurnDurationMultiplier = 1f;
    public static float RegenerationHealMultiplier = 1f;
    // read directly by Entity.cs's Freeze+Fire branch - MeltActive swaps the base instant-shatter
    // reaction for Melt's gradual chip-away-the-duration version
    public static bool MeltActive = false;
    public static float MeltFreezeDurationReduction = 0f;
    public static float MeltDamageMultiplier = 1f;
    // market day: what placing a plant costs (GetPlacementCost) and what a path upgrade costs
    // (Plant.PathCost), as multipliers
    public static float PlacementCostMultiplier = 1f;
    public static float UpgradeCostMultiplier = 1f;
    // dormancy: every plant's damage to insects (Entity.Damage). drought: the sun from every
    // Plant.GenerateSun call, so at zero sun producers make nothing at all
    public static float PlantDamageMultiplier = 1f;
    public static float SunProductionMultiplier = 1f;

    private PlantRegistry plantRegistry;
    private MulchRegistry mulchRegistry;

    // one queued draft. a swap draft (seed swap's replacement) offers sameTierChoices of its own
    // tier plus higherTierChoices from the tier above, and never offers seed swap or seed sell
    private struct PendingDraft
    {
        public MulchTier tier;
        public bool isSwap;
        public int sameTierChoices;
        public int higherTierChoices;
    }

    private readonly List<PendingDraft> pendingGrants = new List<PendingDraft>();
    private readonly List<MulchData> pickedData = new List<MulchData>();
    private readonly List<MulchEffect> activeEffects = new List<MulchEffect>();

    // mulches seed swap or seed sell took away, kept out of every draft for the rest of the level
    private readonly HashSet<MulchData> removedData = new HashSet<MulchData>();

    // mulches the player has rerolled away - excluded from every reroll for the rest of THIS
    // selection screen (so rerolling one card can't bring back what another card just rerolled
    // away), but cleared the next time a fresh draft is generated (see GenerateChoicesForOldest),
    // so the same mulch is fair game again on a later screen
    private readonly HashSet<MulchData> rerolledAway = new HashSet<MulchData>();

    // level-exclusive mulches only - the universal pool lives in mulchRegistry instead, so
    // adding a new mulch that every level should offer never requires touching LevelConfig
    private MulchData[] levelPool;

    public int PendingMulchCount => pendingGrants.Count;
    public IReadOnlyList<MulchData> ActiveMulches => pickedData;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        plantRegistry = Resources.Load<PlantRegistry>("PlantRegistry");
        if (plantRegistry == null)
            Debug.LogError("MulchManager: PlantRegistry not found at Resources/PlantRegistry.asset");
        mulchRegistry = Resources.Load<MulchRegistry>("MulchRegistry");
        if (mulchRegistry == null)
            Debug.LogError("MulchManager: MulchRegistry not found at Resources/MulchRegistry.asset");
    }

    // called once per level start (ProceduralLevel.Start), same spot as
    // FertilizerManager.ResetFertilizerQueue
    public void ResetMulch(MulchData[] levelExclusivePool)
    {
        levelPool = levelExclusivePool;
        pendingGrants.Clear();
        pickedData.Clear();
        rerolledAway.Clear();
        removedData.Clear();

        foreach (MulchEffect effect in activeEffects)
            effect?.Deactivate();
        activeEffects.Clear();

        GlobalSlowBonus = 0f;
        LeakDamageMultiplier = 1f;
        PrimerCooldownMultiplier = 1f;
        BurnDurationMultiplier = 1f;
        RegenerationHealMultiplier = 1f;
        MeltActive = false;
        MeltFreezeDurationReduction = 0f;
        MeltDamageMultiplier = 1f;
        PlacementCostMultiplier = 1f;
        UpgradeCostMultiplier = 1f;
        PlantDamageMultiplier = 1f;
        SunProductionMultiplier = 1f;
    }

    // called by Plant.cs whenever a plant is placed, next to FertilizerManager.ApplyTo/
    // SkillTreeManager.ApplyTo - lets every active mulch effect react to a plant that didn't
    // exist yet when it was picked (see MulchEffect.OnPlantPlaced)
    public void NotifyPlantPlaced(Plant plant)
    {
        foreach (MulchEffect effect in activeEffects)
            effect?.OnPlantPlaced(plant);
    }

    // the hooks below loop over a copy, since an effect reacting to one of them can grant or
    // remove mulches while the loop is still running
    public void NotifyWaveStarted(int wave)
    {
        foreach (MulchEffect effect in new List<MulchEffect>(activeEffects))
            effect?.OnWaveStarted(wave);
    }

    public void NotifyWaveEnded(int wave)
    {
        foreach (MulchEffect effect in new List<MulchEffect>(activeEffects))
            effect?.OnWaveEnded(wave);
    }

    public void NotifyInsectLeaked(Insect insect)
    {
        foreach (MulchEffect effect in new List<MulchEffect>(activeEffects))
            effect?.OnInsectLeaked(insect);
    }

    // called by Plant.GenerateSun with the sun actually banked
    public void NotifySunGenerated(Plant plant, int amount, bool fromKill)
    {
        foreach (MulchEffect effect in new List<MulchEffect>(activeEffects))
            effect?.OnSunGenerated(plant, amount, fromKill);
    }

    // what placing a fresh plant of this species costs right now: the skill tree's reduction,
    // then market day's multiplier, then flat discounts (bargain seeds), and nothing at all while
    // a free placement (bud) is waiting. Tile charges this and PlantSlotButton shows it, so the
    // shown and charged cost always agree
    public static int GetPlacementCost(PlantData data)
    {
        int cost = SkillTreeManager.GetEffectiveSunCost(data);
        if (data == null) return cost;
        cost = Mathf.RoundToInt(cost * Mathf.Max(0f, PlacementCostMultiplier));
        if (instance == null) return cost;

        int discount = 0;
        foreach (MulchEffect effect in instance.activeEffects)
        {
            if (effect == null) continue;
            if (effect.GrantsFreePlacement) return 0;
            discount += effect.PlacementDiscount;
        }
        return Mathf.Max(0, cost - discount);
    }

    // called by Tile right after a bought plant is placed, so one time discounts get used up.
    // checked before any effect reacts, so the free placement that paid for this plant is
    // what every effect sees
    public void NotifyPlantBought(Plant plant)
    {
        bool placedForFree = false;
        foreach (MulchEffect effect in activeEffects)
            if (effect != null && effect.GrantsFreePlacement) { placedForFree = true; break; }

        foreach (MulchEffect effect in new List<MulchEffect>(activeEffects))
            effect?.OnPlantBought(plant, placedForFree);
    }

    // called when a level's MulchGrant fires (its configured wave just completed)
    public void GrantMulch(MulchTier tier)
    {
        pendingGrants.Add(new PendingDraft { tier = tier });
    }

    // seed swap: the replacement draft goes to the front of the queue, so it's the one that opens
    // next (MulchSelectionUI opens it straight away)
    public void GrantSwapDraft(MulchTier tier, int sameTierChoices, int higherTierChoices)
    {
        pendingGrants.Insert(0, new PendingDraft
        {
            tier = tier,
            isSwap = true,
            sameTierChoices = Mathf.Max(0, sameTierChoices),
            higherTierChoices = Mathf.Max(0, higherTierChoices),
        });
    }

    // builds 3 weighted-random choices for the oldest queued grant, without consuming it - only
    // CommitChosen actually dequeues, so backing out of the picker costs nothing
    public MulchData[] GenerateChoicesForOldest()
    {
        if (pendingGrants.Count == 0) return null;
        rerolledAway.Clear(); // a fresh screen - anything rerolled away on a previous one is fair game again
        PendingDraft draft = pendingGrants[0];
        if (draft.isSwap) return GenerateSwapChoices(draft).ToArray();
        List<MulchData> eligible = GetEligible(draft.tier);
        return WeightedPickDistinct(eligible, DraftChoiceCount).ToArray();
    }

    // equal or higher rarity only: the higher choices come from the tier above, and any slot a tier
    // can't fill is topped up from the same tier first, then from the tiers above it
    private List<MulchData> GenerateSwapChoices(PendingDraft draft)
    {
        int total = draft.sameTierChoices + draft.higherTierChoices;
        List<MulchData> picks = WeightedPickDistinct(GetEligible(draft.tier), draft.sameTierChoices);
        if (draft.tier < MulchTier.Legendary)
            picks.AddRange(WeightedPickDistinct(GetEligible(draft.tier + 1, picks), draft.higherTierChoices));
        for (MulchTier tier = draft.tier; picks.Count < total && tier <= MulchTier.Legendary; tier++)
            picks.AddRange(WeightedPickDistinct(GetEligible(tier, picks), total - picks.Count));
        return picks;
    }

    // rerolls a single displayed choice in place. the card being rerolled away is treated as a
    // deliberate pass for the rest of THIS screen - it won't come back on another card's reroll
    // here, but it's fair game again once a fresh draft is generated (see rerolledAway).
    // alsoDisplayed is whatever the OTHER cards in this same draft
    // are currently showing - excluded for just this one roll (not blacklisted level-wide) so the
    // replacement can never collide with a mulch already sitting on another card. without this,
    // two cards could end up showing the same mulch at once (GetEligible has no way to know what
    // the rest of the draft looks like on its own)
    public MulchData RerollSingle(MulchData current, List<MulchData> alsoDisplayed = null)
    {
        if (current != null) rerolledAway.Add(current);
        List<MulchData> eligible = GetEligible(current.tier, alsoDisplayed);
        List<MulchData> picked = WeightedPickDistinct(eligible, 1);
        return picked.Count > 0 ? picked[0] : null;
    }

    // consumes the oldest queued grant, activates the chosen mulch's effect, and keeps it active
    // (stacked on top of any previously picked mulches) for the rest of the level
    public void CommitChosen(MulchData chosen)
    {
        if (pendingGrants.Count == 0 || chosen == null) return;
        // seed swap and seed sell are spent through CommitChosenOnTarget instead
        if (chosen.NeedsOwnedMulchTarget) return;
        pendingGrants.RemoveAt(0);

        // GetEligible already keeps an already-picked mulch out of every future draft/reroll, so
        // this shouldn't normally be reachable - kept as a hard backstop against double-picking
        // (and therefore double-stacking) the same mulch off a stale/duplicate card reference.
        // the grant above is still consumed either way, same as a normal pick, rather than
        // leaving the queue stuck on an unpickable choice forever
        if (pickedData.Contains(chosen)) return;
        ActivateMulch(chosen);
    }

    private void ActivateMulch(MulchData chosen)
    {
        pickedData.Add(chosen);
        MulchEffect effect = chosen.CreateEffect();
        if (effect == null) return;
        activeEffects.Add(effect);
        effect.Activate();
    }

    // dev test menu only (MulchSelectionUI, N key in the editor): every mulch a draft in this level
    // could ever offer, the universal registry plus this level's own pool, conditions ignored
    public List<MulchData> DebugAllMulches()
    {
        List<MulchData> result = new List<MulchData>();
        foreach (MulchData[] source in new[] { mulchRegistry != null ? mulchRegistry.mulches : null, levelPool })
            if (source != null)
                foreach (MulchData data in source)
                    if (data != null && !result.Contains(data)) result.Add(data);
        return result;
    }

    public bool IsOwned(MulchData data) => data != null && pickedData.Contains(data);

    // an owned mulch's live status line for the active effects window (G), null if it has none
    public string GetStatus(MulchData data) => FindEffect(data)?.GetStatus();

    // dev test menu: activates a mulch right away, skipping the queue and every eligibility check
    public bool DebugActivate(MulchData data)
    {
        if (data == null || data.NeedsOwnedMulchTarget || pickedData.Contains(data)) return false;
        removedData.Remove(data);
        ActivateMulch(data);
        return true;
    }

    // dev test menu: takes an owned mulch away so it can be tested again, paid out or not
    public bool DebugRemove(MulchData data)
    {
        if (data == null || !pickedData.Contains(data)) return false;
        MulchEffect effect = FindEffect(data);
        if (effect != null)
        {
            effect.Deactivate();
            activeEffects.Remove(effect);
        }
        pickedData.Remove(data);
        return true;
    }

    // dev test menu: seed swap or seed sell used on an owned mulch without spending a queued draft
    public bool DebugUseTool(MulchData tool, MulchData target)
    {
        if (tool == null || !tool.NeedsOwnedMulchTarget || !CanRemove(target, tool)) return false;
        RemoveMulch(target, tool);
        tool.UseOnTarget(target);
        return true;
    }

    // seed swap and seed sell: uses up the oldest grant like a normal pick, takes the chosen owned
    // mulch away, then lets the tool do its part (pay sun, or queue the replacement draft). the
    // tool is spent on the spot and never joins the owned mulches, so it can show up again in a
    // later draft
    public bool CommitChosenOnTarget(MulchData chosen, MulchData target)
    {
        if (pendingGrants.Count == 0 || chosen == null || !chosen.NeedsOwnedMulchTarget) return false;
        if (!CanRemove(target, chosen)) return false;
        pendingGrants.RemoveAt(0);
        RemoveMulch(target, chosen);
        chosen.UseOnTarget(target);
        return true;
    }

    // owned mulches the given tool (seed swap or seed sell) may still take away, oldest pick first
    public List<MulchData> GetRemovableMulches(MulchData tool = null)
    {
        List<MulchData> result = new List<MulchData>();
        foreach (MulchData data in pickedData)
            if (CanRemove(data, tool)) result.Add(data);
        return result;
    }

    public bool HasRemovableMulch(MulchData tool = null)
    {
        foreach (MulchData data in pickedData)
            if (CanRemove(data, tool)) return true;
        return false;
    }

    // seed swap (or no tool): false once a mulch paid out (MulchEffect.CanBeRemoved). seed sell
    // (MulchData.TakesPaidOutMulches): paid out mulches too, only weather stays (MulchEffect.CanBeSold)
    public bool CanRemove(MulchData data, MulchData tool = null)
    {
        if (data == null || !pickedData.Contains(data)) return false;
        MulchEffect effect = FindEffect(data);
        if (effect == null) return true;
        return tool != null && tool.TakesPaidOutMulches ? effect.CanBeSold : effect.CanBeRemoved;
    }

    // takes an owned mulch away again: its effect is deactivated, which undoes whatever it's still
    // doing (a paid out mulch keeps what it gave), and it stays out of every draft for the rest of
    // the level
    public bool RemoveMulch(MulchData data, MulchData tool = null)
    {
        if (!CanRemove(data, tool)) return false;
        MulchEffect effect = FindEffect(data);
        if (effect != null)
        {
            effect.Deactivate();
            activeEffects.Remove(effect);
        }
        pickedData.Remove(data);
        removedData.Add(data);
        return true;
    }

    private MulchEffect FindEffect(MulchData data)
    {
        foreach (MulchEffect effect in activeEffects)
            if (effect != null && effect.Data == data) return effect;
        return null;
    }

    // excludes mulches already picked this level, so the same stacking mulch can't flood every
    // future draft - revisit if you ever want deliberate duplicate-stacking instead. pulls from
    // both the universal registry and this level's exclusive pool
    private List<MulchData> GetEligible(MulchTier tier, List<MulchData> alsoExclude = null)
    {
        List<MulchData> result = new List<MulchData>();

        List<PlantData> loadout = GetCurrentLoadout();
        int currentWave = GameManager.instance != null ? GameManager.instance.currentWave : 0;
        // a swap draft never offers another swap or sell
        bool excludeTools = pendingGrants.Count > 0 && pendingGrants[0].isSwap;

        AddEligibleFrom(mulchRegistry != null ? mulchRegistry.mulches : null, tier, loadout, currentWave, result, alsoExclude, excludeTools);
        AddEligibleFrom(levelPool, tier, loadout, currentWave, result, alsoExclude, excludeTools);

        return result;
    }

    private void AddEligibleFrom(MulchData[] source, MulchTier tier, List<PlantData> loadout, int currentWave, List<MulchData> result, List<MulchData> alsoExclude, bool excludeTools)
    {
        if (source == null) return;
        foreach (MulchData data in source)
        {
            if (data == null || data.tier != tier) continue;
            if (pickedData.Contains(data)) continue;
            if (removedData.Contains(data)) continue;
            if (excludeTools && data.NeedsOwnedMulchTarget) continue;
            if (rerolledAway.Contains(data)) continue;
            if (alsoExclude != null && alsoExclude.Contains(data)) continue;
            if (result.Contains(data)) continue;
            if (data.condition != null && !data.condition.IsEligible(loadout, currentWave)) continue;
            if (!data.MeetsExtraRequirements(loadout, currentWave)) continue;
            result.Add(data);
        }
    }

    private List<PlantData> GetCurrentLoadout()
    {
        List<PlantData> result = new List<PlantData>();
        if (SaveManager.instance == null || plantRegistry == null || plantRegistry.plants == null) return result;
        foreach (string plantName in SaveManager.instance.selectedLoadout)
            foreach (PlantData data in plantRegistry.plants)
                if (data != null && data.plantName == plantName) { result.Add(data); break; }
        return result;
    }

    // weighted sampling without replacement - each pick draws proportionally to rollWeight among
    // whatever's left, so a removed candidate never skews the remaining odds
    private static List<MulchData> WeightedPickDistinct(List<MulchData> candidates, int count)
    {
        List<MulchData> remaining = new List<MulchData>(candidates);
        List<MulchData> picked = new List<MulchData>();

        while (picked.Count < count && remaining.Count > 0)
        {
            float totalWeight = 0f;
            foreach (MulchData data in remaining) totalWeight += Mathf.Max(0f, data.rollWeight);

            // a rollWeight of 0 means "never draft this" - if that's all that's left, stop
            // rather than falling back to a uniform pick among them
            if (totalWeight <= 0f) break;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            int chosenIndex = remaining.Count - 1;
            for (int i = 0; i < remaining.Count; i++)
            {
                cumulative += Mathf.Max(0f, remaining[i].rollWeight);
                if (roll <= cumulative) { chosenIndex = i; break; }
            }

            picked.Add(remaining[chosenIndex]);
            remaining.RemoveAt(chosenIndex);
        }

        return picked;
    }
}
