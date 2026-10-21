using UnityEngine;

// base class for a single mulch: a drafted, run-defining mechanic change, as opposed to
// fertilizers, which only ever add/multiply existing stats. one concrete subclass per mulch,
// same split as PlantData/Plant - this class holds the authored data (name, tier, roll weight,
// eligibility); subclasses add whatever tunables their paired MulchEffect needs
public abstract class MulchData : ScriptableObject
{
    [Tooltip("rich-text formatted, e.g. '<b><color=orange>Rekindling</color></b>' - every mulch " +
        "name is bolded and colored to its elementalType (PlantData.ElementalColor), even when " +
        "that element is just Neutral/white")]
    public string mulchName;
    [TextArea] public string description;
    public Sprite icon;
    [Tooltip("unused by MulchCard - the card background is tier-colored instead (see " +
        "MulchTierColors), the same way across every mulch regardless of element. kept in case " +
        "something else wants a per-mulch tint later")]
    public Color color = Color.white;
    [Tooltip("every mulch has one, even if it's thematically Neutral - drives mulchName's color")]
    public ElementalType elementalType;
    public MulchTier tier;

    [Tooltip("relative weight when rolling a draft of this tier. higher rolls more often. " +
        "mulches scoped to one plant or family should usually sit lower than board-wide ones, " +
        "since they do more but apply to less")]
    public float rollWeight = 1f;

    [Tooltip("optional - set this when the mulch's effect is themed around (or scoped to) one " +
        "specific plant, e.g. a Cactus-only skill buff. purely informational/targeting data for " +
        "the paired MulchEffect to read; it does not by itself gate eligibility - use condition " +
        "for that")]
    public PlantData targetPlant;

    public MulchCondition condition;

    // builds the runtime behavior this mulch grants once picked. called once per pick
    public abstract MulchEffect CreateEffect();
}
