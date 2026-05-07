using UnityEngine;

public class FreezeEffect : HardCrowdControl
{

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public FreezeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        // for stun, nothing occurs.
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#00FFFF>Freeze</color>";
    public override string GetDescription() => $"Target is completely frozen in place.";

    public override void OnApply()
    {
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Freeze", new Color(0f, 1f, 1f));

        Debug.Log("Freeze applied");

        spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = new Color(0f, 1f, 1f, 1f);
        }
    }

    public override void OnExpire()
    {
        Debug.Log("Freeze expired");
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}
