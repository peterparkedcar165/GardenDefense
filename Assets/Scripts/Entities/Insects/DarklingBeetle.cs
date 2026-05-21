using UnityEngine;

public class DarklingBeetle : Insect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity   = Aggressivity.Medium;
        targetingRange = 2f;
    }
}
