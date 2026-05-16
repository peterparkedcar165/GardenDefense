using UnityEngine;

public class GeyserKnockEffect : HardCrowdControl
{
    private Transform visual;
    private float knockUpHeight;
    private float totalDuration;
    private float elapsed = 0f;
    private float baseVisualY;
    private FlyingInsect flyingInsect;

    public GeyserKnockEffect(Entity target, float duration, int level, Entity source, float knockUpHeight)
        : base(target, duration, level, source)
    {
        this.knockUpHeight = knockUpHeight;
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#4FC3F7>Knocked Up</color>";
    public override string GetDescription() => "Launched into the air by a geyser.";

    public override void OnApply()
    {
        Insect insect = target as Insect;
        if (insect == null) return;

        totalDuration = duration;
        visual = insect.transform.Find("Visual");
        baseVisualY = visual != null ? visual.localPosition.y : 0f;

        flyingInsect = insect as FlyingInsect;
        flyingInsect?.SetFlight(false);
    }

    public override void OnTick(float deltaTime)
    {
        if (visual == null) return;
        elapsed += deltaTime;
        float progress = Mathf.Clamp01(elapsed / totalDuration);
        float arcY = Mathf.Sin(progress * Mathf.PI) * knockUpHeight;
        Vector3 pos = visual.localPosition;
        pos.y = baseVisualY + arcY;
        visual.localPosition = pos;
    }

    public override void OnExpire()
    {
        if (visual != null)
        {
            Vector3 pos = visual.localPosition;
            pos.y = baseVisualY;
            visual.localPosition = pos;
        }
        flyingInsect?.SetFlight(true);
    }
}
