using UnityEngine;

public class AeoniumBloomEffect : StatusEffect
{
    private readonly float _rangeBonus;
    private readonly float _speedBonus;
    private readonly Transform _indicator;

    public AeoniumBloomEffect(Entity target, float duration, int level, Entity source, float rangeBonus, float speedBonus, Transform indicator)
        : base(target, duration, level, source)
    {
        _rangeBonus = rangeBonus;
        _speedBonus = speedBonus;
        _indicator = indicator;
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        target.attackRangeMultiplier += _rangeBonus;
        target.attackSpeedMultiplier += _speedBonus;
        if (_indicator != null) _indicator.gameObject.SetActive(true);
    }

    public override void OnExpire()
    {
        target.attackRangeMultiplier -= _rangeBonus;
        target.attackSpeedMultiplier -= _speedBonus;
        if (_indicator != null) _indicator.gameObject.SetActive(false);
    }

    public override string GetName() => "<color=green>Grassy Terrain</color>";
    public override string GetDescription() =>
        $"Attack Range increased by <color=green><b>{_rangeBonus * 100f:F0}%</b></color>. Attack Speed increased by <color=green><b>{_speedBonus * 100f:F0}%</b></color>.";
}
