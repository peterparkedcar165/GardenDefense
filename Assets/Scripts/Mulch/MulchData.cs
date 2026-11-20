using UnityEngine;

// base class for a loot mulch: it drops from insects during a level, the player picks it up into
// the mulch inventory and drags it onto one plant, which then has its effect until the level ends
// or the plant dies. every mulch exists at three rarities (MulchTier) with its numbers in
// TierValue fields; a picked up copy is a MulchItem, and an equipped one runs a MulchEffect on its
// plant. one concrete subclass per mulch, paired with its MulchEffect. fertilizers are the other
// system: picked before the level, they change the whole level (see FertilizerData)
public abstract class MulchData : ScriptableObject
{
    [Tooltip("rich text, e.g. '<b><color=green>Thorns</color></b>': every mulch name is bold and " +
        "coloured to its elementalType (PlantData.ElementalColor), even when that's just Neutral")]
    public string mulchName;
    [TextArea] public string description;
    [Tooltip("drawn in grayscale, the game tints it by rarity. empty uses the registry's placeholder")]
    public Sprite icon;
    [Tooltip("drives the colour of mulchName")]
    public ElementalType elementalType;

    [Tooltip("relative odds of this mulch when a drop rolls which mulch it is. 0 never drops")]
    public float dropWeight = 1f;

    // builds what this mulch does on a plant. called once per equip
    public abstract MulchEffect CreateEffect(MulchItem item);

    // the tooltip text at the item's rarity. every subclass builds it from its own TierValue fields,
    // so a tuned number never drifts away from what the tooltip says
    public virtual string GetDescription(MulchItem item) => description;

    // whether a drop may roll this mulch, given how many waves the level still has (countdown needs 10)
    public virtual bool CanDrop(int wavesLeft) => true;

    // runs once when a new copy is made (dropped, merged, given by the dev menu). wild mulch rolls
    // its stats here
    public virtual void OnItemCreated(MulchItem item) {}

    // two copies that merge into one of the next rarity: the same mulch at the same rarity, below
    // Epic. wild mulch merges any two of the same rarity
    public virtual bool CanMerge(MulchItem a, MulchItem b) =>
        a != null && b != null && a.data == this && b.data == this && a.rarity == b.rarity && a.rarity < MulchTier.Epic;

    // number helpers for descriptions, at an item's rarity
    protected static string Green(string text) => $"<b><color=green>{text}</color></b>";
    protected static string Red(string text) => $"<b><color=red>{text}</color></b>";
    protected static string Pct(float value) => $"{value * 100f:0.#}%";
    protected const string SunLabel = "<b><color=#FFD700>Sun</color></b>";

    // a rarity's name in its colour
    public static string RarityLabel(MulchTier rarity) =>
        $"<b><color=#{ColorUtility.ToHtmlStringRGB(MulchTierColors.Get(rarity))}>{rarity}</color></b>";
}
