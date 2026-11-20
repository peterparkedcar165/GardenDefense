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
    [Tooltip("icon for a mulch with none of its own")]
    public Sprite placeholderIcon;
    [Tooltip("font of the mulch inventory, tooltips and the dev menu")]
    public TMP_FontAsset font;

    [Header("drops")]
    [Tooltip("chance an insect drops a mulch when it dies, before the killing plant's Item Drop Rate (proposal)")]
    [Range(0f, 1f)] public float baseDropChance = 0.04f;
    [Tooltip("bad luck protection: after this many kills in a row without a drop, the next kill " +
        "always drops one (proposal). 0 turns it off")]
    public int pityKills = 30;
    [Tooltip("relative odds of each rarity on a drop, before the killing plant's Rare Item Drop Rate, " +
        "which multiplies Rare and Epic (proposal)")]
    public TierValue rarityWeights = new TierValue(70f, 25f, 5f);
    [Tooltip("Sun for right clicking a mulch in the inventory to sell it")]
    public TierValue sellPrice = new TierValue(50f, 100f, 175f);

    [Header("limits")]
    public int inventorySize = 8;
    public int plantSlots = 6;

    [Header("ground drops")]
    [Tooltip("seconds a drop stays on the ground, real time: pausing freezes it, game speed doesn't change it")]
    public float groundSeconds = 25f;
    [Tooltip("seconds before it disappears that it starts flashing")]
    public float flashSeconds = 5f;
    [Tooltip("size of a drop on the map, in world units (a tile is 1)")]
    public float dropSize = 0.6f;
    [Tooltip("how high the drop hops and for how long")]
    public float hopHeight = 0.9f;
    public float hopSeconds = 0.55f;
    [Tooltip("how far sideways the drop lands from where it popped out, a random amount between these")]
    public float sideOffsetMin = 0.35f;
    public float sideOffsetMax = 1.1f;
    [Tooltip("a drop from a leaked insect hops this far back toward the map")]
    public float leakHopDistance = 1.6f;

    [Header("drop look")]
    [Tooltip("the drop's own sprite: unlit (Sprite-Unlit-Default), so it shows at full brightness in the dark")]
    public Material spriteMaterial;
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
    [Tooltip("on dark levels a small light under the drop, so it also lights the ground around it (off in daylight and while sunny)")]
    public float darkGlowRadius = 0.9f;
    public float darkGlowIntensity = 0.7f;

    [Header("pick up")]
    [Tooltip("seconds a clicked drop takes to fly into its inventory slot, real time")]
    public float flySeconds = 0.55f;

    [Header("mulch slots in the right panel")]
    [Tooltip("gap between slots. where the rows sit and how big the slots are comes from Canvas.prefab's " +
        "MulchBag and PlantMulches objects (MulchInventoryUI)")]
    public float slotSpacing = 6f;
}
