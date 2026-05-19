using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoutAnt : Ant
{
    public static int scoutAntCount = 0;
    private bool _decremented = false;

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

    private void Decrement()
    {
        if (_decremented) return;
        _decremented = true;
        scoutAntCount--;
    }

    public override void Kill(Entity source)
    {
        Decrement();
        base.Kill(source);
    }

    public override void Kill()
    {
        Decrement();
        base.Kill();
    }

    void OnDestroy()
    {
        Decrement();
    }
}
