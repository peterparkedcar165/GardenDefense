public class Mosquito : FlyingInsect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override string GetDescription() =>
        $"Aggressive insect. Heals for <color=green><b>{(data != null ? data.baseLifesteal : baseLifesteal) * 100f:F0}%</b></color> of the damage dealt to plants." +
        FlyingLine() + AggressivityLine();
}
