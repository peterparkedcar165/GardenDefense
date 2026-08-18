using UnityEngine;

// wind elemental effect proc: does nothing on its own until another element's damage detonates it
public class WindshearEffect : StatusEffect
{
    public WindshearEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Wind;
    }

    public override string GetName() => "<color=#B2EBF2>Windshear</color>";
    public override string GetDescription()
    {
        float shred = 15f * (1f + (source?.elementalAffinity ?? 0f));
        return $"The next non-{PlantData.ElementalTag(ElementalType.Wind)} Elemental Damage taken consumes this effect, reducing that Element's Resistance by <color=red><b>{shred:F0}%</b></color>.";
    }

    public override void OnApply() =>
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Windshear", new Color(0.7f, 0.95f, 0.95f));

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
