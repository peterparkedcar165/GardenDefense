public class Firefly : FlyingInsect
{
    private UnityEngine.Rendering.Universal.Light2D _light2D;

    protected override void Awake()
    {
        base.Awake();
        baseMaxHealth      = 250f;
        baseAttackDamage   = 15f;
        baseAttackSpeed    = 1f;
        baseAttackRange    = 0.5f;
        baseMovementSpeed  = 1.3f;
        baseLightEmissionRange = 1f;
        sunDrop            = 5;
        aggressivity       = Aggressivity.Low;
    }

    public override void UpdateStats()
    {
        base.UpdateStats();

        if (lightEmissionRange > 0f && _light2D == null && visual != null)
        {
            _light2D = visual.gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            _light2D.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            _light2D.intensity = 0.8f;
            _light2D.falloffIntensity = 0.5f;
        }

        if (_light2D != null)
        {
            _light2D.pointLightOuterRadius = lightEmissionRange;
            _light2D.pointLightInnerRadius = lightEmissionRange * 0.3f;
        }
    }

    public override string GetName() => "<b><color=#FFE066>Firefly</color></b>";

    public override string GetDescription()
        => $"The {GetName()} is a fragile insect that glows faintly. Alone it is harmless, but its light empowers nearby Moths.";

    public override string GetPassiveDescription()
        => $"Emits a small light radius of <color=green><b>{lightEmissionRange:F1}</b></color> units, boosting the speed of nearby <b><color=#C8A2C8>Moths</color></b>.";
}
