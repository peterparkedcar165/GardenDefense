using UnityEngine;

public class SunMarkEffect : StatusEffect
{
    public SunMarkEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.neutral;
    }

    public override void OnTargetDied()
    {
        if (target is Insect insect)
        {
            int bonus = Mathf.CeilToInt(insect.sunDrop * 0.27f);
            GameManager.instance?.AddSun(bonus);
            SunIndicator.SpawnBonus(target.transform.position + new Vector3(0.25f, 0.5f, 0f), bonus);
        }
    }

    public override string GetName() => "<color=#FFD700><b>Sun Mark</b></color>";
    public override string GetDescription() => "Yields <color=yellow><b>+27%</b></color> bonus <color=yellow>Sun</color> on death.";
}
