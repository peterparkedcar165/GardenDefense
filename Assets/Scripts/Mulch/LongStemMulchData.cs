using UnityEngine;

[CreateAssetMenu(fileName = "LongStemMulch", menuName = "Scriptable Objects/Mulch/Long Stem")]
public class LongStemMulchData : MulchData
{
    [Tooltip("flat attack range added to this plant (melee plants can't reach past their own range)")]
    public TierValue range = TierValue.Scaled(0.5f);

    public override MulchEffect CreateEffect(MulchItem item) => new LongStemMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"{Green("+" + range.Get(r).ToString("0.##"))} <b><color=green>Attack Range</color></b>. Melee plants can't use it.";
    }
}
