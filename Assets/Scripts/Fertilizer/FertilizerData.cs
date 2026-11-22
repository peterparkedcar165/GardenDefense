using UnityEngine;
using System.Collections.Generic;

// base class for a fertilizer: picked once before the level, from that level's own list
// (LevelConfig.fertilizers), it changes how the whole level works until the level ends. one
// concrete subclass per fertilizer, paired with its FertilizerEffect, the same split as PlantData
// and Plant. fertilizers have no rarity (Tommy, 2026-11-21). mulches are the other system: loot that drops during the level and goes on a single
// plant (see MulchData)
public abstract class FertilizerData : ScriptableObject
{
    [Tooltip("rich text, e.g. '<b><color=orange>Rekindling</color></b>': every fertilizer name is " +
        "bold and coloured to its elementalType (PlantData.ElementalColor), even when that's just Neutral")]
    public string fertilizerName;
    [TextArea] public string description;
    public Sprite icon;
    [Tooltip("drives the colour of fertilizerName")]
    public ElementalType elementalType;

    [Tooltip("loadout requirement: a fertilizer whose condition fails is left off the pick screen " +
        "(e.g. melt needs fire, water and ice plants)")]
    public LoadoutCondition condition;

    // builds the runtime behaviour this fertilizer grants. called once, when it's picked
    public abstract FertilizerEffect CreateEffect();

    // the card text. every subclass builds it from its own fields, so a tuned number never drifts
    // away from what the card says; description is only the fallback
    public virtual string GetDescription() => description;

    // eligibility the condition above can't express
    public virtual bool MeetsExtraRequirements(List<PlantData> loadout) => true;

    [Tooltip("off for now: never offered before a level, even when a level lists it or a short list " +
        "is being filled (greenhouse, Tommy, 2026-11-22). the dev menu still has it")]
    public bool disabled;

    public bool IsOffered(List<PlantData> loadout) =>
        !disabled && (condition == null || condition.IsEligible(loadout, 0)) && MeetsExtraRequirements(loadout);

    // card text pieces, the same colours as the mulch tooltips: numbers green and bold, red for a
    // downside, and Sun yellow
    protected static string Green(string text) => $"<color=green><b>{text}</b></color>";
    protected static string Red(string text) => $"<color=red><b>{text}</b></color>";
    protected static string Pct(float value) => $"{value * 100f:0.#}%";
    protected const string SunLabel = "<color=yellow><b>Sun</b></color>";
}
