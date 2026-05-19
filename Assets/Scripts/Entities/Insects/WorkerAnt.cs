using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override float eatMultiplier => 1.5f;
}
