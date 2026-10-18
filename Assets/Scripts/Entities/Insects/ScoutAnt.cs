using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoutAnt : Ant
{
    [SerializeField] private float buffRadius = 3f;
    [SerializeField] private float speedBonus = 0.4f;

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

    public override string GetDescription() =>
        $"Fast insect that grants <color=green><b>Scout's Encouragement</b></color> to other insects within a <color=green><b>{buffRadius:F0}</b></color>-radius, " +
        $"increasing Movement Speed by <color=green><b>+{speedBonus:F1}</b></color>." + AggressivityLine();

    private void EncourageInsects()
    {
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect is ScoutAnt) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) > buffRadius) continue;

            ScoutsEncouragementEffect existing = insect.GetEffect<ScoutsEncouragementEffect>();
            if (existing != null)
                existing.duration = buffDuration;
            else
                insect.ApplyEffect(new ScoutsEncouragementEffect(insect, buffDuration, 1, this, speedBonus));
        }
    }
}
