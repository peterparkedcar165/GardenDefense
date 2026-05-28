using UnityEngine;

public class FreezeEffect : HardCrowdControl
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float magicResistShred;

    public FreezeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        magicResistShred = 0.32f * (1f + source.elementalAffinity);
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#00FFFF>Freeze</color>";
    public override string GetDescription() => $"Target is completely frozen in place for <color=green><b>{duration:F1}s</b></color>. Reduces Magic Resistance by <color=green><b>{magicResistShred * 100f:F0}%</b></color>. (3 × (1 + <color=#FFD700>{source.elementalAffinity * 100:F0}% Elemental Affinity</color>))";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Freeze", new Color(0f, 1f, 1f));

        Debug.Log("Freeze applied");

        spriteRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = new Color(0f, 1f, 1f, 1f);
        }

        Insect insect = (Insect)target;
        insect.magicResistanceAdder -= magicResistShred;
    }

    public override void OnExpire()
    {
        Debug.Log("Freeze expired");
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        Insect insect = (Insect)target;
        insect.magicResistanceAdder += magicResistShred;
    }
}
