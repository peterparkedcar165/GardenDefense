using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override float eatMultiplier => 1.5f;

    public override string GetDescription() =>
        $"Average insect that deals <b>{eatMultiplier:F1}x</b> damage to path blockers." + AggressivityLine();
}
