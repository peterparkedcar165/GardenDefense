using UnityEngine;

public class Snail : Insect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }
}
