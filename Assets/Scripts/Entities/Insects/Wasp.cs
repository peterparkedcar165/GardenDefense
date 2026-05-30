using UnityEngine;

public class Wasp : FlyingInsect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }
}
