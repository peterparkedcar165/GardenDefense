using UnityEngine;
using System.Collections.Generic;

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
        "mulches scoped to one plant should usually sit lower than board-wide ones, " +
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

    // text shown on the mulch's card - override per subclass to interpolate its own tunables
    // instead of hardcoding numbers that can drift out of sync with the actual values. the base
    // description field still exists as a fallback/author-time placeholder for anything not yet
    // overridden
    public virtual string GetDescription() => description;

    // eligibility the condition above can't express, checked on top of it: e.g. a plant that
    // generates sun is in the loadout (drought, solar flare), or enough waves are left (countdown)
    public virtual bool MeetsExtraRequirements(List<PlantData> loadout, int currentWave) => true;

    protected static bool LoadoutHasSunProducer(List<PlantData> loadout)
    {
        if (loadout == null) return false;
        foreach (PlantData plant in loadout)
            if (plant != null && plant.generatesSun) return true;
        return false;
    }

    // seed swap and seed sell: picking the card first asks which owned mulch to use it on (see
    // MulchSelectionUI.ShowTargetPicker), then MulchManager.CommitChosenOnTarget spends it. a
    // tool's CreateEffect returns null, it never becomes an owned mulch
    public virtual bool NeedsOwnedMulchTarget => false;
    // a tool that may also take mulches that already paid out (seed sell, MulchEffect.CanBeSold).
    // seed swap may not: a fresh draft for a mulch that already gave everything would pay twice
    public virtual bool TakesPaidOutMulches => false;
    // runs after the target was taken away
    public virtual void UseOnTarget(MulchData target) {}
    // the picker's prompt, and the note on each row saying what using the tool on it gives
    public virtual string TargetPrompt => "";
    public virtual string DescribeTargetUse(MulchData target) => "";
    // seed swap: its replacement draft opens straight after it's used
    public virtual bool OpensDraftAfterUse => false;

    // a tier's name in its card color
    public static string TierLabel(MulchTier tier) =>
        $"<b><color=#{ColorUtility.ToHtmlStringRGB(MulchTierColors.Get(tier))}>{tier}</color></b>";

    // the target plant's name in its element's color, for plant mulch descriptions
    protected string TargetPlantLabel(string fallback)
    {
        string name = targetPlant != null && !string.IsNullOrEmpty(targetPlant.displayName) ? targetPlant.displayName : fallback;
        ElementalType element = targetPlant != null ? targetPlant.elementalType : ElementalType.Neutral;
        return $"<b><color={PlantData.ElementalColor(element)}>{name}</color></b>";
    }
}
