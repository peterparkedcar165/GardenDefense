using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LongStemMulch", menuName = "Scriptable Objects/Mulch/Long Stem")]
public class LongStemMulchData : MulchData
{
    [Tooltip("flat attack range added to this plant (melee plants can't reach past their own range)")]
    public TierValue range = TierValue.Scaled(0.5f);

    // melee plants can't reach past their own range, so it needs a ranged plant in the loadout
    public override bool IsUseful(List<PlantData> loadout)
    {
        if (loadout != null)
            foreach (PlantData plant in loadout)
                if (plant != null && !FertilizerStatRules.IsMeleePlant(plant)) return true;
        return false;
    }

    public override MulchEffect CreateEffect(MulchItem item) => new LongStemMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"{Green("+" + range.Get(r).ToString("0.##"))} <b><color=green>Attack Range</color></b>. Melee plants can't use it.";
    }
}
