// Firefly's attack: lights up the plant it bites, giving away its position. Only grants its own
// light if the plant isn't already glowing from some other source (its own kit, another Glowing
// Swarm, a weather exposure effect, etc.) - checked once at apply time, matching how those other
// lightEmissionRangeAdder sources already work (see Plant.UpdateStats)
public class GlowingSwarmEffect : StatusEffect
{
    private const float glowRadius = 0.75f;
    public static readonly UnityEngine.Color GlowColor = new UnityEngine.Color(0.85f, 0.73f, 0.28f);

    private bool _grantedLight;
    private bool _colored;

    public GlowingSwarmEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Fire;
    }

    public override string GetName() => "<color=#D9BB48>Glowing Swarm</color>";
    public override string GetDescription() =>
        $"Emits a <color=#D9BB48><b>{glowRadius:F2}</b></color>-radius light, revealing the plant's position.";

    public override void OnApply()
    {
        _grantedLight = target.lightEmissionRange <= 0f;
        if (_grantedLight)
            target.lightEmissionRangeAdder += glowRadius;
    }

    // the Light2D itself is only created lazily (by Plant.UpdateStats, next frame) once
    // lightEmissionRange actually becomes positive, so the color can't be set until then -
    // polled here each tick until it exists, then set once
    public override void OnTick(float deltaTime)
    {
        if (_colored || !_grantedLight) return;
        var light = target.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
        if (light == null) return;
        light.color = GlowColor;
        _colored = true;
    }

    public override void OnExpire()
    {
        if (_grantedLight)
            target.lightEmissionRangeAdder -= glowRadius;
    }
}
