public class GustEffect : ElementalDebuff
{
    public GustEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {

    }

    public override string GetName() => "<color=#E0E0E0>Gust</color>";
    public override string GetDescription() => "When another elemental primer lands, it is refreshed and spread to nearby insects. Gust is then consumed.";

    public override void OnApply() { }
    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
