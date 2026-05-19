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

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        scoutAntCount++;
    }

    void OnDestroy()
    {
        scoutAntCount--;
    }
}
