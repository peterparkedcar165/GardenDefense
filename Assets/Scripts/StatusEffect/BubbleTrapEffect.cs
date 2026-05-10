using UnityEngine;

public class BubblePrisonEffect : HardCrowdControl
{
    private Transform visual;
    private float knockUpHeight;

    public BubblePrisonEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#3399FF>Bubble Prison</color>";
    public override string GetDescription() => "Imprisoned inside a bubble.";

    public override void OnApply()
    {
        knockUpHeight = Random.Range(0.8f, 1.2f);
        Insect insect = target as Insect;
        if (insect == null) return;
        visual = insect.transform.Find("Visual");
    }

    public override void OnTick(float deltaTime)
    {
        if (visual != null && target is not FlyingInsect)
        {
            Vector3 pos = visual.localPosition;
            pos.y = Mathf.MoveTowards(pos.y, knockUpHeight, 5f * deltaTime);
            visual.localPosition = pos;
        }
    }

    public override void OnExpire()
    {
        if (visual == null || target is FlyingInsect) return;
        Vector3 pos = visual.localPosition;
        pos.y = 0f;
        visual.localPosition = pos;
    }
}
