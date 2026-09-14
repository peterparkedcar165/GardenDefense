using UnityEngine;

public class SunMarkEffect : StatusEffect
{
    private const float FireResistanceReduction = 0.18f;
    private readonly bool _reducesFireResistance;

    public SunMarkEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Fire;
        // multiple Sunflowers can mark the same insect; each mark pays out its own bonus sun on death
        sourceStackable = true;

        // skill tree node 3.2
        _reducesFireResistance = source is Sunflower sunflower && SkillTreeManager.HasUnlock(sunflower, Sunflower.SunMarkResistUnlock);
    }

    public override void OnApply()
    {
        if (_reducesFireResistance) target.fireResistanceAdder -= FireResistanceReduction;
    }

    public override void OnExpire()
    {
        if (_reducesFireResistance) target.fireResistanceAdder += FireResistanceReduction;
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
    public override string GetDescription()
    {
        string desc = "Yields <color=yellow><b>+27%</b></color> bonus <color=yellow>Sun</color> on death.";
        if (_reducesFireResistance) desc += $" Reduces <color=orange><b>Fire Resistance</b></color> by <color=green><b>{FireResistanceReduction * 100f:F0}%</b></color>.";
        return desc;
    }
}
