using UnityEngine;
using TMPro;

// mirrors PlantRegistry, loaded from Resources/MulchRegistry.asset: every loot mulch that can drop,
// every fertilizer (for the dev menu), and every number of the loot system in one place to tune.
// numbers marked proposal in the tooltips weren't set by Tommy yet
[CreateAssetMenu(fileName = "MulchRegistry", menuName = "Scriptable Objects/Mulch Registry")]
public class MulchRegistry : ScriptableObject
{
    [Header("content")]
    [Tooltip("every loot mulch a drop can roll (wild mulch included)")]
    public MulchData[] mulches;
    [Tooltip("every fertilizer, for the dev menu (n). which ones a level offers is LevelConfig.fertilizers")]
    public FertilizerData[] fertilizers;
    [Tooltip("old icon for a mulch with none of its own, only while there's no paper bag below")]
    public Sprite placeholderIcon;
    [Tooltip("the paper bag mulch icon (Downloads\\gd_mulch_icons): every mulch's icon is this bag, the label over " +
        "it in the rarity colour, and the mulch's own symbol (MulchData.symbol) over that in a pale rarity colour")]
    public Sprite iconBag;
    public Sprite iconLabel;
    [Tooltip("the bag's tint, the same for every mulch")]
    public Color iconBagColor = new Color32(0xC4, 0x74, 0x14, 0xFF);
    [Tooltip("the symbol on the label for each kind of mulch (MulchData.symbol): 32 by 32, the 9 by 9 symbol drawn near white")]
    public Sprite symbolHeal;
    public Sprite symbolDefense;
    public Sprite symbolAttack;
    public Sprite symbolAffliction;
    public Sprite symbolSun;
    public Sprite symbolTime;

    public Sprite SymbolSprite(MulchSymbol symbol) => symbol switch
    {
        MulchSymbol.Heal => symbolHeal,
        MulchSymbol.Defense => symbolDefense,
        MulchSymbol.Attack => symbolAttack,
        MulchSymbol.Affliction => symbolAffliction,
        MulchSymbol.Sun => symbolSun,
        MulchSymbol.Time => symbolTime,
        _ => null,
    };
    [Tooltip("font of the mulch inventory, tooltips and the dev menu")]
    public TMP_FontAsset font;

    [Header("drops")]
    [Tooltip("chance an insect drops a mulch when it dies (Tommy: 2%); the killing plant's Mulch Drop Rate is added to it")]
    [Range(0f, 1f)] public float baseDropChance = 0.02f;
    [Tooltip("bad luck protection: after this many kills in a row without a drop, the next kill " +
        "always drops one (Tommy: 50). 0 turns it off")]
    public int pityKills = 50;
    [Tooltip("relative odds of each rarity on a drop, before the killing plant's Rare Item Drop Rate, " +
        "which multiplies Rare and Epic (proposal)")]
    public TierValue rarityWeights = new TierValue(70f, 25f, 5f);
    [Tooltip("Sun for selling a mulch from the inventory or the ground (right click it, then Sell); common x1, rare x2, epic x4")]
    public TierValue sellPrice = new TierValue(15f, 30f, 60f);

    [Header("limits")]
    public int inventorySize = 6;
    public int plantSlots = 6;

    [Header("ground drops")]
    [Tooltip("seconds a drop stays on the ground, real time: pausing freezes it, game speed doesn't change it")]
    public float groundSeconds = 25f;
    [Tooltip("seconds before it disappears that it starts flashing")]
    public float flashSeconds = 5f;
    [Tooltip("seconds for one fade out and back in when it starts flashing, and right before it disappears; it " +
        "speeds up from the first to the second, slowly at first and faster near the end")]
    public float flashCycleStart = 1f;
    public float flashCycleEnd = 0.12f;
    [Header("floating, once it has landed (visual only: the drop itself stays put)")]
    [Tooltip("world units the drop's picture floats above where it would rest")]
    public float floatHeight = 0.12f;
    [Tooltip("world units it bobs up and down around that height, and seconds for one bob")]
    public float bobHeight = 0.06f;
    public float bobSeconds = 2.2f;
    [Tooltip("seconds it takes to rise to its float after landing")]
    public float floatRiseSeconds = 0.35f;
    [Tooltip("size of a drop on the map, in world units (a tile is 1)")]
    public float dropSize = 0.6f;
    [Tooltip("how high the drop hops and for how long")]
    public float hopHeight = 0.55f;
    public float hopSeconds = 0.35f;
    [Tooltip("how far sideways the drop lands from where it popped out, a random amount between these")]
    public float sideOffsetMin = 0.35f;
    public float sideOffsetMax = 1.1f;
    [Tooltip("a drop from a leaked insect hops this far back toward the map")]
    public float leakHopDistance = 1.6f;

    [Header("drop look")]
    [Tooltip("the drop's own sprite: unlit (Sprite-Unlit-Default), so it shows at full brightness in the dark")]
    public Material spriteMaterial;
    [Tooltip("world units wide, the outline in the rarity's colour always drawn around a drop (Tommy, 2026-11-22: " +
        "like a selected plant's, 0.05, a little thinner). 0: no outline")]
    public float outlineWidth = 0.035f;
    [Tooltip("ground shadow, the same one insects use (Resources/Circle, lit, black at 41%)")]
    public Sprite shadowSprite;
    public Material shadowMaterial;
    public Color shadowColor = new Color(0f, 0f, 0f, 0.41f);
    [Tooltip("shadow size in world units, an ellipse under the drop")]
    public Vector2 shadowSize = new Vector2(0.5f, 0.2f);
    [Tooltip("glitter while it rests on the ground: unlit, so it shines in the dark too (MulchGlitter, a white glint)")]
    public Material glitterMaterial;
    public Color glitterColor = Color.white;
    [Tooltip("glints per second")]
    public float glitterRate = 7f;
    [Tooltip("seconds each glint lives, a random amount between x and y")]
    public Vector2 glitterLifetime = new Vector2(0.5f, 0.9f);
    [Tooltip("glint size in world units, a random amount between x and y (0.11 is the texture's own 7 pixels)")]
    public Vector2 glitterSize = new Vector2(0.09f, 0.14f);
    [Tooltip("how far around the drop the glints appear")]
    public float glitterRadius = 0.32f;

    [Header("pick up")]
    [Tooltip("seconds a clicked drop takes to fly into its inventory slot, real time")]
    public float flySeconds = 0.55f;

    [Header("mulch slots in the right panel")]
    [Tooltip("gap between slots. where the rows sit and how big the slots are comes from Canvas.prefab's " +
        "MulchBag and PlantMulches objects (MulchInventoryUI)")]
    public float slotSpacing = 6f;
}
