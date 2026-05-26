using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoutAnt : Ant
{
    private float scanTimer = 0f;
    private const float scanInterval = 0.5f;
    private const float buffDuration = 0.5f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            EncourageInsects();
        }
    }

    public override string GetName() => "<color=#8B4513>Scout Ant</color>";

    private void EncourageInsects()
    {
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect is ScoutAnt) continue;
            ScoutsEncouragementEffect existing = insect.GetEffect<ScoutsEncouragementEffect>();
            if (existing != null)
                existing.duration = buffDuration;
            else
                insect.ApplyEffect(new ScoutsEncouragementEffect(insect, buffDuration, 1, this));
        }
    }
}
