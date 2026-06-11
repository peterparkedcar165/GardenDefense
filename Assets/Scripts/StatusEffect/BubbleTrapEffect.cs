using UnityEngine;

public class BubblePrisonEffect : Airborne
{
    private Transform visual;
    private float knockUpHeight;
    private float storedVisualY;
    private float baseY;
    private float phase;
    private GameObject bubbleVisual;
    private const float bobSpeed = 2f;
    private const float bobAmplitude = 0.08f;

    public BubblePrisonEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#3399FF>Bubble Prison</color>";
    public override string GetDescription() => "Hanging in the air, imprisoned within a bubble.";

    public override void OnApply()
    {
        Insect insect = target as Insect;
        if (insect == null) return;
        visual = insect.transform.Find("Visual");

        Sprite bubbleSprite = Resources.Load<Sprite>("bubblePrisonTrapped");
        if (bubbleSprite != null && visual != null)
        {
            bubbleVisual = new GameObject("BubblePrisonVisual");
            bubbleVisual.transform.SetParent(visual);
            bubbleVisual.transform.localPosition = Vector3.zero;
            bubbleVisual.transform.localScale = Vector3.one * 4f; // SIZE OF BUBBLE
            SpriteRenderer sr = bubbleVisual.AddComponent<SpriteRenderer>();
            sr.sprite = bubbleSprite;
            SpriteRenderer insectSR = visual.GetComponent<SpriteRenderer>();
            if (insectSR != null)
            {
                sr.sortingLayerID = insectSR.sortingLayerID;
                sr.sortingOrder = insectSR.sortingOrder + 1;
            }
        }

        if (target is FlyingInsect fi)
        {
            storedVisualY = visual != null ? visual.localPosition.y : 0f;
            fi.SetFlight(false);
        }
        else
        {
            knockUpHeight = Random.Range(0.3f, 0.5f);
        }
    }

    public override void OnTick(float deltaTime)
    {
        if (visual == null) return;

        Insect insect = target as Insect;
        if (insect != null && insect.verticalVelocity != 0f)
        {
            if (target is FlyingInsect)
                storedVisualY = visual.localPosition.y;
            else
            {
                baseY = visual.localPosition.y;
                knockUpHeight = visual.localPosition.y;
            }

            if (insect.HasEffect<KnockUpEffect>()) return; // geyser overrides , let ApplyGravity control Y
            if (insect.affectedByGravity) return;
            insect.verticalVelocity = 0f;          // bubble caught at apex: snap vV so bob runs
        }

        phase += bobSpeed * deltaTime;
        float bob = Mathf.Sin(phase) * bobAmplitude;

        Vector3 pos = visual.localPosition;
        if (target is FlyingInsect)
            pos.y = storedVisualY + bob;
        else
        {
            baseY = Mathf.MoveTowards(baseY, knockUpHeight, 8f * deltaTime);
            pos.y = baseY + bob;
        }
        visual.localPosition = pos;
    }

    public override void OnExpire()
    {
        if (bubbleVisual != null)
            UnityEngine.Object.Destroy(bubbleVisual);

        if (visual == null) return;

        if (target is FlyingInsect)
        {
            target.ApplyEffect(new GroundedEffect(target, 5f, 1, source));
        }
        else
        {
            // gravity takes over naturally from current visual height
        }
    }
}
