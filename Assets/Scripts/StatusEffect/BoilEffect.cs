using UnityEngine;

public class BoilEffect : StatusEffect
{
    public float resistShred;

    public BoilEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        resistShred = 0.32f * (1f + source.elementalPower);
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#1E90FF>Boil</color>";
    public override string GetDescription() => $"Reduce Water Resistance and Fire Resistance by <color=green><b>{resistShred * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Boil", new Color(0.25f, 0.75f, 1f));

        Insect insect = (Insect)target;
        insect.fireResistanceAdder  -= resistShred;
        insect.waterResistanceAdder -= resistShred;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.fireResistanceAdder  += resistShred;
        insect.waterResistanceAdder += resistShred;
    }
}
