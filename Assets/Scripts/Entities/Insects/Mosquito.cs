public class Mosquito : FlyingInsect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
        targetingRange = 2.5f;
    }
}
