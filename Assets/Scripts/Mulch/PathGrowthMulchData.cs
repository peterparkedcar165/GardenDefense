using UnityEngine;

// attack growth, passive growth and skill growth: one class, three assets, each naming its path.
// the levels go on the path's effective level, not its point level (Tommy, 2026-11-21)
[CreateAssetMenu(fileName = "PathGrowthMulch", menuName = "Scriptable Objects/Mulch/Path Growth")]
public class PathGrowthMulchData : MulchData
{
    public enum GrowthPath { Attack = 1, Passive = 2, Skill = 3 }

    [Tooltip("the path whose effective level goes up")]
    public GrowthPath path = GrowthPath.Attack;
    [Tooltip("levels added to that path's effective level")]
    public TierValue levels = new TierValue(1f, 2f, 3f);

    public override MulchEffect CreateEffect(MulchItem item) => new PathGrowthMulchEffect(this, item);

    public override string GetDescription(MulchItem item) =>
        $"Increase the {path} path's effective level by {Green(levels.GetInt(item.rarity).ToString())}.";
}
