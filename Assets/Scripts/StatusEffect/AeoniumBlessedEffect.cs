using UnityEngine;

public class AeoniumBlessedEffect : StatusEffect
{
    private readonly int _bonusSun;

    public AeoniumBlessedEffect(Entity target, float duration, int level, Entity source, int bonusSun)
        : base(target, duration, level, source)
    {
        _bonusSun = bonusSun;
        effectType = Type.neutral;
    }

    public override void OnTargetDied()
    {
        GameManager.instance.AddSun(_bonusSun);
        SunIndicator.SpawnBonus(target.transform.position + new Vector3(0.25f, 0.5f, 0f), Mathf.RoundToInt(_bonusSun));
    }

    public override string GetName() => "<color=yellow>Mark of the Sun</color>";
    public override string GetDescription() => $"Yields <color=yellow><b>+{_bonusSun}</b></color> bonus <color=yellow>Sun</color> on death.";
}
