using UnityEngine;

public class FruitFly : FlyingInsect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }
}
