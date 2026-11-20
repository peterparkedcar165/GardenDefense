using UnityEngine;

[CreateAssetMenu(fileName = "CompostTeaMulch", menuName = "Scriptable Objects/Mulch/Compost Tea")]
public class CompostTeaMulchData : MulchData
{
    [Tooltip("share of this plant's max health it heals for each kill it helped with, e.g. 0.02 = 2%")]
    public TierValue healPercentPerKill = TierValue.Scaled(0.02f);

    public override MulchEffect CreateEffect(MulchItem item) => new CompostTeaMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Whenever an insect this plant helped kill dies, this plant heals {Green(Pct(healPercentPerKill.Get(r)))} of its <b><color=green>Max Health</color></b>.";
    }
}
