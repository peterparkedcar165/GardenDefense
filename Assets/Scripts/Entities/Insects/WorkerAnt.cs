using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    // no ability of its own (Tommy, 2026-11-24): the 1.5x damage to path blockers moved to the Soldier Ant
    public override string GetDescription() =>
        "Average insect." + AggressivityLine();
}
