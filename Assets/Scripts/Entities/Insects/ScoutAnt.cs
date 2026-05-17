using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoutAnt : Ant
{
    public static int scoutAntCount = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        scoutAntCount = 0;
        SceneManager.sceneLoaded += (scene, mode) => scoutAntCount = 0;
    }

    protected override void Awake() {
        baseAttackDamage = 15f;
        baseMaxHealth = 200f;
        sunDrop = 6;
        base.Awake();
        baseMovementSpeed = 1.8f;
        scoutAntCount++;
    }

    void OnDestroy()
    {
        scoutAntCount--;
    }

    public override string GetName() => "<b><color=#8B4513>Scout Ant</color></b>";

    public override string GetDescription() => $"The {GetName()} is quick and evasive, hard to pin down.";

    public override string GetPassiveDescription() => "While alive, all non-Scout Ants gain 15% Movement Speed.";
}
