using UnityEngine;

public class SunMarkEffect : StatusEffect
{
    public SunMarkEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Fire;
        // multiple Sunflowers can mark the same insect; each mark pays out its own bonus sun on death
        sourceStackable = true;
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

    public override string GetName() => "<color=orange><b>Sun Mark</b></color>";
    public override string GetDescription() => "Yields <color=yellow><b>+27%</b></color> bonus <color=yellow>Sun</color> on death.";
}
