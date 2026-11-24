using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    // no ability of its own (Tommy, 2026-11-24): the 1.5x damage to path blockers moved to the Soldier Ant.
    // its description is in its data ("An average insect.")
}
