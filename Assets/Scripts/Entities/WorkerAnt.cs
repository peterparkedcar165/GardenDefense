using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake() {
        maxHealth = 20f;
        sunDrop = 25;
        base.Awake();
    }
}
