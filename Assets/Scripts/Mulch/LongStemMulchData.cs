using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LongStemMulch", menuName = "Scriptable Objects/Mulch/Long Stem")]
public class LongStemMulchData : MulchData
{
    [Tooltip("attack range added to this plant, as a share of its base range, e.g. 0.25 = +25% (melee plants can't reach past their own range)")]
    public TierValue range = new TierValue(0.25f, 0.5f, 0.75f);

    // melee plants can't reach past their own range, so it needs a ranged plant in the loadout
    public override bool IsUseful(List<PlantData> loadout)
    {
        if (loadout != null)
            foreach (PlantData plant in loadout)
                if (plant != null && !FertilizerStatRules.IsMeleePlant(plant)) return true;
        return false;
    }

    public override MulchEffect CreateEffect(MulchItem item) => new LongStemMulchEffect(this, item);

    public override string GetDescription(MulchItem item) =>
        $"Increase {Kw("Attack Range", "green")} by {Green(Pct(range.Get(item.rarity)))}.";
}
