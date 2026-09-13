using UnityEngine;

public class SteamEffect : StatusEffect
{
    public float resistShred;
    public float healingReduction;

    public SteamEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        resistShred = 0.18f * (1f + source.elementalAffinity);
        healingReduction = 0.35f * (1f + source.elementalAffinity);
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#1E90FF>Steam</color>";
    public override string GetDescription() =>
        $"Reduce <color=#4FC3F7><b>Water Resistance</b></color> and <color=orange><b>Fire Resistance</b></color> by <color=red><b>{resistShred * 100f:F0}%</b></color>.\n" +
        $"Reduce <color=#FF6B81><b>Healing Received</b></color> by <color=red><b>{healingReduction * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Steam", new Color(0.25f, 0.75f, 1f));

        Insect insect = (Insect)target;
        insect.fireResistanceAdder  -= resistShred;
        insect.waterResistanceAdder -= resistShred;
        insect.healingReceivedAdder -= healingReduction;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.fireResistanceAdder  += resistShred;
        insect.waterResistanceAdder += resistShred;
        insect.healingReceivedAdder += healingReduction;
    }
}
