using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoutAnt : Ant
{
    public static int aliveCount = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        aliveCount = 0;
        SceneManager.sceneLoaded += (scene, mode) => aliveCount = 0;
    }

    protected override void Awake() {
        baseAttackDamage = 20f;
        baseMaxHealth = 200f;
        sunDrop = 8;
        base.Awake();
        baseMovementSpeed = 1.8f;
        aliveCount++;
    }

    void OnDestroy()
    {
        aliveCount--;
    }

    public override string GetName() => "<b><color=#8B4513>Scout Ant</color></b>";

    public override string GetDescription() => $"The {GetName()} is quick and evasive, hard to pin down.";

    public override string GetPassiveDescription() => "While alive, all non-scout ants gain 15% movement speed.";
}
