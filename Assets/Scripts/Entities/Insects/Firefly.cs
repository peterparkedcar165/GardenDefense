public class Firefly : FlyingInsect
{
    private UnityEngine.Rendering.Universal.Light2D _light2D;

    // Glowing Swarm refreshes on every successful hit, so it stays lit for as long as the
    // Firefly keeps attacking the same plant, fading out a few seconds after it stops
    private const float GlowingSwarmDuration = 6f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void Attack()
    {
        if (target is Plant plant)
        {
            bool hit = plant.ReceiveAttack(attackDamage, this);
            if (hit)
                plant.ApplyEffect(new GlowingSwarmEffect(plant, GlowingSwarmDuration, 1, this));
        }
        else
        {
            base.Attack();
        }
    }

    public override void UpdateStats()
    {
        base.UpdateStats();

        if (lightEmissionRange > 0f && _light2D == null)
        {
            // centred on the insect's own spot, where its light counts from (DarknessManager) and where plants look at
            // it, not on its raised sprite (Tommy, 2026-11-28)
            UnityEngine.GameObject glow = new UnityEngine.GameObject("Glow");
            glow.transform.SetParent(transform, false);
            _light2D = glow.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            _light2D.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            _light2D.intensity = 0.5f;
            _light2D.falloffIntensity = 0.5f;
            _light2D.color = GlowingSwarmEffect.GlowColor;
        }

        if (_light2D != null)
        {
            _light2D.pointLightOuterRadius = lightEmissionRange;
            _light2D.pointLightInnerRadius = lightEmissionRange * 0.3f;
        }
    }

    public override string GetAbilityDescription() =>
        $"Emits a faint glow in a <b>{(data != null ? data.baseLightEmissionRange : lightEmissionRange):0.#}</b>-radius. " +
        $"Attacks inflict <color=#D9BB48><b>Glowing Swarm</b></color>, lighting up the target plant.";
}
