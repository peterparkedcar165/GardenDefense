using UnityEngine;

public class Wasp : FlyingInsect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
        targetingRange = 1.5f;
    }
}
