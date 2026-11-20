using UnityEngine;
using System.Collections.Generic;

// loot mulches for the level: the player's inventory, equipping onto plants, merging, selling, and
// the drops themselves. session only, reset at every level start (ProceduralLevel.Start).
//
// drops: every enemy insect that dies rolls baseDropChance, raised by the killing plant's Item Drop
// Rate; after pityKills kills in a row without one, the next kill always drops (bad luck
// protection). on top of that, every wave except the last guarantees one mulch where its last
// insect died or leaked (its own roll, so a lucky last kill drops two). a drop's rarity uses the
// killing plant's Rare Item Drop Rate. both rates are points with diminishing returns:
// bonus = X / (X + 100)
public class MulchManager : MonoBehaviour
{
    public static MulchManager instance;

    // the inventory or an equip changed: the inventory bar and the plant slots redraw
    public static event System.Action OnChanged;

    public MulchRegistry Registry { get; private set; }

    private MulchItem[] inventory = new MulchItem[8];
    private int killsSinceDrop;

    // the guaranteed end of wave drop. every enemy insect is counted under the wave it spawned in;
    // once that wave's spawning is over and its count is back to 0, its last insect was the last one
    private readonly Dictionary<int, int> aliveByWave = new Dictionary<int, int>();
    private readonly HashSet<int> spawningDone = new HashSet<int>();
    private readonly HashSet<int> guaranteedHandled = new HashSet<int>();
    private readonly Dictionary<int, (Vector3 position, Plant killer, bool leaked)> lastExit =
        new Dictionary<int, (Vector3, Plant, bool)>();

    public IReadOnlyList<MulchItem> Inventory => inventory;
    public int InventorySize => inventory.Length;
    public int PlantSlots => Registry != null ? Mathf.Max(1, Registry.plantSlots) : 6;

    // a ground drop's clock: real time, frozen while the game is paused, never sped up
    public static float DropDeltaTime => Time.timeScale > 0f ? Time.unscaledDeltaTime : 0f;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Registry = Resources.Load<MulchRegistry>("MulchRegistry");
        if (Registry == null)
            Debug.LogError("MulchManager: MulchRegistry not found at Resources/MulchRegistry.asset");
        inventory = new MulchItem[Registry != null ? Mathf.Max(1, Registry.inventorySize) : 8];
    }

    // called once per level start (ProceduralLevel.Start): the inventory empties every level
    public void ResetForLevel()
    {
        inventory = new MulchItem[Registry != null ? Mathf.Max(1, Registry.inventorySize) : 8];
        killsSinceDrop = 0;
        aliveByWave.Clear();
        spawningDone.Clear();
        guaranteedHandled.Clear();
        lastExit.Clear();
        MulchDrop.ClearAll();
        MulchInventoryUI.Ensure();
        RaiseChanged();
    }

    public static void RaiseChanged() => OnChanged?.Invoke();

    // ── inventory ───────────────────────────────────────────────────────────

    public bool HasRoom
    {
        get
        {
            foreach (MulchItem item in inventory)
                if (item == null) return true;
            return false;
        }
    }

    public bool AddToInventory(MulchItem item) => AddToInventorySlot(item) >= 0;

    // the first empty slot it went into, -1 when the inventory is full
    public int AddToInventorySlot(MulchItem item)
    {
        if (item == null) return -1;
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] != null) continue;
            inventory[i] = item;
            RaiseChanged();
            return i;
        }
        return -1;
    }

    public MulchItem GetSlot(int slot) => slot >= 0 && slot < inventory.Length ? inventory[slot] : null;

    // dragging one inventory slot onto another: merges two identical copies into the next rarity,
    // otherwise moves into an empty slot or swaps the two
    public void MoveOrMerge(int from, int to)
    {
        if (from == to || GetSlot(from) == null || to < 0 || to >= inventory.Length) return;
        MulchItem moving = inventory[from];
        MulchItem target = inventory[to];
        if (target != null && moving.CanMergeWith(target))
        {
            inventory[to] = moving.Merged();
            inventory[from] = null;
            AnnounceMerge(inventory[to], null);
        }
        else
        {
            inventory[to] = moving;
            inventory[from] = target;
        }
        RaiseChanged();
    }

    public int SellPrice(MulchItem item) =>
        item == null || Registry == null ? 0 : Registry.sellPrice.GetInt(item.rarity);

    // right click in the inventory
    public void Sell(int slot)
    {
        MulchItem item = GetSlot(slot);
        if (item == null) return;
        inventory[slot] = null;
        int price = SellPrice(item);
        if (price > 0) GameManager.instance?.AddSun(price);
        RaiseChanged();
    }

    // ── equipping ───────────────────────────────────────────────────────────

    public enum EquipResult { Equipped, Merged, SlotsFull, Invalid }

    // dragging an inventory slot onto a plant (on the map or onto its slots in the plant row)
    public EquipResult EquipFromSlot(int slot, Plant plant)
    {
        MulchItem item = GetSlot(slot);
        EquipResult result = Equip(item, plant);
        if (result == EquipResult.Equipped || result == EquipResult.Merged)
        {
            inventory[slot] = null;
            RaiseChanged();
        }
        else if (result == EquipResult.SlotsFull && plant != null)
        {
            StatusIndicator.Spawn(plant.transform.position + new Vector3(0.4f, 0f, 0f), "Mulch slots full", new Color(1f, 0.45f, 0.4f));
        }
        return result;
    }

    // a plant that already has this mulch at this rarity merges it into the next rarity in place;
    // anything else takes a free slot. equipped mulches can't be taken off again
    public EquipResult Equip(MulchItem item, Plant plant)
    {
        if (item == null || item.data == null || plant == null || !plant.IsAlive) return EquipResult.Invalid;

        foreach (MulchEffect owned in plant.mulches)
        {
            if (owned == null || !item.CanMergeWith(owned.Item)) continue;
            owned.Upgrade(owned.Item.Merged());
            AnnounceMerge(owned.Item, plant);
            RaiseChanged();
            return EquipResult.Merged;
        }

        if (plant.mulches.Count >= PlantSlots) return EquipResult.SlotsFull;

        MulchEffect effect = item.data.CreateEffect(item);
        if (effect == null) return EquipResult.Invalid;
        effect.AttachTo(plant);
        plant.mulches.Add(effect);
        effect.Activate();
        StatusIndicator.Spawn(plant.transform.position + new Vector3(0.4f, 0f, 0f), "Mulch!", MulchTierColors.Get(item.rarity));
        TutorialManager.instance?.NotifyEvent(TutorialEvents.EquipMulch);
        RaiseChanged();
        return EquipResult.Equipped;
    }

    private static void AnnounceMerge(MulchItem merged, Plant plant)
    {
        Vector3 at = plant != null ? plant.transform.position + new Vector3(0.4f, 0f, 0f) : Vector3.zero;
        if (plant != null) StatusIndicator.Spawn(at, $"{merged.rarity}!", MulchTierColors.Get(merged.rarity));
        if (GameManager.instance != null && GameManager.instance.plantSelect != null)
            GameManager.instance.PlaySound(GameManager.instance.plantSelect);
    }

    // the plant died: its mulches stop and go into its dead record. only a revive by a max level
    // Rhodiola skill brings them back (RestoreMulches); any other revive leaves them lost
    public void OnPlantDied(Plant plant, DeadPlantRecord record)
    {
        if (plant == null) return;
        foreach (MulchEffect effect in plant.mulches)
            effect?.Deactivate();
        if (record != null) record.mulches = new List<MulchEffect>(plant.mulches);
        plant.mulches.Clear();
        RaiseChanged();
    }

    // the plant left the field any other way (uprooted, or the level unloading): its mulches stop
    // for good
    public static void OnPlantRemoved(Plant plant)
    {
        if (plant == null || plant.mulches.Count == 0) return;
        foreach (MulchEffect effect in plant.mulches)
            effect?.Deactivate();
        plant.mulches.Clear();
        RaiseChanged();
    }

    // a max level Rhodiola skill revive: the same effects carry on on the new body, with whatever
    // they had counted so far
    public static void RestoreMulches(DeadPlantRecord record, Plant revived)
    {
        if (record == null || record.mulches == null || revived == null) return;
        foreach (MulchEffect effect in record.mulches)
        {
            if (effect == null) continue;
            effect.AttachTo(revived);
            revived.mulches.Add(effect);
            effect.Activate();
        }
        record.mulches = null;
        RaiseChanged();
    }

    // ── hooks ───────────────────────────────────────────────────────────────

    public void NotifyWaveStarted(int wave)
    {
        foreach (MulchEffect effect in AllEquipped())
            effect.OnWaveStarted(wave);
    }

    public void NotifyWaveEnded(int wave)
    {
        foreach (MulchEffect effect in AllEquipped())
            effect.OnWaveEnded(wave);
    }

    // called by Plant.GenerateSun with the sun actually banked
    public static void NotifySunGenerated(Plant plant, int amount, bool fromKill)
    {
        if (plant == null || plant.mulches.Count == 0) return;
        foreach (MulchEffect effect in new List<MulchEffect>(plant.mulches))
            effect?.OnSunGenerated(amount, fromKill);
    }

    // a copy, since an effect reacting to a hook can change what's equipped
    private static List<MulchEffect> AllEquipped()
    {
        List<MulchEffect> result = new List<MulchEffect>();
        foreach (Plant plant in Plant.allPlants)
            if (plant != null && plant.IsAlive)
                foreach (MulchEffect effect in plant.mulches)
                    if (effect != null) result.Add(effect);
        return result;
    }

    // per plant values the rest of the game reads, so it never has to know about mulches
    public static float SlowBonusFrom(Entity source) => source is Plant plant ? plant.mulchSlowBonus : 0f;
    public static float PrimerCooldownFactor(Entity source) =>
        source is Plant plant ? Mathf.Max(0f, plant.mulchPrimerCooldownMultiplier) : 1f;
    public static float RegenerationFactor(Entity source) =>
        source is Plant plant ? Mathf.Max(0f, 1f + plant.mulchRegenerationBonus) : 1f;

    // wisdom leaf: the extra share of exp a plant type earns this level, every wisdom leaf on a
    // living plant of that type added together (read at level end, Plant.CommitPendingExpToSave)
    public static float WisdomBonusFor(string plantName)
    {
        float total = 0f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant.data == null || plant.data.plantName != plantName) continue;
            foreach (MulchEffect effect in plant.mulches)
                if (effect is WisdomLeafMulchEffect wisdom) total += wisdom.Bonus;
        }
        return total;
    }

    // ── drops ───────────────────────────────────────────────────────────────

    // drop rate points to the bonus they give: X / (X + 100), never past double
    public static float DropRateBonus(float points) => points > 0f ? points / (points + 100f) : 0f;

    // every enemy insect counts under the wave it spawned in (Insect.Start)
    public void RegisterInsect(Insect insect)
    {
        if (insect == null || insect.team == Team.Friendly || insect.mulchCounted) return;
        int wave = GameManager.instance != null ? GameManager.instance.currentWave : 0;
        insect.mulchWave = wave;
        insect.mulchCounted = true;
        aliveByWave[wave] = (aliveByWave.TryGetValue(wave, out int n) ? n : 0) + 1;
    }

    // an enemy insect died (Insect.Kill, or turned by hypnosis): its own drop roll, then the wave count
    public void NotifyInsectDied(Insect insect, Entity source)
    {
        if (insect == null || !insect.mulchCounted) return;
        Plant killer = source as Plant;
        RollKillDrop(insect, killer);
        InsectGone(insect, killer, false, true);
    }

    // an enemy insect reached the end of the path. it never rolls a drop of its own, but if it was
    // its wave's last insect the guaranteed mulch pops out of it and hops back onto the map
    public void NotifyInsectLeaked(Insect insect)
    {
        if (insect == null || !insect.mulchCounted) return;
        InsectGone(insect, null, true, true);
    }

    // an insect destroyed without dying or leaking: it just stops counting. never spawns anything,
    // since this can run while a scene is unloading
    public void NotifyInsectDestroyed(Insect insect)
    {
        if (insect == null || !insect.mulchCounted) return;
        InsectGone(insect, null, false, false);
    }

    private void InsectGone(Insect insect, Plant killer, bool leaked, bool mayDrop)
    {
        insect.mulchCounted = false;
        int wave = insect.mulchWave;
        aliveByWave[wave] = Mathf.Max(0, (aliveByWave.TryGetValue(wave, out int n) ? n : 0) - 1);
        lastExit[wave] = (insect.transform.position, killer, leaked);
        if (mayDrop) TryGuaranteedDrop(wave);
    }

    // ProceduralLevel, right after a wave's spawning finishes. the last wave never guarantees one
    public void NotifyWaveSpawningDone(int wave, bool lastWave)
    {
        spawningDone.Add(wave);
        if (lastWave) { guaranteedHandled.Add(wave); return; }
        TryGuaranteedDrop(wave);
    }

    private void TryGuaranteedDrop(int wave)
    {
        if (!spawningDone.Contains(wave) || guaranteedHandled.Contains(wave)) return;
        if (aliveByWave.TryGetValue(wave, out int alive) && alive > 0) return;
        if (!lastExit.TryGetValue(wave, out var exit)) return;
        guaranteedHandled.Add(wave);

        int epicEvery = FertilizerManager.EpicMulchEveryWaves;
        bool epic = epicEvery > 0 && wave > 0 && wave % epicEvery == 0;
        MulchItem item = RollItem(exit.killer, epic);
        if (item != null) MulchDrop.Spawn(item, exit.position, exit.leaked);
    }

    private void RollKillDrop(Insect insect, Plant killer)
    {
        if (Registry == null) return;
        killsSinceDrop++;
        float chance = Registry.baseDropChance * (1f + DropRateBonus(killer != null ? killer.itemDropRate : 0f));
        bool pity = Registry.pityKills > 0 && killsSinceDrop >= Registry.pityKills;
        if (!pity && Random.value >= chance) return;
        killsSinceDrop = 0;
        MulchItem item = RollItem(killer, false);
        if (item != null) MulchDrop.Spawn(item, insect.transform.position, false);
    }

    // which mulch and at what rarity. only mulches the loadout can use (MulchData.CanDropFor:
    // their condition, like hinder needing a plant that slows), and countdown is left out with fewer
    // than 10 waves to go
    public MulchItem RollItem(Plant killer, bool forceEpic)
    {
        if (Registry == null || Registry.mulches == null) return null;

        MulchTier rarity = MulchTier.Epic;
        if (!forceEpic)
        {
            float rareBoost = 1f + DropRateBonus(killer != null ? killer.rareItemDropRate : 0f);
            float common = Mathf.Max(0f, Registry.rarityWeights.common);
            float rare = Mathf.Max(0f, Registry.rarityWeights.rare) * rareBoost;
            float epic = Mathf.Max(0f, Registry.rarityWeights.epic) * rareBoost;
            float roll = Random.Range(0f, common + rare + epic);
            rarity = roll < common ? MulchTier.Common : roll < common + rare ? MulchTier.Rare : MulchTier.Epic;
        }

        int wavesLeft = WavesLeft();
        List<PlantData> loadout = FertilizerManager.GetCurrentLoadout();
        List<MulchData> pool = new List<MulchData>();
        float total = 0f;
        foreach (MulchData data in Registry.mulches)
        {
            if (data == null || !data.CanDropFor(wavesLeft, loadout)) continue;
            pool.Add(data);
            total += Mathf.Max(0f, data.dropWeight);
        }
        if (total <= 0f) return null;

        float pick = Random.Range(0f, total);
        MulchData chosen = null;
        foreach (MulchData data in pool)
        {
            chosen = data;
            pick -= Mathf.Max(0f, data.dropWeight);
            if (pick <= 0f) break;
        }
        return chosen != null ? new MulchItem(chosen, rarity) : null;
    }

    public static int WavesLeft()
    {
        LevelConfig config = ProceduralLevel.CurrentConfig;
        int wave = GameManager.instance != null ? GameManager.instance.currentWave : 0;
        return config != null ? config.maxWaves - wave : int.MaxValue;
    }

    // clicking a ground drop: the slot it goes into (it's there at once, the bar just shows it once
    // the drop has flown in), or -1 (and the drop stays) when the inventory is full
    public int TryCollect(MulchItem item)
    {
        int slot = AddToInventorySlot(item);
        if (slot >= 0) TutorialManager.instance?.NotifyEvent(TutorialEvents.CollectMulch);
        return slot;
    }

    // the first time a drop appears in level 1, the mulch tutorial explains picking it up, equipping
    // and selling, with the game paused like the other level 1 tutorials (the drop's own clock is
    // frozen meanwhile)
    public static void OnDropSpawned()
    {
        if (ProceduralLevel.CurrentConfig == null || ProceduralLevel.CurrentConfig.levelNumber != 1) return;
        if (TutorialManager.instance != null && GameManager.instance != null
            && TutorialManager.instance.TryShow(TutorialIds.Level1FirstMulch, TutorialContent.Level1FirstMulch,
                TutorialSpeaker.AcornKnight, () => GameManager.instance.SetPause(false)))
            GameManager.instance.SetPause(true);
    }

    // ── dev menu ────────────────────────────────────────────────────────────

    public bool DebugGive(MulchData data, MulchTier rarity)
    {
        if (data == null) return false;
        MulchItem item = new MulchItem(data, rarity);
        if (AddToInventory(item)) return true;
        Vector3 at = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        at.z = 0f;
        MulchDrop.Spawn(item, at, false);
        return false;
    }
}
