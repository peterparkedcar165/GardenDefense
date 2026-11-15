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

    private PlantRegistry plantRegistry;
    private MulchRegistry mulchRegistry;

    private readonly Queue<MulchTier> pendingGrants = new Queue<MulchTier>();
    private readonly List<MulchData> pickedData = new List<MulchData>();
    private readonly List<MulchEffect> activeEffects = new List<MulchEffect>();

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
        pendingGrants.Enqueue(tier);
    }

    // builds 3 weighted-random choices for the oldest queued grant, without consuming it - only
    // CommitChosen actually dequeues, so backing out of the picker costs nothing
    public MulchData[] GenerateChoicesForOldest()
    {
        if (pendingGrants.Count == 0) return null;
        rerolledAway.Clear(); // a fresh screen - anything rerolled away on a previous one is fair game again
        MulchTier tier = pendingGrants.Peek();
        List<MulchData> eligible = GetEligible(tier);
        return WeightedPickDistinct(eligible, DraftChoiceCount).ToArray();
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
        pendingGrants.Dequeue();

        // GetEligible already keeps an already-picked mulch out of every future draft/reroll, so
        // this shouldn't normally be reachable - kept as a hard backstop against double-picking
        // (and therefore double-stacking) the same mulch off a stale/duplicate card reference.
        // the grant above is still consumed either way, same as a normal pick, rather than
        // leaving the queue stuck on an unpickable choice forever
        if (pickedData.Contains(chosen)) return;
        pickedData.Add(chosen);

        MulchEffect effect = chosen.CreateEffect();
        if (effect == null) return;
        activeEffects.Add(effect);
        effect.Activate();
    }

    // excludes mulches already picked this level, so the same stacking mulch can't flood every
    // future draft - revisit if you ever want deliberate duplicate-stacking instead. pulls from
    // both the universal registry and this level's exclusive pool
    private List<MulchData> GetEligible(MulchTier tier, List<MulchData> alsoExclude = null)
    {
        List<MulchData> result = new List<MulchData>();

        List<PlantData> loadout = GetCurrentLoadout();
        int currentWave = GameManager.instance != null ? GameManager.instance.currentWave : 0;

        AddEligibleFrom(mulchRegistry != null ? mulchRegistry.mulches : null, tier, loadout, currentWave, result, alsoExclude);
        AddEligibleFrom(levelPool, tier, loadout, currentWave, result, alsoExclude);

        return result;
    }

    private void AddEligibleFrom(MulchData[] source, MulchTier tier, List<PlantData> loadout, int currentWave, List<MulchData> result, List<MulchData> alsoExclude)
    {
        if (source == null) return;
        foreach (MulchData data in source)
        {
            if (data == null || data.tier != tier) continue;
            if (pickedData.Contains(data)) continue;
            if (rerolledAway.Contains(data)) continue;
            if (alsoExclude != null && alsoExclude.Contains(data)) continue;
            if (result.Contains(data)) continue;
            if (data.condition != null && !data.condition.IsEligible(loadout, currentWave)) continue;
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
