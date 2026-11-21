using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SolarFlareMulch", menuName = "Scriptable Objects/Mulch/Solar Flare")]
public class SolarFlareMulchData : MulchData
{
    [Tooltip("damage dealt to each insect within the plant's attack range, as a share of the sun made, e.g. 2 = 200%")]
    public TierValue damagePercentOfSun = TierValue.Scaled(2f);
    public DamageType damageType = DamageType.Magic;
    [Tooltip("on: the flare is in the plant's own element. off: it uses fixedElement instead")]
    public bool useSourceElement = true;
    public ElementalType fixedElement = ElementalType.Neutral;
    [Tooltip("on: the flare can put its element's primer on insects, like any elemental hit")]
    public bool appliesPrimer = true;
    [Tooltip("on: sun paid for kills (glowshroom) also sets off a flare. off: only the plant's own production does")]
    public bool triggeredByKillSun = false;

    // only drops when something makes sun: a sun producer in the loadout, or sun sap
    public override bool IsUseful(List<PlantData> loadout) => HasSunMaker(loadout);

    public override MulchEffect CreateEffect(MulchItem item) => new SolarFlareMulchEffect(this, item);

    public override string GetDescription(MulchItem item) =>
        $"Whenever the plant produces {SunLabel}, insects within its {Kw("Attack Range", "green")} take damage equal to " +
        $"{Green(Pct(damagePercentOfSun.Get(item.rarity)))} of the {SunLabel} generated.";
}
