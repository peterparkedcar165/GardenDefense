using UnityEngine;
using System.Collections.Generic;

// base class for a loot mulch: it drops from insects during a level, the player picks it up into
// the mulch inventory and drags it onto one plant, which then has its effect until the level ends
// or the plant dies. every mulch exists at three rarities (MulchTier) with its numbers in
// TierValue fields; a picked up copy is a MulchItem, and an equipped one runs a MulchEffect on its
// plant. one concrete subclass per mulch, paired with its MulchEffect. fertilizers are the other
// system: picked before the level, they change the whole level (see FertilizerData)

// the kind of effect a mulch has, shown as the symbol on its paper bag icon's label (Tommy,
// 2026-11-20: symbols are categories, not one per mulch). the sprite for each is on MulchRegistry
public enum MulchSymbol
{
    None,
    Heal,        // the plus: healing and regeneration
    Defense,     // the shield: armor, shields and hitting back
    Attack,      // the lightning: attack speed, range and area
    Affliction,  // the spiral: slows, primers, debuffs and damage over time
    Sun,         // the sun: making sun
    Time,        // the hourglass: payoffs that come later in the level
}

public abstract class MulchData : ScriptableObject
{
    [Tooltip("rich text, e.g. '<b><color=green>Thorns</color></b>': every mulch name is bold and " +
        "coloured to its elementalType (PlantData.ElementalColor), even when that's just Neutral")]
    public string mulchName;
    [TextArea] public string description;
    [Tooltip("which kind of mulch it is, drawn as the symbol on its icon's label (the sprites are on MulchRegistry). " +
        "None: the bag and label only")]
    public MulchSymbol symbol;
    [Tooltip("the old single sprite icon, tinted by rarity. only shown while the registry has no paper bag (iconBag)")]
    public Sprite icon;
    [Tooltip("drives the colour of mulchName")]
    public ElementalType elementalType;

    [Tooltip("relative odds of this mulch when a drop rolls which mulch it is. 0 never drops")]
    public float dropWeight = 1f;

    [Tooltip("which loadouts this mulch is any use to: it never drops unless the plants brought into " +
        "the level pass (e.g. hinder needs a plant that slows). leave empty for every loadout")]
    public LoadoutCondition condition;

    // builds what this mulch does on a plant. called once per equip
    public abstract MulchEffect CreateEffect(MulchItem item);

    // the tooltip text at the item's rarity. every subclass builds it from its own TierValue fields,
    // so a tuned number never drifts away from what the tooltip says
    public virtual string GetDescription(MulchItem item) => description;

    // the same text for a copy equipped on a plant, for mulches whose numbers depend on that plant
    // (ironclad riposte's shield). effect is null for a copy in the bag or on the ground
    public virtual string GetDescription(MulchItem item, MulchEffect effect) => GetDescription(item);

    // whether a drop may roll this mulch, given how many waves the level still has (none needs it now,
    // kept for mulches that pay out after a set number of waves)
    public virtual bool CanDrop(int wavesLeft) => true;

    // whether a plant in this loadout could use it at all, on top of the condition above, for what
    // the condition can't say (a sun producer, a ranged plant)
    public virtual bool IsUseful(List<PlantData> loadout) => true;

    // the full drop check: waves left, the condition and IsUseful
    public bool CanDropFor(int wavesLeft, List<PlantData> loadout) =>
        CanDrop(wavesLeft) && (condition == null || condition.IsEligible(loadout, 0)) && IsUseful(loadout);

    // a sun producer in the loadout, or a plant already given sun sap (which makes any plant one)
    protected static bool HasSunMaker(List<PlantData> loadout)
    {
        if (loadout != null)
            foreach (PlantData plant in loadout)
                if (plant != null && plant.generatesSun) return true;
        foreach (Plant plant in Plant.allPlants)
            if (plant != null && plant.IsAlive)
                foreach (MulchEffect effect in plant.mulches)
                    if (effect is SunSapMulchEffect) return true;
        if (MulchManager.instance != null)
            foreach (MulchItem item in MulchManager.instance.Inventory)
                if (item != null && item.data is SunSapMulchData) return true;
        return false;
    }

    // runs once when a new copy is made (dropped, merged, given by the dev menu). wild mulch rolls
    // its stats here
    public virtual void OnItemCreated(MulchItem item) {}

    // two copies that merge into one of the next rarity: the same mulch at the same rarity, below
    // Epic. wild mulch merges any two of the same rarity
    public virtual bool CanMerge(MulchItem a, MulchItem b) =>
        a != null && b != null && a.data == this && b.data == this && a.rarity == b.rarity && a.rarity < MulchTier.Epic;

    // the copy two mergeable copies become: a fresh one at the next rarity. wild mulch keeps both
    // copies' stats instead
    public virtual MulchItem Merge(MulchItem a, MulchItem b) => new MulchItem(this, NextRarity(a.rarity));

    protected static MulchTier NextRarity(MulchTier rarity) => rarity < MulchTier.Epic ? rarity + 1 : rarity;

    // number helpers for descriptions, at an item's rarity. numbers are green and bold, red for a
    // downside (Tommy's tooltip rules, 2026-11-21)
    protected static string Green(string text) => $"<b><color=green>{text}</color></b>";
    protected static string Red(string text) => $"<b><color=red>{text}</color></b>";
    protected static string Pct(float value) => $"{value * 100f:0.#}%";
    // a keyword in its colour, bold
    protected static string Kw(string text, string color) => $"<color={color}><b>{text}</b></color>";
    protected const string SunLabel = "<color=yellow><b>Sun</b></color>";

    // the tooltip keyword colours (Tommy, 2026-11-21)
    protected const string HealthColor = "red";
    protected const string ArmorColor = "#00CED1";
    protected const string ShieldColor = "grey";
    protected const string CritColor = "#FFD700";
    protected const string CounterColor = "#CD7F32";
    protected const string LifestealColor = "#FF6B81";
    protected const string PhysicalColor = "#A0522D";
    protected const string IceColor = "#00FFFF";
    protected const string PoisonColor = "purple";
    // no colour existed for these yet (proposal)
    protected const string MeleeColor = "#E9967A";
    protected const string OnHitColor = "#F0E68C";
    protected const string TrueColor = "#D3D3D3";

    // a rarity's name in its colour
    public static string RarityLabel(MulchTier rarity) =>
        $"<b><color=#{ColorUtility.ToHtmlStringRGB(MulchTierColors.Get(rarity))}>{rarity}</color></b>";
}
