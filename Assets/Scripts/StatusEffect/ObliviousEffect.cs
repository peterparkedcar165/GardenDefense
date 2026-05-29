public class ObliviousEffect : StatusEffect
{
    public ObliviousEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new UnityEngine.Vector3(0.4f, 0f, 0f), "Oblivious", new UnityEngine.Color(0.6f, 0.6f, 0.6f));
    }

    public override void OnExpire() { }
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#AAAAAA>Oblivious</color>";
    public override string GetDescription() => "Cannot locate or target plants.";
}
