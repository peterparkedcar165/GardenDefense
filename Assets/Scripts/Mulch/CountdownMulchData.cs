using UnityEngine;

[CreateAssetMenu(fileName = "CountdownMulch", menuName = "Scriptable Objects/Mulch/Countdown")]
public class CountdownMulchData : MulchData
{
    [Tooltip("whole waves its plant must survive with it equipped. it never drops with fewer waves left in the level")]
    public int wavesNeeded = 10;
    [Tooltip("free levels its plant then gets on every path, up to the level's max")]
    public TierValue levels = new TierValue(1f, 2f, 3f);

    public override MulchEffect CreateEffect(MulchItem item) => new CountdownMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        int n = levels.GetInt(r);
        return $"Once this plant survives {Green(wavesNeeded.ToString())} waves with this equipped, it gains {Green("+" + n)} {(n == 1 ? "level" : "levels")} " +
            $"on all 3 upgrade paths for free, up to the level's max upgrade level.";
    }

    // can't drop when the level ends before it could pay out
    public override bool CanDrop(int wavesLeft) => wavesLeft >= wavesNeeded;
}
