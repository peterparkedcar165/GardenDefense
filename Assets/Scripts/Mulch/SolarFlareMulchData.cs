using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SolarFlareMulchData", menuName = "Scriptable Objects/Mulch/Solar Flare")]
public class SolarFlareMulchData : MulchData
{
    [Tooltip("damage dealt to each insect in range, as a share of the sun generated, e.g. 4 = 400%")]
    public float damagePercentOfSun = 4f;
    [Tooltip("radius around the sun plant, in tiles (a tile is 1 unit)")]
    public float radiusTiles = 2f;
    public DamageType damageType = DamageType.Magic;
    [Tooltip("on: the flare is in the sun plant's own element. off: it uses fixedElement instead")]
    public bool useSourceElement = true;
    public ElementalType fixedElement = ElementalType.Neutral;
    [Tooltip("on: the flare can put its element's primer on insects, like any elemental hit. " +
        "off: it never primes an insect or sets off a reaction")]
    public bool appliesPrimer = true;
    [Tooltip("on: sun paid for kills (glowshroom) also sets off a flare. off: only a plant's own production does")]
    public bool triggeredByKillSun = false;

    public override MulchEffect CreateEffect() => new SolarFlareMulchEffect(this);

    public override bool MeetsExtraRequirements(List<PlantData> loadout, int currentWave) => LoadoutHasSunProducer(loadout);

    public override string GetDescription()
    {
        const string sun = "<b><color=#FFD700>Sun</color></b>";
        string element = useSourceElement
            ? "in that plant's own element"
            : $"as <b><color={PlantData.ElementalColor(fixedElement)}>{fixedElement}</color></b>";
        return $"Whenever a plant generates {sun}, it deals {PlantData.DamageTypeLabel(damageType)} equal to " +
            $"<b><color=green>{damagePercentOfSun * 100f:F0}%</color></b> of the {sun} generated to every insect within " +
            $"<b><color=green>{radiusTiles:0.#}</color></b> tiles, {element}.";
    }
}
