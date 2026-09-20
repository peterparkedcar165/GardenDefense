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

        if (lightEmissionRange > 0f && _light2D == null && visual != null)
        {
            _light2D = visual.gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
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

    public override string GetDescription() =>
        $"Passive insect. Emits a faint <b>{(data != null ? data.baseLightEmissionRange : lightEmissionRange):F1}</b>-radius light. " +
        $"Attacks inflict <color=#D9BB48><b>Glowing Swarm</b></color>, lighting up the target plant if it isn't already glowing." + FlyingLine() + AggressivityLine();
}
