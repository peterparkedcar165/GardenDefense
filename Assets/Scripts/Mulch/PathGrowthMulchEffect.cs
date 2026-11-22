// adds its levels to one path's effective level on its plant (Plant.AddPathLevelBonus) and takes
// them back off when it stops. a merge takes the old levels off and puts the new ones on
public class PathGrowthMulchEffect : MulchEffect
{
    private readonly PathGrowthMulchData typedData;
    private int applied;

    public PathGrowthMulchEffect(PathGrowthMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null || applied != 0) return;
        applied = VI(typedData.levels);
        Owner.AddPathLevelBonus((int)typedData.path, applied);
    }

    public override void Deactivate()
    {
        if (Owner != null && applied != 0) Owner.AddPathLevelBonus((int)typedData.path, -applied);
        applied = 0;
    }

    public override string GetStatus()
    {
        if (Owner == null || applied == 0) return null;
        int effective = typedData.path == PathGrowthMulchData.GrowthPath.Attack ? Owner.effectivePath1Level
            : typedData.path == PathGrowthMulchData.GrowthPath.Passive ? Owner.effectivePath2Level
            : Owner.effectivePath3Level;
        return $"{typedData.path} path level: {effective} ({effective - applied} without it)";
    }
}
