using UnityEngine;

public class FreezeEffect : HardCrowdControl
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float magicResistShred;

    public FreezeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        float baseShred = 0.32f * (1f + source.elementalAffinity);
        magicResistShred = 100f * baseShred / (1f - Mathf.Min(baseShred, 0.99f));
        effectType = Type.negative;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Freeze</color>";
    public override string GetDescription() => $"Target is completely frozen in place for <color=green><b>{duration:F1}s</b></color>. Magic Armor reduced by <color=green><b>{magicResistShred:F0}</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Freeze", new Color(0f, 1f, 1f));

        Insect insect = (Insect)target;
        insect.magicArmorAdder -= magicResistShred;
    }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.magicArmorAdder += magicResistShred;
    }
}
